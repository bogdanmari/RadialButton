using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace RadialButton
{
    internal static class SettingsCheckStyle
    {
        private static readonly ConditionalWeakTable<CheckBox, object> Styled = new ConditionalWeakTable<CheckBox, object>();
        public static void Apply(CheckBox check)
        {
            if (Styled.TryGetValue(check, out _)) return;
            Styled.Add(check, new object());
            check.Paint += (sender, e) =>
            {
                float scale = check.DeviceDpi / 96f;
                int size = (int)(13 * scale);
                var bounds = new Rectangle(0, (check.Height - size) / 2, size, size);
                SettingsPalette palette = SettingsTheme.For(check);
                Color fill = !check.Enabled ? SettingsTheme.DisabledBackground : check.Checked ? palette.Accent : Color.White;
                Color border = !check.Enabled ? SettingsTheme.DisabledBorder : check.Checked ? palette.Accent : SettingsTheme.Border;
                using (var brush = new SolidBrush(fill)) e.Graphics.FillRectangle(brush, bounds);
                using (var pen = new Pen(border, scale))
                    e.Graphics.DrawRectangle(pen, bounds.Left + scale / 2, bounds.Top + scale / 2, bounds.Width - scale, bounds.Height - scale);
                if (check.Checked)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var pen = new Pen(check.Enabled ? Color.White : SettingsTheme.DisabledText, 1.5f * scale))
                        e.Graphics.DrawLines(pen, new[] { new PointF(bounds.Left + 3 * scale, bounds.Top + 6 * scale),
                            new PointF(bounds.Left + 5 * scale, bounds.Top + 9 * scale), new PointF(bounds.Left + 10 * scale, bounds.Top + 3 * scale) });
                }
                if (check.Focused) SettingsDrawing.Focus(e.Graphics, check.ClientRectangle, palette.Accent, scale);
            };
            check.CheckedChanged += (sender, e) => check.Invalidate();
        }
    }
}
