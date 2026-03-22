using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Drawing;
//using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;

namespace MEP_Up_and_Down.Lib
{
    public class MEPLib
    {
        //convert bitmap to bitmap
        public static BitmapImage Convert(Bitmap bitmap)
        {
            MemoryStream memory = new MemoryStream();
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        public static XYZ GetIntersectionPoint(Line line, Plane plane)
        {
            XYZ normal = plane.Normal;
            XYZ origin = plane.Origin;

            XYZ sp = line.GetEndPoint(0);
            XYZ ep = line.GetEndPoint(1);

            XYZ normalize = (ep - sp).Normalize();

            double distance = (normal.DotProduct(origin) - normal.DotProduct(sp)/normal.DotProduct(normalize));

            XYZ intersecPoint = sp + distance * normalize;

            return intersecPoint;
        }

        public static XYZ FindPointFromStartPoint(Line line, double distance)
        {
            XYZ A = line.GetEndPoint(0);
            XYZ B = line.GetEndPoint(1);
            XYZ vectorAB = A - B;

            double scale = distance / vectorAB.GetLength();

            double X = scale * vectorAB.X + A.X;
            double Y = scale * vectorAB.Y + A.Y;
            double Z = scale * vectorAB.Z + A.Z;

            return new XYZ(X, Y, Z);
        }

        public static XYZ FindPointFromEndPoint(Line line, double distance)
        {
            XYZ A = line.GetEndPoint(0);
            XYZ B = line.GetEndPoint(1);
            XYZ vectorAB = A - B;

            double scale = distance / vectorAB.GetLength();

            double X = scale * vectorAB.X + B.X;
            double Y = scale * vectorAB.Y + B.Y;
            double Z = scale * vectorAB.Z + B.Z;

            return new XYZ(X, Y, Z);
        }

        public static double ToRadian(double degree)
        {
            return degree * Math.PI / 180;
        }

        public static void CreateElbowFitting(Document doc, Duct duct0, Duct duct1)
        {
            ConnectorManager cm1 = duct0.ConnectorManager;
            ConnectorManager cm2 = duct1.ConnectorManager;
            ConnectorSet cs1 = cm1.Connectors;
            ConnectorSet cs2 = cm2.Connectors;
            
            List<Connector> connectors = new List<Connector>();
            foreach (Connector c1 in cs1) { 
                foreach(Connector c2 in cs2)
                {
                    XYZ o1 = c1.Origin;
                    XYZ o2 = c2.Origin;

                    if (o1.DistanceTo(o2) == 0)
                    {
                        connectors.Add(c1);
                        connectors.Add(c2);
                    }
                }
            }

            doc.Create.NewElbowFitting(connectors[0], connectors[1]);
        }

        public static Solid GetMEPSoild(Element element)
        {


            Options options = new Options()
            {
                ComputeReferences = true,
                DetailLevel = ViewDetailLevel.Medium,
            };

            GeometryElement geometryElement = element.get_Geometry(options);
            foreach (GeometryObject geoOb in geometryElement)
            {
                if (geoOb is Solid solid)
                {
                    return solid;
                }
            }

            return null;
        }

        public static bool CheckSoil(Element ele1, Element ele2)
        {

            Solid solid1 = MEPLib.GetMEPSoild(ele1);
            Solid solid2 = MEPLib.GetMEPSoild(ele2);

            Solid intersectSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Intersect);

            if (intersectSolid != null && intersectSolid.Volume > 0) return true;

            return false;
        }

        public static void DeleteElement(Document doc, IList<ElementId> ids, double length, out List<ElementId> result)
        {
            result = new List<ElementId>();

            foreach (ElementId id in ids)
            {
                LocationCurve locationCurve = null;
                Element element = doc.GetElement(id);

                if(element is Duct duct) locationCurve = duct.Location as LocationCurve;
                if(element is Pipe pipe) locationCurve = pipe.Location as LocationCurve;
                if(element is CableTray ct) locationCurve = ct.Location as LocationCurve;

                double curveLength = locationCurve.Curve.Length;
                double val = Math.Round(curveLength - length, 3);
                if (val == 0)
                {
                    doc.Delete(id);
                }
                else
                {
                    result.Add(id);
                }
            }
        }
    }


}
