using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MEP_Up_and_Down.UpDownTool
{
    public static class UpAndDownUtils
    {
        public static bool Matches(Element e, string kind) => kind == "Duct" && e is Duct || kind == "Pipe" && e is Pipe
            || kind == "Cable Tray" && e is CableTray || kind == "Conduit" && e is Conduit;

        public static bool MatchesMove(Element e, string kind)
        {
            if (Matches(e, kind)) return true;
            var category = kind == "Duct" ? BuiltInCategory.OST_DuctFitting : kind == "Pipe" ? BuiltInCategory.OST_PipeFitting
                : kind == "Cable Tray" ? BuiltInCategory.OST_CableTrayFitting : BuiltInCategory.OST_ConduitFitting;
            return e is FamilyInstance && e.Category?.Id == new ElementId(category);
        }

        private static ConnectorManager Manager(Element e) => e is MEPCurve curve ? curve.ConnectorManager
            : (e as FamilyInstance)?.MEPModel?.ConnectorManager;

        public static void Move(Document doc, ICollection<ElementId> ids, XYZ delta)
        {
            var connections = new List<Tuple<ElementId, int, ElementId, int>>();
            foreach (var id in ids)
            {
                Element element = doc.GetElement(id);
                if (element.Pinned || element.GroupId != ElementId.InvalidElementId)
                    throw new InvalidOperationException("Unpin selected elements and remove them from groups before moving.");
                var manager = Manager(element);
                if (manager == null) continue;
                foreach (Connector c in manager.Connectors)
                    if (c.ConnectorType == ConnectorType.End)
                        foreach (Connector other in c.AllRefs)
                            if (other.Owner.Id != id && other.ConnectorType == ConnectorType.End && c.IsConnectedTo(other))
                                connections.Add(Tuple.Create(id, c.Id, other.Owner.Id, other.Id));
            }
            ElementTransformUtils.MoveElements(doc, ids, delta);
            doc.Regenerate();
            foreach (var pair in connections)
            {
                var a = Manager(doc.GetElement(pair.Item1))?.Lookup(pair.Item2);
                var b = Manager(doc.GetElement(pair.Item3))?.Lookup(pair.Item4);
                if (a == null || b == null || !a.IsConnectedTo(b) || a.Origin.DistanceTo(b.Origin) > 1e-4)
                    throw new InvalidOperationException("Move would disconnect the route. Include connected fittings/elements in the selection, or use Cut to create an offset.");
            }
        }

        public static void Cut(Document doc, Reference first, Reference second, double offset, bool up, bool angle45)
        {
            if (first.ElementId != second.ElementId) throw new InvalidOperationException("Pick both points on the same element.");
            var source = doc.GetElement(first) as MEPCurve;
            var axis = (source?.Location as LocationCurve)?.Curve as Line;
            if (axis == null) throw new InvalidOperationException("Only straight MEP elements are supported.");
            if (source.Pinned || source.GroupId != ElementId.InvalidElementId)
                throw new InvalidOperationException("Unpin the element and remove it from its group before cutting.");
            var connectors = source.ConnectorManager.Connectors.Cast<Connector>().ToList();
            int endCount = connectors.Count(c => c.ConnectorType == ConnectorType.End);
            if (endCount != 2)
                throw new InvalidOperationException($"Cut requires two endpoint connectors. Element {source.Id} has {endCount} endpoint connectors.");
            // AllRefs can also contain logical references; only physical connections
            // on an additional connector indicate a branch attached to this curve.
            var branchOwners = connectors.Where(c => c.ConnectorType != ConnectorType.End
                    && c.ConnectorType != ConnectorType.Logical)
                .SelectMany(c => c.AllRefs.Cast<Connector>().Where(r => r.Owner.Id != source.Id
                    && r.ConnectorType != ConnectorType.Logical && c.IsConnectedTo(r)))
                .Select(r => r.Owner.Id).Distinct().ToList();
            if (branchOwners.Count > 0)
                throw new InvalidOperationException($"Element {source.Id} has connected branches/taps (element IDs: {string.Join(", ", branchOwners)}). Cut currently supports elements without branches/taps. Select a separate straight segment or disconnect these branches first.");
            if (Math.Abs(axis.Direction.Z) > 1e-6)
                throw new InvalidOperationException("Cut requires a horizontal element. Move supports sloped elements.");
            XYZ start = axis.GetEndPoint(0), end = axis.GetEndPoint(1), direction = axis.Direction;
            double a = (first.GlobalPoint - start).DotProduct(direction), b = (second.GlobalPoint - start).DotProduct(direction);
            if (a > b) { double swap = a; a = b; b = swap; }
            double run = angle45 ? offset : 0, tolerance = doc.Application.ShortCurveTolerance * 2;
            if (a - run <= tolerance || b + run >= axis.Length - tolerance || b - a <= tolerance || offset <= tolerance)
                throw new InvalidOperationException("Not enough length for this offset. Pick points farther from the ends or reduce the offset.");
            XYZ rise = XYZ.BasisZ * (up ? offset : -offset);
            XYZ[] points = { start, start + direction * (a - run), start + direction * a + rise,
                start + direction * b + rise, start + direction * (b + run), end };
            Connector originalEnd = At(source, end);
            Connector originalStart = At(source, start);
            var startNeighbors = originalStart.AllRefs.Cast<Connector>().Where(c => c.Owner.Id != source.Id
                && c.ConnectorType == ConnectorType.End && originalStart.IsConnectedTo(c)).ToList();
            var neighbors = originalEnd.AllRefs.Cast<Connector>().Where(c => c.Owner.Id != source.Id
                && c.ConnectorType == ConnectorType.End && originalEnd.IsConnectedTo(c)).ToList();
            foreach (var neighbor in neighbors) originalEnd.DisconnectFrom(neighbor);
            // Copy before trimming to retain type, dimensions and instance parameters.
            var segments = new List<MEPCurve> { source };
            for (int i = 1; i < 5; i++)
            {
                var ids = ElementTransformUtils.CopyElement(doc, source.Id, XYZ.Zero);
                var copy = ids.Select(doc.GetElement).OfType<MEPCurve>().Single();
                foreach (Connector connector in copy.ConnectorManager.Connectors)
                {
                    var refs = connector.AllRefs.Cast<Connector>().Where(c => c.Owner.Id != copy.Id && connector.IsConnectedTo(c)).ToList();
                    foreach (var other in refs) connector.DisconnectFrom(other);
                }
                segments.Add(copy);
            }
            for (int i = 0; i < 5; i++) ((LocationCurve)segments[i].Location).Curve = Line.CreateBound(points[i], points[i + 1]);
            doc.Regenerate();
            foreach (var neighbor in neighbors) At(segments[4], end).ConnectTo(neighbor);
            for (int i = 0; i < 4; i++)
            {
                doc.Regenerate();
                try
                {
                    doc.Create.NewElbowFitting(At(segments[i], points[i + 1]), At(segments[i + 1], points[i + 1]));
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException ex)
                {
                    throw new InvalidOperationException("Could not create an offset elbow. Check available length, fitting sizes and the selected type's elbow/routing preferences. " + ex.Message, ex);
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
                {
                    throw new InvalidOperationException("Could not create an offset elbow. Check available length, fitting sizes and the selected type's elbow/routing preferences. " + ex.Message, ex);
                }
            }
            doc.Regenerate();
            foreach (var neighbor in startNeighbors)
                if (!At(source, start).IsConnectedTo(neighbor))
                    throw new InvalidOperationException("The existing start connection could not be preserved.");
            foreach (var neighbor in neighbors)
                if (!At(segments[4], end).IsConnectedTo(neighbor))
                    throw new InvalidOperationException("The existing end connection could not be preserved.");
            foreach (var segment in segments)
                foreach (Connector c in segment.ConnectorManager.Connectors)
                    if (c.ConnectorType == ConnectorType.End && c.Origin.DistanceTo(start) > 1e-4
                        && c.Origin.DistanceTo(end) > 1e-4 && !c.IsConnected)
                        throw new InvalidOperationException("An internal joint is open. Check the type's elbow/routing preferences.");
        }
        private static Connector At(MEPCurve curve, XYZ point)
        {
            var c = curve.ConnectorManager.Connectors.Cast<Connector>().Where(x => x.ConnectorType == ConnectorType.End)
                .OrderBy(x => x.Origin.DistanceTo(point)).FirstOrDefault();
            if (c == null || c.Origin.DistanceTo(point) > 1e-4) throw new InvalidOperationException("Could not find the endpoint connector.");
            return c;
        }
    }
}
