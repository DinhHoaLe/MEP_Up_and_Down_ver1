using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MEP_Up_and_Down.UpDownTool;
using System.Windows.Interop;

namespace MEP_Up_and_Down
{
    [Transaction(TransactionMode.Manual)]
    public class MEP_Up_and_Down_Class : IExternalCommand
    {
        private static UpAndDown activeWindow;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (commandData.Application.ActiveUIDocument == null)
            {
                message = "Open a Revit project first.";
                return Result.Failed;
            }
            if (activeWindow != null && !activeWindow.IsClosed)
            {
                activeWindow.Activate();
                return Result.Succeeded;
            }
            activeWindow = new UpAndDown();
            new WindowInteropHelper(activeWindow).Owner = commandData.Application.MainWindowHandle;
            activeWindow.Show();
            return Result.Succeeded;
        }
    }
}
