using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace RadialButton
{
    internal static class FlatGridButtons
    {
        private sealed class State
        {
            public int Row = -1;
            public int Column = -1;
            public bool Pressed;
        }
        private static readonly ConditionalWeakTable<DataGridView, State> Styled =
            new ConditionalWeakTable<DataGridView, State>();

        public static void Apply(DataGridView grid)
        {
            if (Styled.TryGetValue(grid, out _)) return;
            var state = new State();
            Styled.Add(grid, state);
            grid.CellPainting += Paint;
            grid.EditingControlShowing += (sender, e) =>
            {
                if (e.Control is TextBox editor)
                {
                    editor.BackColor = Color.White;
                    editor.ForeColor = SettingsTheme.Ink;
                    SettingsNativeStyle.Apply(editor);
                }
            };
            grid.CellMouseEnter += (sender, e) =>
            {
                int previous = state.Row;
                state.Row = e.RowIndex; state.Column = e.ColumnIndex;
                if (previous >= 0 && previous < grid.Rows.Count) grid.InvalidateRow(previous);
                if (state.Row >= 0) grid.InvalidateRow(state.Row);
            };
            grid.CellMouseLeave += (sender, e) =>
            {
                state.Row = -1; state.Column = -1; state.Pressed = false;
                if (e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count) grid.InvalidateRow(e.RowIndex);
            };
            grid.MouseDown += (sender, e) => { state.Pressed = e.Button == MouseButtons.Left; grid.Invalidate(); };
            grid.MouseUp += (sender, e) => { state.Pressed = false; grid.Invalidate(); };
            grid.MouseCaptureChanged += (sender, e) => { state.Pressed = false; grid.Invalidate(); };
            grid.GotFocus += (sender, e) => grid.Invalidate();
            grid.LostFocus += (sender, e) => grid.Invalidate();
        }

        private static void Paint(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (e.ColumnIndex < 0 || !Styled.TryGetValue(grid, out State state)) return;
            if (e.RowIndex < 0)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.ContentForeground);
                using (Pen pen = new Pen(SettingsTheme.BorderSubtle))
                    e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                e.Handled = true;
                return;
            }
            SettingsPalette palette = SettingsTheme.For(grid);
            bool selected = (e.State & DataGridViewElementStates.Selected) != 0;
            bool hover = grid.Enabled && state.Row == e.RowIndex;
            bool hotCell = hover && state.Column == e.ColumnIndex;
            Color background = selected ? (hover ? palette.TintPressed : palette.TintHover) :
                hover ? palette.Tint : Color.White;
            using (Brush brush = new SolidBrush(background)) e.Graphics.FillRectangle(brush, e.CellBounds);
            float scale = grid.DeviceDpi / 96f;
            string name = grid.Columns[e.ColumnIndex].Name;
            if (grid.Columns[e.ColumnIndex] is DataGridViewButtonColumn)
            {
                bool available = grid.Enabled && !string.IsNullOrEmpty(Convert.ToString(e.FormattedValue));
                if (available)
                {
                    bool remove = name == "Remove";
                    Color ink = hotCell ? (remove ? SettingsTheme.Danger : palette.AccentText) : SettingsTheme.Muted;
                    if (hotCell)
                    {
                        Color fill = remove ? (state.Pressed ? SettingsTheme.DangerPressed : SettingsTheme.DangerHover) :
                            state.Pressed ? palette.TintPressed : palette.TintHover;
                        Rectangle bounds = Rectangle.Inflate(e.CellBounds, -(int)(2 * scale), -(int)(2 * scale));
                        using (var path = SettingsButton.RoundedRectangle(bounds, 4 * scale))
                        using (var brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
                    }
                    SettingsDrawing.RowIcon(e.Graphics, e.CellBounds, name, ink, scale);
                }
            }
            else if (grid.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn)
            {
                int arrow = SystemInformation.VerticalScrollBarWidth;
                var textBounds = new Rectangle(e.CellBounds.Left + e.CellStyle.Padding.Left, e.CellBounds.Top,
                    Math.Max(0, e.CellBounds.Width - arrow - e.CellStyle.Padding.Horizontal), e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, Convert.ToString(e.FormattedValue), e.CellStyle.Font, textBounds,
                    SettingsTheme.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                SettingsDrawing.Chevron(e.Graphics, new Rectangle(e.CellBounds.Right - arrow, e.CellBounds.Top, arrow, e.CellBounds.Height), SettingsTheme.Muted, scale);
                if (hotCell || selected)
                    using (Pen pen = new Pen(selected && grid.Focused ? palette.Accent : SettingsTheme.Border, scale))
                        e.Graphics.DrawRectangle(pen, e.CellBounds.Left + 1, e.CellBounds.Top + 1, e.CellBounds.Width - 3, e.CellBounds.Height - 3);
            }
            else e.Paint(e.ClipBounds, DataGridViewPaintParts.ContentForeground);
            using (Pen pen = new Pen(SettingsTheme.BorderSubtle))
                e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            if (selected)
            {
                using (Brush brush = new SolidBrush(palette.Accent))
                    e.Graphics.FillRectangle(brush, e.CellBounds.Left, e.CellBounds.Top + scale, 2 * scale, e.CellBounds.Height - 2 * scale);
                if (grid.Focused && grid.CurrentCell?.RowIndex == e.RowIndex && grid.CurrentCell.ColumnIndex == e.ColumnIndex)
                    SettingsDrawing.Focus(e.Graphics, e.CellBounds, palette.Accent, scale);
            }
            e.Paint(e.ClipBounds, DataGridViewPaintParts.ErrorIcon);
            e.Handled = true;
        }
    }
}
