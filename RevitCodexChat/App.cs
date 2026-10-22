using System;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitCodexChat
{
    public class App : IExternalApplication
    {
        public static readonly DockablePaneId PaneId = new DockablePaneId(new Guid("F9F26897-2D24-46D8-92AB-C8463B01E988"));
        private ChatPane pane;
        private ExternalEvent readEvent;

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                pane = new ChatPane();
                readEvent = ExternalEvent.Create(new SelectionReader(pane));
                pane.ReadSelection = () => readEvent.Raise() == ExternalEventRequest.Accepted;
                application.RegisterDockablePane(PaneId, "Codex · Revit AI", new PaneProvider(pane));
                var panel = application.CreateRibbonPanel("Codex AI");
                var button = panel.AddItem(new PushButtonData("RevitCodexChat.Open", "Mở chat AI",
                    Assembly.GetExecutingAssembly().Location, typeof(ShowChatCommand).FullName));
                button.ToolTip = "Mở bảng chat Codex. Chỉ đọc dữ liệu phần tử, không sửa model.";
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                pane?.Shutdown();
                readEvent?.Dispose();
                TaskDialog.Show("Codex · Revit AI", "Không thể khởi tạo add-in: " + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            pane?.Shutdown();
            readEvent?.Dispose();
            return Result.Succeeded;
        }
    }

    internal class PaneProvider : IDockablePaneProvider
    {
        private readonly ChatPane pane;
        public PaneProvider(ChatPane pane) { this.pane = pane; }
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = pane;
            data.InitialState = new DockablePaneState { DockPosition = DockPosition.Right };
        }
    }

    [Transaction(TransactionMode.ReadOnly)]
    public class ShowChatCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            data.Application.GetDockablePane(App.PaneId).Show();
            return Result.Succeeded;
        }
    }
}
