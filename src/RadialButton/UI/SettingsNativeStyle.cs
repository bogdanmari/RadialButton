using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RadialButton
{
    // Scoped overlays retain native edit selection, trackbar hit testing and geometry.
    internal sealed class SettingsNativeStyle : NativeWindow
    {
        private static readonly ConditionalWeakTable<Control, SettingsNativeStyle> Styles =
            new ConditionalWeakTable<Control, SettingsNativeStyle>();
        private readonly Control control;
        private bool hovered;
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr handle);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr handle, IntPtr dc);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, ref NativeRect rect);

        public static void Apply(Control control)
        {
            if (!Styles.TryGetValue(control, out _)) Styles.Add(control, new SettingsNativeStyle(control));
            control.Invalidate();
        }

        private SettingsNativeStyle(Control control)
        {
            this.control = control;
            control.HandleCreated += (s, e) => AssignHandle(control.Handle);
            control.HandleDestroyed += (s, e) => ReleaseHandle();
            control.GotFocus += (s, e) => control.Invalidate();
            control.LostFocus += (s, e) => control.Invalidate();
            control.MouseEnter += (s, e) => { hovered = true; control.Invalidate(); };
            control.MouseLeave += (s, e) => { hovered = false; control.Invalidate(); };
            control.EnabledChanged += (s, e) => control.Invalidate();
            if (control is TrackBar slider) slider.ValueChanged += (s, e) => control.Invalidate();
            if (control.IsHandleCreated) AssignHandle(control.Handle);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (control.IsDisposed || !control.IsHandleCreated) return;
            if (m.Msg == 0x000F || m.Msg == 0x0085) // WM_PAINT / WM_NCPAINT
            {
                IntPtr dc = GetWindowDC(Handle);
                if (dc == IntPtr.Zero) return;
                try { using (Graphics g = Graphics.FromHdc(dc)) Draw(g); }
                finally { ReleaseDC(Handle, dc); }
            }
            else if (m.Msg == 0x0317 && m.WParam != IntPtr.Zero)
                using (Graphics g = Graphics.FromHdc(m.WParam)) Draw(g);
        }

        private void Draw(Graphics graphics)
        {
            SettingsPalette palette = SettingsTheme.For(control);
            float scale = control.DeviceDpi / 96f;
            if (control is TrackBar slider && slider.Orientation == Orientation.Horizontal)
            {
                NativeRect track = new NativeRect(), thumb = new NativeRect();
                SendMessage(Handle, 0x041A, IntPtr.Zero, ref track); // TBM_GETCHANNELRECT
                SendMessage(Handle, 0x0419, IntPtr.Zero, ref thumb); // TBM_GETTHUMBRECT
                if (track.Right <= track.Left || thumb.Right <= thumb.Left) return;
                float center = (thumb.Left + thumb.Right) / 2f;
                // Cover only native channel and thumb; leave ticks and hit areas intact.
                using (Brush white = new SolidBrush(Color.White))
                {
                    graphics.FillRectangle(white, track.Left, track.Top, track.Right - track.Left, track.Bottom - track.Top);
                    graphics.FillRectangle(white, thumb.Left, thumb.Top, thumb.Right - thumb.Left, thumb.Bottom - thumb.Top);
                }
                float y = (track.Top + track.Bottom) / 2f;
                using (Pen neutral = new Pen(SettingsTheme.Border, 2 * scale))
                using (Pen active = new Pen(control.Enabled ? palette.Accent : SettingsTheme.DisabledBorder, 2 * scale))
                {
                    graphics.DrawLine(neutral, track.Left, y, track.Right, y);
                    graphics.DrawLine(active, track.Left, y, center, y);
                }
                var bounds = new RectangleF(thumb.Left + scale, thumb.Top + scale,
                    thumb.Right - thumb.Left - 2 * scale, thumb.Bottom - thumb.Top - 2 * scale);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = SettingsButton.RoundedRectangle(bounds, 3 * scale))
                using (var fill = new SolidBrush(control.Enabled ? palette.Accent : SettingsTheme.DisabledBackground))
                using (var pen = new Pen(control.Enabled ? palette.Accent : SettingsTheme.DisabledBorder, scale))
                { graphics.FillPath(fill, path); graphics.DrawPath(pen, path); }
                if (control.Focused) SettingsDrawing.Focus(graphics, control.ClientRectangle, palette.Accent, scale);
                return;
            }
            Color color = !control.Enabled ? SettingsTheme.DisabledBorder : control.Focused ? palette.Accent :
                hovered ? SettingsTheme.BorderHover : SettingsTheme.Border;
            float stroke = control.Focused ? 2 * scale : scale;
            using (Pen pen = new Pen(color, stroke))
                graphics.DrawRectangle(pen, stroke / 2, stroke / 2, control.Width - stroke, control.Height - stroke);
        }
    }
}
