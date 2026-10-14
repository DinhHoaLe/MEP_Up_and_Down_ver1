using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MEP_Up_and_Down.UpDownTool.DuctCutEvent
{
    public sealed class MEPRequest
    {
        public string Kind { get; }
        public string Option { get; }
        public bool Angle45 { get; }
        public double Offset { get; }
        public MEPRequest(string kind, string option, bool angle45, double offset)
        { Kind = kind; Option = option; Angle45 = angle45; Offset = offset; }
    }
    public class MEPDuctCutEvent : IExternalEventHandler
    {
        public UpAndDown window { get; set; }
        public MEPRequest Request { get; set; }
        public void Execute(UIApplication app)
        {
            var request = Request;
            Request = null;
            if (request == null) return;
            if (window.IsClosed) { window.CompleteRequest(); return; }
            window.Hide();
            try
            {
                UIDocument uidoc = app.ActiveUIDocument;
                if (uidoc == null) throw new InvalidOperationException("Open a Revit project first.");
                Document doc = uidoc.Document;
                bool move = request.Option.StartsWith("Move", StringComparison.Ordinal);
                var filter = new CurveFilter(request.Kind, movement: move);
                double offset = UnitUtils.ConvertToInternalUnits(request.Offset, UnitTypeId.Millimeters);
                bool up = request.Option.EndsWith("Up", StringComparison.Ordinal);
                if (request.Option.StartsWith("Move", StringComparison.Ordinal))
                {
                    var refs = uidoc.Selection.PickObjects(ObjectType.Element, filter, "Select MEP elements to move, then click Finish.");
                    if (refs.Count == 0) return;
                    Run(doc, request.Option, () => UpAndDownUtils.Move(doc,
                        refs.Select(r => r.ElementId).Distinct().ToList(), XYZ.BasisZ * (up ? offset : -offset)));
                }
                else
                {
                    var first = uidoc.Selection.PickObject(ObjectType.PointOnElement, filter, "Pick the first point on a horizontal MEP element.");
                    var second = uidoc.Selection.PickObject(ObjectType.PointOnElement, new CurveFilter(request.Kind, first.ElementId), "Pick the second point on the same element.");
                    Run(doc, request.Option, () => UpAndDownUtils.Cut(doc, first, second, offset, up, request.Angle45));
                }
                window.SetStatus("Completed. You can run another operation.");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { window.SetStatus("Selection cancelled. No changes made."); }
            catch (Exception ex)
            {
                window.SetStatus("Operation failed. Changes rolled back.");
                TaskDialog.Show("MEP Up And Down", ex.Message);
            }
            finally
            {
                window.CompleteRequest();
                if (!window.IsClosed) { window.Show(); window.Activate(); }
            }
        }
        private static void Run(Document doc, string name, Action action)
        {
            using (var t = new Transaction(doc, "MEP " + name))
            {
                t.Start();
                var failures = new RollbackErrors();
                var options = t.GetFailureHandlingOptions();
                options.SetFailuresPreprocessor(failures);
                options.SetClearAfterRollback(true);
                t.SetFailureHandlingOptions(options);
                action();
                if (t.Commit() != TransactionStatus.Committed)
                    throw new InvalidOperationException("Revit rolled back MEP " + name + "."
                        + (failures.Errors.Count > 0
                            ? "\n\n" + string.Join("\n\n", failures.Errors)
                            : "\nNo detailed failure was returned. Check constraints, fitting sizes and routing preferences."));
            }
        }
        public string GetName() => "MEP Up And Down";
        private sealed class CurveFilter : ISelectionFilter
        {
            private readonly string kind;
            private readonly ElementId id;
            private readonly bool movement;
            public CurveFilter(string kind, ElementId id = null, bool movement = false) { this.kind = kind; this.id = id; this.movement = movement; }
            public bool AllowElement(Element e) => (movement ? UpAndDownUtils.MatchesMove(e, kind) : UpAndDownUtils.Matches(e, kind)) && (id == null || e.Id == id);
            public bool AllowReference(Reference r, XYZ p) => true;
        }
        private sealed class RollbackErrors : IFailuresPreprocessor
        {
            public List<string> Errors { get; } = new List<string>();

            public FailureProcessingResult PreprocessFailures(FailuresAccessor a)
            {
                bool hasErrors = false;
                foreach (var message in a.GetFailureMessages())
                {
                    if (message.GetSeverity() != FailureSeverity.Error) continue;
                    hasErrors = true;
                    // Save plain text before rollback clears Revit's failure objects.
                    var ids = message.GetFailingElementIds().Select(id => id.ToString()).ToList();
                    string detail = message.GetDescriptionText()
                        + (ids.Count > 0 ? "\nElement IDs: " + string.Join(", ", ids) : "");
                    if (!Errors.Contains(detail)) Errors.Add(detail);
                }
                return hasErrors ? FailureProcessingResult.ProceedWithRollBack : FailureProcessingResult.Continue;
            }
        }
    }
}
