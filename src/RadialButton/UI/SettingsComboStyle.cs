using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RadialButton
{
    // Keep the original ComboBox, native popup, accessibility and event wiring.
    // Only its visual surface and option rendering are replaced, locally.
    internal sealed class SettingsComboStyle : NativeWindow
    {
        private static readonly ConditionalWeakTable<ComboBox, SettingsComboStyle> Styles =
            new ConditionalWeakTable<ComboBox, SettingsComboStyle>();
        private readonly ComboBox combo;
        private bool hovered;
        private readonly int selectionHeight;
        private readonly int initialDpi;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

        public static void Apply(ComboBox combo)
        {
            if (combo.DropDownStyle != ComboBoxStyle.DropDownList) return;
            if (!Styles.TryGetValue(combo, out _)) Styles.Add(combo, new SettingsComboStyle(combo));
            combo.Invalidate();
        }

        private SettingsComboStyle(ComboBox combo)
        {
            this.combo = combo;
            initialDpi = combo.DeviceDpi;
            int itemHeight = combo.ItemHeight;
            int originalHeight = combo.Height;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = itemHeight;
            // Owner drawing adds one pixel to the closed field on WinForms.
            // Restore only its native selection-field height; popup item heights stay unchanged.
            selectionHeight = itemHeight - (combo.Height - originalHeight);
            combo.DrawItem += DrawOption;
            combo.HandleCreated += (sender, e) => Attach();
            combo.HandleDestroyed += (sender, e) => ReleaseHandle();
            combo.MouseEnter += (sender, e) => { hovered = true; combo.Invalidate(); };
            combo.MouseLeave += (sender, e) => { hovered = false; combo.Invalidate(); };
            combo.GotFocus += (sender, e) => combo.Invalidate();
            combo.LostFocus += (sender, e) => combo.Invalidate();
            combo.SelectedIndexChanged += (sender, e) => combo.Invalidate();
            combo.DropDownClosed += (sender, e) => combo.Invalidate();
            combo.EnabledChanged += (sender, e) => combo.Invalidate();
            if (combo.IsHandleCreated) Attach();
        }

        private void Attach()
        {
            AssignHandle(combo.Handle);
            RestoreSelectionHeight();
        }

        private void RestoreSelectionHeight() => SendMessage(combo.Handle, 0x0153, new IntPtr(-1),
            new IntPtr((int)Math.Round(selectionHeight * (double)combo.DeviceDpi / initialDpi)));

        private void DrawOption(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            SettingsPalette palette = SettingsTheme.For(combo);
            bool closed = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool highlighted = (e.State & DrawItemState.Selected) != 0;
            Color fill = closed ? Color.White : highlighted ? palette.TintHover :
                e.Index == combo.SelectedIndex ? palette.Tint : Color.White;
            using (Brush brush = new SolidBrush(fill)) e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, combo.GetItemText(combo.Items[e.Index]), combo.Font, e.Bounds,
                combo.Enabled ? SettingsTheme.Ink : SettingsTheme.DisabledText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (combo.IsDisposed || !combo.IsHandleCreated) return;
            if (m.Msg == 0x02E3) RestoreSelectionHeight(); // WM_DPICHANGED_AFTERPARENT
            if (m.Msg == 0x000F) // WM_PAINT
            {
                using (Graphics graphics = Graphics.FromHwnd(Handle)) DrawClosed(graphics);
            }
            else if ((m.Msg == 0x0317 || m.Msg == 0x0318) && m.WParam != IntPtr.Zero)
            {
                using (Graphics graphics = Graphics.FromHdc(m.WParam)) DrawClosed(graphics);
            }
        }

        private void DrawClosed(Graphics graphics)
        {
            float scale = combo.DeviceDpi / 96f;
            SettingsPalette palette = SettingsTheme.For(combo);
            // WM_PRINT can share the parent's HDC: never clear outside our bounds.
            using (var background = new SolidBrush(combo.Parent?.BackColor ?? Color.White))
                graphics.FillRectangle(background, new Rectangle(0, 0, combo.Width, combo.Height));
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color border = !combo.Enabled ? SettingsTheme.DisabledBorder : combo.Focused ? palette.Accent :
                hovered ? SettingsTheme.BorderHover : SettingsTheme.Border;
            float stroke = combo.Focused ? 2 * scale : scale;
            var rect = new RectangleF(stroke / 2, stroke / 2, combo.Width - stroke, combo.Height - stroke);
            using (var path = SettingsButton.RoundedRectangle(rect, 6 * scale))
            using (var fill = new SolidBrush(combo.Enabled ? Color.White : SettingsTheme.DisabledBackground))
            using (var pen = new Pen(border, stroke))
            {
                graphics.FillPath(fill, path); graphics.DrawPath(pen, path);
            }
            int arrowWidth = SystemInformation.VerticalScrollBarWidth;
            var textRect = new Rectangle((int)(5 * scale), 0, Math.Max(0, combo.Width - arrowWidth - (int)(8 * scale)), combo.Height);
            TextRenderer.DrawText(graphics, combo.Text, combo.Font, textRect,
                combo.Enabled ? SettingsTheme.Ink : SettingsTheme.DisabledText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            SettingsDrawing.Chevron(graphics, new Rectangle(combo.Width - arrowWidth, 0, arrowWidth, combo.Height),
                combo.Enabled ? SettingsTheme.Muted : SettingsTheme.DisabledText, scale);
        }
    }
}
