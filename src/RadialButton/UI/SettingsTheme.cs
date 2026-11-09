using System.Drawing;
using System.Windows.Forms;
using System.Runtime.CompilerServices;

namespace RadialButton
{
    // Flat white surfaces; the wheel scheme supplies selection and hover colors.
    internal static class SettingsTheme
    {
        public static readonly Color Background = Color.White;
        public static readonly Color Ink = ColorTranslator.FromHtml("#20242A");
        public static readonly Color Muted = ColorTranslator.FromHtml("#59616B");
        public static readonly Color DisabledText = ColorTranslator.FromHtml("#8B929C");
        public static readonly Color Border = ColorTranslator.FromHtml("#858D98");
        public static readonly Color BorderHover = ColorTranslator.FromHtml("#616B78");
        public static readonly Color BorderSubtle = ColorTranslator.FromHtml("#E6E9EE");
        public static readonly Color SurfaceSubtle = ColorTranslator.FromHtml("#F7F8FA");
        public static readonly Color DisabledBackground = ColorTranslator.FromHtml("#F1F3F5");
        public static readonly Color DisabledBorder = ColorTranslator.FromHtml("#E2E6EB");
        public static readonly Color Hover = ColorTranslator.FromHtml("#F7F8FA");
        public static readonly Color Selection = ColorTranslator.FromHtml("#E9EFF4");
        public static readonly Color Danger = ColorTranslator.FromHtml("#B42318");
        public static readonly Color DangerHover = ColorTranslator.FromHtml("#FEF3F2");
        public static readonly Color DangerPressed = ColorTranslator.FromHtml("#FEE4E2");
        private static readonly ConditionalWeakTable<Control, SettingsPalette> Palettes =
            new ConditionalWeakTable<Control, SettingsPalette>();
        private static readonly SettingsPalette DefaultPalette = new SettingsPalette(WheelColorScheme.CreateDefault());

        public static SettingsPalette For(Control control)
        {
            for (Control current = control; current != null; current = current.Parent)
                if (Palettes.TryGetValue(current, out SettingsPalette palette)) return palette;
            return DefaultPalette;
        }

        public static void Apply(Form form, WheelColorScheme scheme = null)
        {
            SettingsPalette palette = new SettingsPalette(scheme ?? UserInterfaceSettingsService.LoadForAppearance().GetSelectedScheme());
            ApplySurface(form, palette);
        }

        public static void ApplySurface(Control form, SettingsPalette palette)
        {
            Palettes.Remove(form);
            Palettes.Add(form, palette);
            form.BackColor = Background;
            form.ForeColor = Ink;
            ApplyChildren(form);
            form.Invalidate(true);
        }

        private static void ApplyChildren(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is DataGridView grid)
                {
                    ApplyEditor(grid);
                    continue;
                }
                if (control is ComboBox combo)
                {
                    combo.FlatStyle = FlatStyle.Flat;
                    combo.BackColor = Color.White;
                    combo.ForeColor = Ink;
                    SettingsComboStyle.Apply(combo);
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = Color.White;
                    textBox.ForeColor = Ink;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    SettingsNativeStyle.Apply(textBox);
                }
                else if (control is CheckBox check)
                {
                    check.ForeColor = Ink;
                    SettingsCheckStyle.Apply(check);
                }
                else if (control is TrackBar slider)
                {
                    slider.BackColor = Background;
                    SettingsNativeStyle.Apply(slider);
                }
                else if (control is Panel || control is TabPage || control is TrackBar)
                    control.BackColor = Background;
                else if (control is Label label)
                {
                    label.ForeColor = label.ForeColor == Muted ? Muted : Ink;
                    label.BackColor = Background;
                }
                ApplyChildren(control);
            }
        }

        public static void ApplyEditor(DataGridView grid)
        {
            SettingsPalette palette = For(grid);
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = BorderSubtle;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            FlatGridButtons.Apply(grid);
            grid.ColumnHeadersDefaultCellStyle.Font = grid.Font;
            grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceSubtle;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceSubtle;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Muted;
            grid.DefaultCellStyle.ForeColor = Ink;
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = palette.Selection;
            grid.DefaultCellStyle.SelectionForeColor = Ink;
            grid.RowsDefaultCellStyle.SelectionBackColor = palette.Selection;
            grid.RowsDefaultCellStyle.SelectionForeColor = Ink;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Background;
        }
    }
}
