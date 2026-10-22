using System;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitCodexChat
{
    // This is the ONLY model access path. It runs on Revit's API thread and
    // returns plain text, never live Element/Document/Connector objects to AI.
    public class SelectionReader : IExternalEventHandler
    {
        private readonly ChatPane pane;
        public SelectionReader(ChatPane pane) { this.pane = pane; }
        public string GetName() => "Codex read selected elements";

        public void Execute(UIApplication app)
        {
            try
            {
                var ui = app.ActiveUIDocument;
                if (ui == null) throw new InvalidOperationException("Hãy mở một model Revit trước.");
                var ids = ui.Selection.GetElementIds().OrderBy(id => id.Value).ToList();
                if (ids.Count == 0) throw new InvalidOperationException("Chưa chọn phần tử. Chọn trong model rồi bấm Đọc lại.");
                var text = new StringBuilder();
                text.AppendLine("Thời điểm đọc: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                text.AppendLine("Tài liệu: " + ui.Document.Title);
                text.AppendLine("Số phần tử chọn: " + ids.Count + " (tối đa đọc 20 phần tử; mỗi phần tử tối đa 40 thông số).");
                foreach (var id in ids.Take(20))
                {
                    var e = ui.Document.GetElement(id);
                    if (e == null) continue;
                    text.AppendLine("\n--- Phần tử " + id + " ---");
                    text.AppendLine("Tên: " + e.Name + "; Category: " + e.Category?.Name + "; Class: " + e.GetType().Name);
                    text.AppendLine("Type: " + ui.Document.GetElement(e.GetTypeId())?.Name);
                    text.AppendLine("Level: " + ui.Document.GetElement(e.LevelId)?.Name + "; Pinned: " + e.Pinned);
                    if (e.Location is LocationCurve lc && lc.Curve is Line line)
                    {
                        text.AppendLine("Chiều dài (mm): " + UnitUtils.ConvertFromInternalUnits(line.Length, UnitTypeId.Millimeters).ToString("0.##"));
                        text.AppendLine("Đầu 1 (mm): " + Point(line.GetEndPoint(0)) + "; Đầu 2 (mm): " + Point(line.GetEndPoint(1)));
                    }
                    var manager = (e as MEPCurve)?.ConnectorManager
                        ?? (e as FamilyInstance)?.MEPModel?.ConnectorManager;
                    if (manager != null)
                    {
                        foreach (Connector c in manager.Connectors)
                        {
                            if (c.ConnectorType == ConnectorType.Logical) continue;
                            var linked = c.AllRefs.Cast<Connector>().Where(r => r.Owner.Id != e.Id
                                && r.ConnectorType != ConnectorType.Logical && c.IsConnectedTo(r))
                                .Select(r => r.Owner.Id.ToString()).Distinct();
                            text.AppendLine("Connector " + c.Id + " (" + c.ConnectorType + ") tại " + Point(c.Origin)
                                + " mm; nối ID: " + string.Join(", ", linked));
                        }
                    }
                    foreach (var p in e.Parameters.Cast<Parameter>().Where(p => p.HasValue)
                        .OrderBy(p => p.Definition.Name).Take(40))
                    {
                        string value = p.StorageType == StorageType.String ? p.AsString() : p.AsValueString();
                        if (string.IsNullOrEmpty(value) && p.StorageType == StorageType.ElementId) value = p.AsElementId().ToString();
                        if (string.IsNullOrEmpty(value) && p.StorageType == StorageType.Integer) value = p.AsInteger().ToString();
                        if (!string.IsNullOrEmpty(value)) text.AppendLine(p.Definition.Name + ": " + Limit(value, 300));
                    }
                    if (text.Length > 45000) { text.AppendLine("Đã giới hạn dữ liệu."); break; }
                }
                pane.SelectionCompleted(text.ToString());
            }
            catch (Exception ex) { pane.SelectionFailed(ex.Message); }
        }

        private static string Limit(string value, int count) => value.Length <= count ? value : value.Substring(0, count) + "…";
        private static string Point(XYZ p) => string.Join(", ", new[] { p.X, p.Y, p.Z }
            .Select(x => UnitUtils.ConvertFromInternalUnits(x, UnitTypeId.Millimeters).ToString("0.##")));
    }
}
