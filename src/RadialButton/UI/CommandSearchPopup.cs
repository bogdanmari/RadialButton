using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RadialButton
{
    // The query is never written into a grid cell. Only an explicit choice
    // commits one of the original command labels, so Cancel/no matches is safe.
    internal sealed class CommandSearchPopup : ToolStripDropDown
    {
        private readonly TextBox search = new TextBox();
        private readonly ListBox results = new ListBox();
        private readonly Label status = new Label();
        private readonly List<string> commands;
        private readonly Action<string> accept;
        private int hovered = -1;

        public CommandSearchPopup(IEnumerable<string> commands, string current,
            int width, Font font, SettingsPalette palette, Action<string> accept)
        {
            this.commands = commands.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            this.accept = accept;
            AutoClose = true;
            Padding = new Padding(1);
            BackColor = SettingsTheme.BorderSubtle;
            var panel = new TableLayoutPanel
            {
                Size = new Size(Math.Max(280, width), 310),
                Padding = new Padding(8), ColumnCount = 1, RowCount = 4,
                BackColor = Color.White, ForeColor = SettingsTheme.Ink, Font = font
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            panel.Controls.Add(new Label { Text = "Search commands", Dock = DockStyle.Fill,
                ForeColor = SettingsTheme.Muted }, 0, 0);
            search.Dock = DockStyle.Fill;
            search.AccessibleName = "Search commands";
            search.BorderStyle = BorderStyle.FixedSingle;
            results.Dock = DockStyle.Fill;
            results.BorderStyle = BorderStyle.None;
            results.IntegralHeight = false;
            results.HorizontalScrollbar = true;
            results.AccessibleName = "Matching commands";
            results.DrawMode = DrawMode.OwnerDrawFixed;
            results.ItemHeight = font.Height + 8;
            results.DrawItem += (sender, e) =>
            {
                if (e.Index < 0) return;
                bool selected = (e.State & DrawItemState.Selected) != 0;
                using (Brush brush = new SolidBrush(e.Index == hovered ? palette.TintHover : selected ? palette.Tint : Color.White))
                    e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, Convert.ToString(results.Items[e.Index]), font,
                    e.Bounds, SettingsTheme.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                if ((e.State & DrawItemState.Focus) != 0)
                    SettingsDrawing.Focus(e.Graphics, e.Bounds, palette.Accent, results.DeviceDpi / 96f);
            };
            results.MouseMove += (sender, e) =>
            {
                int next = results.IndexFromPoint(e.Location);
                if (next != hovered) { hovered = next; results.Invalidate(); }
            };
            results.MouseLeave += (sender, e) => { hovered = -1; results.Invalidate(); };
            status.Dock = DockStyle.Fill;
            status.ForeColor = SettingsTheme.Muted;
            panel.Controls.Add(search, 0, 1);
            panel.Controls.Add(results, 0, 2);
            panel.Controls.Add(status, 0, 3);
            SettingsTheme.ApplySurface(panel, palette);
            Items.Add(new ToolStripControlHost(panel) { AutoSize = false, Size = panel.Size,
                Margin = Padding.Empty, Padding = Padding.Empty });
            search.TextChanged += (sender, e) => Filter();
            search.KeyDown += OnSearchKeyDown;
            results.KeyDown += OnResultKeyDown;
            results.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left && results.IndexFromPoint(e.Location) >= 0)
                    AcceptSelection();
            };
            Filter();
            int index = results.Items.IndexOf(current);
            if (index >= 0) results.SelectedIndex = index;
        }

        public void FocusSearch(string initialQuery)
        {
            search.Text = initialQuery ?? string.Empty;
            search.Focus();
            search.SelectionStart = search.TextLength;
        }

        private void Filter()
        {
            string[] words = search.Text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            results.BeginUpdate();
            try
            {
                results.Items.Clear();
                foreach (string command in commands)
                    if (words.All(word => command.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))
                        results.Items.Add(command);
                results.SelectedIndex = results.Items.Count == 0 ? -1 : 0;
                results.HorizontalExtent = results.Items.Cast<string>().Select(value =>
                    TextRenderer.MeasureText(value, results.Font).Width + 12).DefaultIfEmpty(0).Max();
            }
            finally { results.EndUpdate(); }
            status.Text = results.Items.Count == 0 ? "No matching commands" :
                results.Items.Count + " commands · Enter to select · Esc to cancel";
        }

        private void OnSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up)
            {
                if (results.Items.Count > 0)
                    results.SelectedIndex = Math.Max(0, Math.Min(results.Items.Count - 1,
                        results.SelectedIndex + (e.KeyCode == Keys.Down ? 1 : -1)));
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else OnResultKeyDown(sender, e);
        }

        private void OnResultKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Escape) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (e.KeyCode == Keys.Enter) AcceptSelection();
            else Close(ToolStripDropDownCloseReason.Keyboard);
        }

        private void AcceptSelection()
        {
            string selected = results.SelectedItem as string;
            if (selected == null) return;
            Close(ToolStripDropDownCloseReason.ItemClicked);
            accept(selected);
        }
    }
}
