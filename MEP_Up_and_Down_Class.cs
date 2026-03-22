using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using MEP_Up_and_Down.Lib;
using MEP_Up_and_Down.UpDownTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MEP_Up_and_Down
{
    [Transaction(TransactionMode.Manual)]
    public class MEP_Up_and_Down_Class : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var window = new UpAndDown();
            window.Show();

            //UIDocument uidoc = commandData.Application.ActiveUIDocument;
            //Document doc = uidoc.Document;
            //View view = doc.ActiveView;


            //double offset = 1000 / 304.8;

            //Reference r1 = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, "Pick First Point");
            //Reference r2 = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, "Pick second Point");


            //UpAndDownUtils.DuctCut45(doc, r1, r2, offset, true);

            //UpAndDownUtils.DuctCut90(doc, r1, r2, offset, true);


            return Result.Succeeded;
        }







    }
}
