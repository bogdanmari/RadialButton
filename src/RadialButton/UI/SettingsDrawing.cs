using System.Drawing;
using System.Drawing.Drawing2D;

namespace RadialButton
{
    internal static class SettingsDrawing
    {
        public static void Focus(Graphics graphics, Rectangle bounds, Color color, float scale, bool onAccent = false)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = bounds;
            rect.Inflate(-3 * scale, -3 * scale);
            using (var path = SettingsButton.RoundedRectangle(rect, 4 * scale))
            using (var pen = new Pen(onAccent ? Color.White : color, 2 * scale))
                graphics.DrawPath(pen, path);
        }

        public static void Chevron(Graphics graphics, Rectangle bounds, Color color, float scale)
        {
            float x = bounds.Left + bounds.Width / 2f, y = bounds.Top + bounds.Height / 2f;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 1.5f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                graphics.DrawLines(pen, new[] { new PointF(x - 3 * scale, y - 1.5f * scale),
                    new PointF(x, y + 1.5f * scale), new PointF(x + 3 * scale, y - 1.5f * scale) });
        }

        public static void RowIcon(Graphics graphics, Rectangle bounds, string name, Color color, float scale)
        {
            float x = bounds.Left + bounds.Width / 2f, y = bounds.Top + bounds.Height / 2f;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 1.5f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (name == "Remove") { graphics.DrawLine(pen, x - 4 * scale, y, x + 4 * scale, y); return; }
                float direction = name == "MoveUp" ? -1 : 1;
                graphics.DrawLine(pen, x, y - 5 * scale, x, y + 5 * scale);
                graphics.DrawLines(pen, new[] { new PointF(x - 3.5f * scale, y + direction * scale),
                    new PointF(x, y + direction * 5 * scale), new PointF(x + 3.5f * scale, y + direction * scale) });
            }
        }
    }
}
