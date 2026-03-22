using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using MEP_Up_and_Down.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEP_Up_and_Down.UpDownTool
{
    public class UpAndDownUtils
    {
        public static void DuctCut45(Document doc, Reference r1, Reference r2, double offset, bool isTop)
        {


            XYZ pick1 = r1.GlobalPoint;
            XYZ pick2 = r2.GlobalPoint;

            Duct duct = doc.GetElement(r1) as Duct;
            Location location = duct.Location;
            LocationCurve locationCurve = location as LocationCurve;
            Line locationLine = locationCurve.Curve as Line;
            XYZ direction = locationLine.Direction;

            Plane plane1 = Plane.CreateByNormalAndOrigin(direction, pick1);
            Plane plane2 = Plane.CreateByNormalAndOrigin(direction, pick2);

            XYZ gd1 = MEPLib.GetIntersectionPoint(locationLine, plane1);
            XYZ gd2 = MEPLib.GetIntersectionPoint(locationLine, plane2);


            double dis = offset / Math.Tan(MEPLib.ToRadian(45));
            Line line = Line.CreateBound(gd1, gd2);

            IList<ElementId> ids = new List<ElementId>();

            XYZ p1 = MEPLib.FindPointFromStartPoint(line, dis);
            XYZ p2 = MEPLib.FindPointFromEndPoint(line, -dis);


            double length = p1.DistanceTo(p2);
            using (Transaction t = new Transaction(doc, "Test"))
            {
                t.Start();

                ElementId id1 = MechanicalUtils.BreakCurve(doc, duct.Id, p1);
                ids.Add(id1);

                ElementId id2 = null;
                ElementId id3 = null;

                try
                {
                    id2 = MechanicalUtils.BreakCurve(doc, duct.Id, p2);
                }
                catch { }

                try
                {
                    id3 = MechanicalUtils.BreakCurve(doc, id1, p2);
                }
                catch { }

                if (id2 != null) ids.Add(id2);
                if (id3 != null) ids.Add(id3);
                ids.Add(duct.Id);

                MEPLib.DeleteElement(doc, ids, length, out List<ElementId> newIds);

                XYZ ngd1;
                XYZ ngd2;

                if (isTop)
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z + offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z + offset);
                }
                else
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z - offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z - offset);
                }

                ElementId ductTypeId = duct.GetTypeId();
                ElementId levelId = duct.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
                ElementId systemId = duct.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM).AsElementId();

                //Duct.Create(doc, systemId, ductTypeId, levelId, ngd1, ngd2);

                Duct ductTop = Duct.Create(doc, systemId, ductTypeId, levelId, ngd1, ngd2);

                Duct ductLeft = Duct.Create(doc, systemId, ductTypeId, levelId, p1, ngd1);

                Duct ductRight = Duct.Create(doc, systemId, ductTypeId, levelId, p2, ngd2);

                MEPLib.CreateElbowFitting(doc, ductTop, ductLeft);

                MEPLib.CreateElbowFitting(doc, ductTop, ductRight);

                Duct d1 = doc.GetElement(newIds[0]) as Duct;
                Duct d2 = doc.GetElement(newIds[1]) as Duct;

                bool inTersection = MEPLib.CheckSoil(d1, ductLeft);

                if (inTersection)
                {
                    MEPLib.CreateElbowFitting(doc, d1, ductLeft);
                    MEPLib.CreateElbowFitting(doc, d2, ductRight);
                }
                else
                {
                    MEPLib.CreateElbowFitting(doc, d2, ductLeft);
                    MEPLib.CreateElbowFitting(doc, d1, ductRight);
                }

                t.Commit();
            }
        }

        public static void DuctCut90(Document doc, Reference r1, Reference r2, double offset, bool isTop)
        {


            XYZ pick1 = r1.GlobalPoint;
            XYZ pick2 = r2.GlobalPoint;

            Duct duct = doc.GetElement(r1) as Duct;
            Location location = duct.Location;
            LocationCurve locationCurve = location as LocationCurve;
            Line locationLine = locationCurve.Curve as Line;
            XYZ direction = locationLine.Direction;

            Plane plane1 = Plane.CreateByNormalAndOrigin(direction, pick1);
            Plane plane2 = Plane.CreateByNormalAndOrigin(direction, pick2);

            XYZ gd1 = MEPLib.GetIntersectionPoint(locationLine, plane1);
            XYZ gd2 = MEPLib.GetIntersectionPoint(locationLine, plane2);


            //double dis = offset / Math.Tan(MEPLib.ToRadian(45));
            double dis = 0;

            Line line = Line.CreateBound(gd1, gd2);

            IList<ElementId> ids = new List<ElementId>();

            XYZ p1 = MEPLib.FindPointFromStartPoint(line, dis);
            XYZ p2 = MEPLib.FindPointFromEndPoint(line, -dis);


            double length = p1.DistanceTo(p2);
            using (Transaction t = new Transaction(doc, "Test"))
            {
                t.Start();

                ElementId id1 = MechanicalUtils.BreakCurve(doc, duct.Id, p1);
                ids.Add(id1);

                ElementId id2 = null;
                ElementId id3 = null;

                try
                {
                    id2 = MechanicalUtils.BreakCurve(doc, duct.Id, p2);
                }
                catch { }

                try
                {
                    id3 = MechanicalUtils.BreakCurve(doc, id1, p2);
                }
                catch { }

                if (id2 != null) ids.Add(id2);
                if (id3 != null) ids.Add(id3);
                ids.Add(duct.Id);

                MEPLib.DeleteElement(doc, ids, length, out List<ElementId> newIds);


                XYZ ngd1;
                XYZ ngd2;

                if (isTop) {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z + offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z + offset);
                }
                else
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z - offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z - offset);
                }


                ElementId ductTypeId = duct.GetTypeId();
                ElementId levelId = duct.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
                ElementId systemId = duct.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM).AsElementId();

                //Duct.Create(doc, systemId, ductTypeId, levelId, ngd1, ngd2);

                Duct ductBottom = Duct.Create(doc, systemId, ductTypeId, levelId, ngd1, ngd2);

                Duct ductLeft = Duct.Create(doc, systemId, ductTypeId, levelId, p1, ngd1);

                Duct ductRight = Duct.Create(doc, systemId, ductTypeId, levelId, p2, ngd2);

                MEPLib.CreateElbowFitting(doc, ductBottom, ductLeft);

                MEPLib.CreateElbowFitting(doc, ductBottom, ductRight);

                Duct d1 = doc.GetElement(newIds[0]) as Duct;
                Duct d2 = doc.GetElement(newIds[1]) as Duct;

                bool inTersection = MEPLib.CheckSoil(d1, ductLeft);

                if (inTersection)
                {
                    MEPLib.CreateElbowFitting(doc, d1, ductLeft);
                    MEPLib.CreateElbowFitting(doc, d2, ductRight);
                }
                else
                {
                    MEPLib.CreateElbowFitting(doc, d2, ductLeft);
                    MEPLib.CreateElbowFitting(doc, d1, ductRight);
                }

                t.Commit();
            }
        }

    }
}
