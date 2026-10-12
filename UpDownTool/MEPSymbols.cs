using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace MEP_Up_and_Down.UpDownTool
{
 public static class MEPSymbols
 {
 public static DrawingImage Create(string kind, string option, bool diagonal, string offsetLabel)
 {
 bool move = option.StartsWith("Move"), up = option.EndsWith("Up");
            var color = kind == "Duct" ? "#2563EB" : kind == "Pipe" ? "#059669" : kind == "Cable Tray" ? "#D97706" : "#7C3AED";
            var brush = (Brush)new BrushConverter().ConvertFromString(color);
            var drawing = new DrawingGroup();
            double baseline = up ? 135 : 55, result = move ? (up ? 55 : 135) : (up ? 75 : 115);
            using (var dc = drawing.Open())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 360, 190));
                var ghost = new Pen(new SolidColorBrush(Color.FromRgb(165, 179, 198)), 2) { DashStyle = DashStyles.Dash };
                dc.DrawLine(ghost, new Point(15, baseline), new Point(310, baseline));
                Point[] points = move ? new[] { new Point(15, result), new Point(310, result) }
                    : new[] { new Point(15, baseline), new Point(65, baseline), new Point(diagonal ? 125 : 65, result),
                        new Point(diagonal ? 200 : 260, result), new Point(260, baseline), new Point(310, baseline) };
                DrawRoute(dc, points, brush, kind);
                var arrow = new Pen(brush, 2);
                dc.DrawLine(arrow, new Point(335, baseline), new Point(335, result));
                double sign = up ? 1 : -1;
                dc.DrawLine(arrow, new Point(335, result), new Point(329, result + sign * 9));
                dc.DrawLine(arrow, new Point(335, result), new Point(341, result + sign * 9));
                if (!move)
                {
                    dc.DrawEllipse(Brushes.White, new Pen(brush, 2), points[2], 4, 4);
                    dc.DrawEllipse(Brushes.White, new Pen(brush, 2), points[3], 4, 4);
                }
                var label = new FormattedText(offsetLabel + " mm", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 13, brush, 1);
                dc.DrawText(label, new Point(135, 160));
            }
            drawing.Freeze();
            return new DrawingImage(drawing);
        }
        private static void DrawRoute(DrawingContext dc, Point[] points, Brush brush, string kind)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                Point a = points[i], b = points[i + 1];
                Vector d = b - a; d.Normalize();
                Vector n = new Vector(-d.Y, d.X) * (kind == "Duct" || kind == "Cable Tray" ? 5 : 3);
                if (kind == "Cable Tray" || kind == "Duct")
                {
                    dc.DrawLine(new Pen(brush, 2), a + n, b + n);
                    dc.DrawLine(new Pen(brush, 2), a - n, b - n);
                    if (kind == "Cable Tray")
                        for (double t = 0.08; t < 1; t += 14 / (b - a).Length)
                        { Point rung = a + (b - a) * t; dc.DrawLine(new Pen(brush, 1), rung - n, rung + n); }
                }
                else
                {
                    dc.DrawLine(new Pen(brush, kind == "Pipe" ? 7 : 5), a, b);
                    dc.DrawLine(new Pen(Brushes.White, kind == "Pipe" ? 2 : 1), a, b);
                    if (kind == "Conduit")
                        for (double t = 0.15; t < 1; t += 32 / (b - a).Length)
                        { Point band = a + (b - a) * t; dc.DrawLine(new Pen(brush, 2), band - n * 1.6, band + n * 1.6); }
                }
            }
        }
 }
}
