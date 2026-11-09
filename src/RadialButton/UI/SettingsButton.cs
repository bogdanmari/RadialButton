using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RadialButton
{
    internal sealed class SettingsButton : Button
    {
        private bool hovered;
        private bool pressed;
        public bool Primary { get; set; }
        public bool Destructive { get; set; }
        public bool AccentLabel { get; set; }

        public SettingsButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? SettingsTheme.Background : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            SettingsPalette palette = SettingsTheme.For(this);
            Color fill = Primary ? palette.Accent : Color.White;
            Color ink = Primary ? Color.White : Destructive ? SettingsTheme.Danger :
                AccentLabel ? palette.AccentText : SettingsTheme.Ink;
            Color border = Primary ? palette.Accent : SettingsTheme.Border;
            if (Enabled && (hovered || pressed))
            {
                fill = Primary ? (pressed ? palette.AccentPressed : palette.AccentHover) :
                    Destructive ? (pressed ? SettingsTheme.DangerPressed : SettingsTheme.DangerHover) :
                    pressed ? palette.TintPressed : AccentLabel ? palette.TintHover : palette.Tint;
                ink = Primary ? Color.White : Destructive ? SettingsTheme.Danger : palette.AccentText;
                border = Destructive ? SettingsTheme.Danger : palette.Accent;
            }
            if (!Enabled)
            {
                fill = SettingsTheme.DisabledBackground;
                ink = SettingsTheme.DisabledText;
                border = SettingsTheme.DisabledBorder;
            }
            float radius = 6f * DeviceDpi / 96f;
            float stroke = DeviceDpi / 96f;
            RectangleF rect = new RectangleF(stroke / 2, stroke / 2, Width - stroke, Height - stroke);
            using (GraphicsPath path = RoundedRectangle(rect, radius))
            using (Brush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border, stroke))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
            Rectangle textBounds = ClientRectangle;
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
            using (Font emphasis = new Font(Font, FontStyle.Bold))
            {
                Font font = Primary && TextRenderer.MeasureText(Text.Replace("&&", "&"), emphasis).Width < Width - 8
                    ? emphasis : Font;
                TextRenderer.DrawText(e.Graphics, Text, font, textBounds, ink, flags);
            }
            if (Enabled && Focused && ShowFocusCues)
                SettingsDrawing.Focus(e.Graphics, ClientRectangle, palette.Accent, stroke, Primary);
        }

        internal static GraphicsPath RoundedRectangle(RectangleF rect, float radius)
        {
            float diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnMouseCaptureChanged(EventArgs e) { pressed = false; Invalidate(); base.OnMouseCaptureChanged(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) pressed = true; Invalidate(); base.OnKeyDown(e); }
        protected override void OnKeyUp(KeyEventArgs e) { pressed = false; Invalidate(); base.OnKeyUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { pressed = false; Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { pressed = false; Invalidate(); base.OnEnabledChanged(e); }
    }
}
