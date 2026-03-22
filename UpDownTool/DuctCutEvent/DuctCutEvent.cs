using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEP_Up_and_Down.UpDownTool.DuctCutEvent
{
    public class MEPDuctCutEvent : IExternalEventHandler
    {
        public UpAndDown window { get; set; }
        public void Execute(UIApplication app)
        {
            window.Hide();
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            Reference r1 = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, "Pick First Point");
            Reference r2 = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, "Pick second Point");

            //UpAndDownUtils.DuctCut45(doc, r1, r2, true);

            string option = window.Option;
            string angle = window.Angle;
            double offset = window.Offset / 304.8;

            switch (option) 
            {
                case "Cut Up":
                    {
                       if(angle == "45°")
                        {
                            UpAndDownUtils.DuctCut45(doc, r1, r2, offset, true);
                        }
                        else
                        {
                            UpAndDownUtils.DuctCut90(doc, r1, r2, offset, true);
                        }
                    break;
                    }

                case "Cut Down":
                    {
                        if (angle == "90°")
                        {
                            UpAndDownUtils.DuctCut90(doc, r1, r2, offset, false);
                        }
                        else
                        {
                            UpAndDownUtils.DuctCut45(doc, r1, r2, offset, false);
                        }
                        break;
                    }

            }

            window.Show();

        }

        public string GetName()
        {
            return "DuctCutEvent";
        }
    }
}
