using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RadialButton
{
    public sealed class AdvancedSettingsForm : Form
    {
        private readonly UserInterfaceSettings _settings;
        private readonly ComboBox _schemeCombo;
        private readonly TrackBar _scaleTrackBar;
        private readonly Label _scaleValueLabel;
        private readonly Button _backButtonCapture;
        private readonly Panel _previewPanel;
        private bool _scaleChanged;
        private bool _isCapturingBackButton;
        private string _capturedBackButton;

        public AdvancedSettingsForm(
            UserInterfaceSettings settings,
            Action importConfiguration,
            Action exportConfiguration,
            Font ownerFont)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            Text = "RadialButton — Advanced Settings";
            ClientSize = new Size(560, 370);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = true;
            ShowIcon = false;
            ShowInTaskbar = false;
            Font = ownerFont;
            AutoScaleMode = AutoScaleMode.Dpi;

            _schemeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            foreach (WheelColorScheme scheme in _settings.ColorSchemes)
                _schemeCombo.Items.Add(scheme.Name);
            _schemeCombo.SelectedItem = _settings.SelectedColorScheme;
            if (_schemeCombo.SelectedIndex < 0 && _schemeCombo.Items.Count > 0)
                _schemeCombo.SelectedIndex = 0;

            _previewPanel = new Panel
            {
                Width = 104,
                Height = 28,
                BorderStyle = BorderStyle.FixedSingle
            };
            _previewPanel.Paint += OnPreviewPaint;
            _schemeCombo.SelectedIndexChanged += delegate
            {
                _previewPanel.Invalidate();
                ApplySelectedScheme();
            };

            _scaleTrackBar = new TrackBar
            {
                Minimum = 10,
                Maximum = 40,
                TickFrequency = 5,
                SmallChange = 1,
                LargeChange = 5,
                Width = 220,
                AutoSize = false,
                Height = 34
            };
            _scaleValueLabel = new Label
            {
                AutoSize = false,
                Width = 58,
                Height = 34,
                TextAlign = ContentAlignment.MiddleLeft
            };
            double displayedScale = Math.Max(0.5, Math.Min(2.0, _settings.WheelScale));
            _scaleTrackBar.Value = (int)Math.Round(displayedScale * 20.0);
            UpdateScaleValueLabel();
            _scaleTrackBar.ValueChanged += delegate
            {
                _scaleChanged = true;
                UpdateScaleValueLabel();
            };
            _scaleChanged = false;

            _capturedBackButton = string.IsNullOrWhiteSpace(_settings.KeyboardBackButton)
                ? "None"
                : _settings.KeyboardBackButton;
            _backButtonCapture = CreateButton(GetBackButtonDisplayText(_capturedBackButton));
            _backButtonCapture.Width = 220;
            _backButtonCapture.Click += OnBackButtonCaptureClick;
            _backButtonCapture.KeyDown += OnBackButtonCaptureKeyDown;

            BuildLayout(importConfiguration, exportConfiguration);
            ApplySelectedScheme();
        }

        private void ApplySelectedScheme()
        {
            WheelColorScheme scheme = _settings.ColorSchemes.Find(value =>
                string.Equals(value.Name, Convert.ToString(_schemeCombo.SelectedItem),
                    StringComparison.OrdinalIgnoreCase)) ?? _settings.GetSelectedScheme();
            SettingsTheme.Apply(this, scheme);
        }

        private void BuildLayout(Action importConfiguration, Action exportConfiguration)
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 16),
                ColumnCount = 3,
                RowCount = 6
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label note = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(510, 0),
                ForeColor = SettingsTheme.Muted,
                Text = "Color schemes and unrestricted scale values can also be edited in ui-settings.json."
            };
            layout.SetColumnSpan(note, 3);
            layout.Controls.Add(note, 0, 0);

            AddRow(layout, 1, "Color scheme:", _schemeCombo);
            _previewPanel.Anchor = AnchorStyles.Left;
            _previewPanel.Margin = new Padding(10, 6, 0, 6);
            layout.Controls.Add(_previewPanel, 2, 1);

            FlowLayoutPanel scalePanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };
            scalePanel.Controls.Add(_scaleTrackBar);
            scalePanel.Controls.Add(_scaleValueLabel);
            AddRow(layout, 2, "Wheel scale:", scalePanel);
            AddRow(layout, 3, "Back button:", _backButtonCapture);

            FlowLayoutPanel transfer = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty
            };
            Button importButton = CreateButton("Import Configuration");
            importButton.Width = 160;
            importButton.Click += delegate { if (importConfiguration != null) importConfiguration(); };
            Button exportButton = CreateButton("Export Configuration");
            exportButton.Width = 160;
            exportButton.Click += delegate { if (exportConfiguration != null) exportConfiguration(); };
            transfer.Controls.Add(importButton);
            transfer.Controls.Add(exportButton);

            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Margin = Padding.Empty
            };
            Button cancelButton = CreateButton("Cancel");
            cancelButton.DialogResult = DialogResult.Cancel;
            Button okButton = CreateButton("OK");
            ((SettingsButton)okButton).Primary = true;
            okButton.Click += OnOkClick;
            actions.Controls.Add(cancelButton);
            actions.Controls.Add(okButton);

            TableLayoutPanel bottomPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 14, 0, 0)
            };
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottomPanel.Controls.Add(transfer, 0, 0);
            bottomPanel.Controls.Add(actions, 1, 0);

            layout.SetColumnSpan(bottomPanel, 3);
            layout.Controls.Add(bottomPanel, 0, 5);

            Controls.Add(layout);
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private static void AddRow(TableLayoutPanel layout, int row, string labelText, Control control)
        {
            Label label = new Label
            {
                Text = labelText,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 8, 8)
            };
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            control.Margin = new Padding(0, 5, 0, 5);
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private static Button CreateButton(string text)
        {
            return new SettingsButton { Text = text, Width = 104, Height = 36 };
        }

        private void UpdateScaleValueLabel()
        {
            _scaleValueLabel.Text = (_scaleTrackBar.Value / 20.0).ToString("0.00") + "x";
        }

        private void OnBackButtonCaptureClick(object sender, EventArgs e)
        {
            _isCapturingBackButton = true;
            _backButtonCapture.Text = "Press a key...";
            _backButtonCapture.Focus();
        }

        private void OnBackButtonCaptureKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isCapturingBackButton) return;
            e.Handled = true;
            e.SuppressKeyPress = true;

            if (e.KeyCode == Keys.Escape)
            {
                _isCapturingBackButton = false;
                _backButtonCapture.Text = GetBackButtonDisplayText(_capturedBackButton);
                return;
            }

            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
                e.KeyCode == Keys.Menu) return;

            _capturedBackButton = e.KeyCode.ToString();
            _isCapturingBackButton = false;
            _backButtonCapture.Text = GetBackButtonDisplayText(_capturedBackButton);
        }

        private static string GetBackButtonDisplayText(string key)
        {
            if (string.Equals(key, "Back", StringComparison.OrdinalIgnoreCase))
                return "Backspace";
            return string.IsNullOrWhiteSpace(key) ? "None" : key;
        }

        private void OnPreviewPaint(object sender, PaintEventArgs e)
        {
            WheelColorScheme scheme = _settings.ColorSchemes.Find(value =>
                string.Equals(value.Name, Convert.ToString(_schemeCombo.SelectedItem),
                    StringComparison.OrdinalIgnoreCase)) ?? _settings.GetSelectedScheme();

            Rectangle left = new Rectangle(0, 0, _previewPanel.ClientSize.Width / 2,
                _previewPanel.ClientSize.Height);
            Rectangle right = new Rectangle(left.Right, 0,
                _previewPanel.ClientSize.Width - left.Width, _previewPanel.ClientSize.Height);
            using (Brush normal = new SolidBrush(scheme.ReadColor(scheme.SectorColor, Color.DimGray)))
            using (Brush hover = new SolidBrush(scheme.ReadColor(scheme.SectorHoverColor, Color.SteelBlue)))
            {
                e.Graphics.FillRectangle(normal, left);
                e.Graphics.FillRectangle(hover, right);
            }
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            _settings.SelectedColorScheme = Convert.ToString(_schemeCombo.SelectedItem);
            if (_scaleChanged) _settings.WheelScale = _scaleTrackBar.Value / 20.0;
            _settings.KeyboardBackButton = _capturedBackButton;
            _settings.MouseBackButton = "None";

            try
            {
                UserInterfaceSettingsService.Save(_settings);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The interface settings could not be saved.\n\n" + exception.Message,
                    "RadialButton", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
