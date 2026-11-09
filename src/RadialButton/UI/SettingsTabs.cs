using System.Drawing;
using System;
using System.Windows.Forms;

namespace RadialButton
{
    internal sealed class SettingsTabs : TabControl
    {
        private int hovered = -1;
        public SettingsTabs()
        {
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        // Native TabControl paints the sunken frame seen around the table.
        // Keep its selection/keyboard behaviour, but paint the entire surface.
        public override Rectangle DisplayRectangle
        {
            get
            {
                int top = base.DisplayRectangle.Top;
                return new Rectangle(0, top, ClientSize.Width, Math.Max(0, ClientSize.Height - top));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
            for (int index = 0; index < TabCount; index++)
                OnDrawItem(new DrawItemEventArgs(e.Graphics, Font, GetTabRect(index), index, DrawItemState.Default));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int next = -1;
            for (int index = 0; index < TabCount; index++)
                if (GetTabRect(index).Contains(e.Location)) { next = index; break; }
            if (next != hovered) { hovered = next; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = -1; Invalidate(); base.OnMouseLeave(e);
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e); Invalidate();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            bool selected = e.Index == SelectedIndex;
            SettingsPalette palette = SettingsTheme.For(this);
            Color background = e.Index == hovered ? palette.Hover : Color.White;
            using (Brush brush = new SolidBrush(background)) e.Graphics.FillRectangle(brush, e.Bounds);
            using (Font emphasis = new Font(Font, FontStyle.Bold))
            {
                Font font = selected && TextRenderer.MeasureText(TabPages[e.Index].Text, emphasis).Width < e.Bounds.Width - 4
                    ? emphasis : Font;
                TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, font, e.Bounds,
                    selected || e.Index == hovered ? SettingsTheme.Ink : SettingsTheme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
            if (selected)
            {
                using (Pen pen = new Pen(palette.Accent, 2f * DeviceDpi / 96f))
                    e.Graphics.DrawLine(pen, e.Bounds.Left + 2, e.Bounds.Bottom - 2, e.Bounds.Right - 2, e.Bounds.Bottom - 2);
                if (Focused && ShowFocusCues)
                    SettingsDrawing.Focus(e.Graphics, e.Bounds, palette.Accent, DeviceDpi / 96f);
            }
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control is TabPage page)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = SettingsTheme.Background;
            }
        }
    }
}
