using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using System.Diagnostics;
using System.IO;

namespace RadialButton
{
    public sealed class WheelSettingsForm : Form
    {
        private readonly DataGridView _toolsGrid;

        private readonly System.Windows.Forms.ComboBox _configurationCombo;
        private readonly Button _renameButton;
        private readonly Button _deleteButton;
        private readonly Panel _editorPanel;
        private readonly Label _emptyStateLabel;

        private readonly Button _saveButton;
        private readonly Button _cancelButton;
        private readonly Button _advancedSettingsButton;
        private readonly Button _helpButton;
        private readonly Button _addButton;
        private readonly TabControl _tabControl;
        private const string AddTabPageTag = "__ADD_TAB__";

        private readonly List<CommandChoice> _commandChoices;

        private bool _isLoading;
        private string _activeConfigurationName;
        private WheelConfiguration _editingConfiguration;
        private string _activeTabId;
        private CommandSearchPopup _commandSearch;
        private bool _commandSearchPending;
        private string _commandSearchQuery;

        public WheelSettingsForm()
        {
            Text = "RadialButton — Settings";

            ClientSize = new Size(1000, 650);
            MinimumSize = new Size(860, 600);

            StartPosition =
                FormStartPosition.CenterParent;

            FormBorderStyle = FormBorderStyle.Sizable;

            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Icon = IconResources.LoadApplicationIcon();

            Font = new Font(
                "Segoe UI",
                9.0f,
                FontStyle.Regular,
                GraphicsUnit.Point);

            _commandChoices =
                CreateCommandChoices();

            _toolsGrid = CreateToolsGrid();

            _editorPanel = new Panel();
            _editorPanel.Dock = DockStyle.Fill;

            _emptyStateLabel = new Label();
            _emptyStateLabel.Dock = DockStyle.Fill;
            _emptyStateLabel.Text = "Create a new configuration to edit settings.";
            _emptyStateLabel.TextAlign = ContentAlignment.MiddleCenter;
            _emptyStateLabel.Font = new Font("Segoe UI", 11.0f, FontStyle.Regular);
            _emptyStateLabel.ForeColor = SettingsTheme.Muted;
            _emptyStateLabel.BackColor = SettingsTheme.Background;
            _emptyStateLabel.Visible = false;

            _editorPanel.Controls.Add(_toolsGrid);
            _editorPanel.Controls.Add(_emptyStateLabel);

            _configurationCombo = new System.Windows.Forms.ComboBox();
            _configurationCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _configurationCombo.Width = 360;
            _configurationCombo.SelectedIndexChanged += OnConfigurationChanged;

            _renameButton = CreateButton(
                "Rename",
                OnRenameClick);
            _renameButton.Width = 96;

            _deleteButton = CreateButton(
                "Delete",
                OnDeleteClick);
            _deleteButton.Width = 96;

            _saveButton = CreateButton(
                "Save && Close",
                OnSaveClick);
            _saveButton.Margin = new Padding(0, 3, 0, 0);
            ((SettingsButton)_saveButton).Primary = true;
            ((SettingsButton)_deleteButton).Destructive = true;

            _cancelButton = CreateButton(
                "Cancel",
                OnCancelClick);
            _cancelButton.Margin = new Padding(0, 3, 6, 0);

            _advancedSettingsButton = CreateButton(
                "Advanced Settings",
                OnAdvancedSettingsClick);
            _advancedSettingsButton.Width = 146;
            _advancedSettingsButton.Margin = new Padding(0, 3, 6, 0);

            _helpButton = CreateButton("Help", OnHelpClick);
            _helpButton.Width = 84;
            _helpButton.Margin = new Padding(0, 3, 0, 0);

            _addButton = CreateButton(
                "+ Add command",
                OnAddClick);

            _addButton.Width = 132;
            ((SettingsButton)_addButton).AccentLabel = true;

            _tabControl = new SettingsTabs();
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.SelectedIndexChanged += OnTabChanged;
            _tabControl.Selecting += OnTabSelecting;
            _tabControl.MouseDoubleClick += OnTabMouseDoubleClick;

            BuildLayout();
            LoadCurrentConfiguration();
            SettingsTheme.Apply(this);
            // The editor is detached when no configuration exists.
            SettingsTheme.ApplyEditor(_toolsGrid);
        }

        private DataGridView CreateToolsGrid()
        {
            DataGridView grid =
                new DataGridView();

            grid.Dock = DockStyle.Fill;

            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = false;

            grid.SelectionMode =
                DataGridViewSelectionMode.CellSelect;

            grid.EditMode =
                DataGridViewEditMode.EditOnEnter;

            grid.AutoGenerateColumns = false;
            grid.BackgroundColor =
                Color.White;

            grid.BorderStyle =
                BorderStyle.FixedSingle;

            grid.GridColor =
                SettingsTheme.Border;

            grid.EnableHeadersVisualStyles = false;

            grid.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            grid.ColumnHeadersHeight = 42;

            grid.ColumnHeadersDefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor =
                        SettingsTheme.Hover,

                    ForeColor =
                        SettingsTheme.Ink,

                    Font = new Font(
                        "Segoe UI",
                        9.5f,
                        FontStyle.Bold),

                    Alignment =
                        DataGridViewContentAlignment.MiddleLeft,

                    WrapMode =
                        DataGridViewTriState.False,

                    Padding =
                        new Padding(4, 0, 4, 0)
                };

            grid.DefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor = Color.White,

                    ForeColor =
                        SettingsTheme.Ink,

                    SelectionBackColor =
                        SettingsTheme.Selection,

                    SelectionForeColor =
                        SettingsTheme.Ink,

                    Padding =
                        new Padding(4, 0, 4, 0)
                };

            grid.RowTemplate.Height = 31;

            DataGridViewTextBoxColumn positionColumn =
                new DataGridViewTextBoxColumn();

            positionColumn.Name = "Position";
            positionColumn.HeaderText = "Position";
            positionColumn.Width = 76;
            positionColumn.ReadOnly = true;

            positionColumn.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            DataGridViewTextBoxColumn titleColumn =
                new DataGridViewTextBoxColumn();

            titleColumn.Name = "Title";
            titleColumn.HeaderText = "Displayed Name";
            titleColumn.Width = 245;

            DataGridViewComboBoxColumn commandColumn =
                new DataGridViewComboBoxColumn();

            commandColumn.Name = "Command";
            commandColumn.HeaderText = "Command";

            commandColumn.AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill;

            // The editor intentionally contains plain strings only.
            // Command metadata is resolved separately when settings are read.
            commandColumn.ValueType = typeof(string);

            commandColumn.DisplayStyle =
                DataGridViewComboBoxDisplayStyle
                    .DropDownButton;

            commandColumn.FlatStyle =
                FlatStyle.Flat;

            DataGridViewButtonColumn removeColumn =
                new DataGridViewButtonColumn();

            DataGridViewButtonColumn moveUpColumn =
                new DataGridViewButtonColumn();

            moveUpColumn.Name = "MoveUp";
            moveUpColumn.HeaderText = string.Empty;
            moveUpColumn.Width = 36;
            moveUpColumn.AutoSizeMode =
                DataGridViewAutoSizeColumnMode.None;
            moveUpColumn.FlatStyle = FlatStyle.Flat;

            DataGridViewButtonColumn moveDownColumn =
                new DataGridViewButtonColumn();

            moveDownColumn.Name = "MoveDown";
            moveDownColumn.HeaderText = string.Empty;
            moveDownColumn.Width = 36;
            moveDownColumn.AutoSizeMode =
                DataGridViewAutoSizeColumnMode.None;
            moveDownColumn.FlatStyle = FlatStyle.Flat;

            removeColumn.Name = "Remove";
            removeColumn.HeaderText = string.Empty;
            removeColumn.Width = 44;
            removeColumn.AutoSizeMode =
                DataGridViewAutoSizeColumnMode.None;
            removeColumn.FlatStyle = FlatStyle.Flat;

            foreach (CommandChoice choice
                     in _commandChoices)
            {
                commandColumn.Items.Add(choice.DisplayName);
            }

            grid.Columns.Add(positionColumn);
            grid.Columns.Add(titleColumn);
            grid.Columns.Add(commandColumn);
            grid.Columns.Add(moveUpColumn);
            grid.Columns.Add(moveDownColumn);
            grid.Columns.Add(removeColumn);

            grid.CellEndEdit +=
                OnGridCellEndEdit;

            grid.CellBeginEdit += OnCommandBeginEdit;
            grid.KeyPress += OnCommandKeyPress;

            grid.CellContentClick +=
                OnGridCellContentClick;

            return grid;
        }

        private void OnCommandBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (_toolsGrid.Columns[e.ColumnIndex].Name != "Command") return;
            e.Cancel = true;
            QueueCommandSearch(e.RowIndex);
        }

        private void OnCommandKeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar) || _toolsGrid.CurrentCell == null ||
                _toolsGrid.CurrentCell.OwningColumn.Name != "Command") return;
            e.Handled = true;
            _commandSearchQuery = (_commandSearchQuery ?? string.Empty) + e.KeyChar;
            QueueCommandSearch(_toolsGrid.CurrentCell.RowIndex);
        }

        private void QueueCommandSearch(int rowIndex)
        {
            if (_commandSearchPending || _commandSearch != null || rowIndex < 0) return;
            _commandSearchPending = true;
            DataGridViewCell cell = _toolsGrid.Rows[rowIndex].Cells["Command"];
            BeginInvoke(new Action(() =>
            {
                _commandSearchPending = false;
                string query = _commandSearchQuery;
                _commandSearchQuery = null;
                if (IsDisposed || cell.DataGridView != _toolsGrid || !Visible) return;
                Rectangle bounds = _toolsGrid.GetCellDisplayRectangle(cell.ColumnIndex, cell.RowIndex, true);
                var column = (DataGridViewComboBoxColumn)cell.OwningColumn;
                var popup = new CommandSearchPopup(column.Items.Cast<string>(), Convert.ToString(cell.Value),
                    bounds.Width, Font, SettingsTheme.For(this), selected =>
                    {
                        if (IsDisposed || cell.DataGridView != _toolsGrid) return;
                        cell.Value = selected;
                        OnGridCellEndEdit(_toolsGrid, new DataGridViewCellEventArgs(cell.ColumnIndex, cell.RowIndex));
                    });
                _commandSearch = popup;
                popup.Closed += (sender, e) =>
                {
                    _commandSearch = null;
                    // Dispose after the accepting click/key event finishes.
                    if (!IsDisposed) BeginInvoke(new Action(popup.Dispose));
                };
                popup.Show(_toolsGrid, new Point(bounds.Left, bounds.Bottom));
                popup.FocusSearch(query);
            }));
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _commandSearch?.Dispose();
            _commandSearch = null;
            base.OnFormClosed(e);
        }

        private Button CreateButton(
            string text,
            EventHandler clickHandler)
        {
            Button button =
                new SettingsButton();

            button.Text = text;
            button.Width = 104;
            button.Height = 36;
            button.AutoSize = false;

            button.Click += clickHandler;

            return button;
        }

        private void BuildLayout()
        {
            BackColor =
                SettingsTheme.Background;

            TableLayoutPanel mainLayout =
                new TableLayoutPanel();

            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Padding = new Padding(20, 18, 20, 18);
            mainLayout.ColumnCount = 1;
            mainLayout.RowCount = 6;

            mainLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            mainLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            mainLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            mainLayout.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    100.0f));

            mainLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            mainLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            Label descriptionLabel =
                new Label();

            descriptionLabel.AutoSize = true;

            descriptionLabel.Font =
                new Font(
                    "Segoe UI",
                    10.0f,
                    FontStyle.Regular);

            descriptionLabel.ForeColor =
                SettingsTheme.Ink;

            descriptionLabel.Margin =
                new Padding(0, 0, 0, 6);

            descriptionLabel.Text =
                "Choose the Revit or plugin command assigned " +
                "to each radial menu sector.";

            Label hintLabel =
                new Label();

            hintLabel.AutoSize = true;

            hintLabel.ForeColor =
                SettingsTheme.Muted;

            hintLabel.Margin =
                new Padding(0, 0, 0, 14);

            hintLabel.Text =
                "The displayed name changes automatically " +
                "when another command is selected. Use the arrows to reorder rows.";

            TableLayoutPanel configurationPanel = new TableLayoutPanel();
            configurationPanel.Dock = DockStyle.Fill;
            configurationPanel.AutoSize = true;
            configurationPanel.ColumnCount = 3;
            configurationPanel.Margin = new Padding(0, 0, 0, 12);
            configurationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            configurationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            configurationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _configurationCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _configurationCombo.Margin = new Padding(0, 3, 12, 3);
            _renameButton.Margin = new Padding(0, 0, 0, 0);
            _deleteButton.Margin = new Padding(8, 0, 0, 0);

            configurationPanel.Controls.Add(_configurationCombo, 0, 0);
            configurationPanel.Controls.Add(_renameButton, 1, 0);
            configurationPanel.Controls.Add(_deleteButton, 2, 0);

            TableLayoutPanel buttonsPanel = new TableLayoutPanel();
            buttonsPanel.Dock = DockStyle.Fill;
            buttonsPanel.AutoSize = true;
            buttonsPanel.ColumnCount = 3;
            buttonsPanel.Padding = Padding.Empty;
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttonsPanel.Margin = Padding.Empty;

            FlowLayoutPanel leftButtons = new FlowLayoutPanel();
            leftButtons.AutoSize = true;
            leftButtons.WrapContents = false;
            leftButtons.Margin = Padding.Empty;
            leftButtons.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            leftButtons.Controls.Add(_advancedSettingsButton);
            leftButtons.Controls.Add(_helpButton);
            
            FlowLayoutPanel rightButtons = new FlowLayoutPanel();
            rightButtons.AutoSize = true;
            rightButtons.WrapContents = false;
            rightButtons.FlowDirection = FlowDirection.LeftToRight;
            rightButtons.Margin = Padding.Empty;
            rightButtons.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            rightButtons.Controls.Add(_cancelButton);
            rightButtons.Controls.Add(_saveButton);

            buttonsPanel.Controls.Add(leftButtons, 0, 0);
            buttonsPanel.Controls.Add(new Panel(), 1, 0);
            buttonsPanel.Controls.Add(rightButtons, 2, 0);

            FlowLayoutPanel addPanel =
                new FlowLayoutPanel();

            addPanel.Dock = DockStyle.Fill;
            addPanel.AutoSize = true;
            addPanel.FlowDirection = FlowDirection.LeftToRight;
            addPanel.Padding = new Padding(0, 8, 0, 0);
            addPanel.Controls.Add(_addButton);

            mainLayout.Controls.Add(
                descriptionLabel,
                0,
                0);

            mainLayout.Controls.Add(
                hintLabel,
                0,
                1);

            mainLayout.Controls.Add(
                configurationPanel,
                0,
                2);

            mainLayout.Controls.Add(
                _tabControl,
                0,
                3);

            mainLayout.Controls.Add(
                addPanel,
                0,
                4);

            mainLayout.Controls.Add(buttonsPanel, 0, 5);

            Controls.Add(mainLayout);

            AcceptButton = _saveButton;
            CancelButton = _cancelButton;
        }

        private void LoadCurrentConfiguration()
        {
            try
            {
                RefreshConfigurationList();

                List<string> names = WheelConfigurationService.GetConfigurationNames();
                if (names.Count == 0)
                {
                    ShowEmptyState();
                    return;
                }

                WheelConfiguration configuration =
                    WheelConfigurationService.Load();

                _activeConfigurationName =
                    WheelConfigurationService.GetActiveConfigurationName();

                LoadConfiguration(configuration);
                SelectConfigurationInCombo(_activeConfigurationName);
                UpdateConfigurationState();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    "The settings could not be loaded.\n\n" +
                    exception.Message,
                    "RadialButton",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                LoadConfiguration(
                    WheelConfigurationService
                        .CreateDefault());
            }
        }

        private void ShowEmptyState()
        {
            _activeConfigurationName = null;
            _editingConfiguration = null;
            _activeTabId = null;
            _tabControl.TabPages.Clear();
            TabPage emptyPage = new TabPage();
            _tabControl.TabPages.Add(emptyPage);
            _editorPanel.Parent = emptyPage;
            _editorPanel.Dock = DockStyle.Fill;
            _toolsGrid.Rows.Clear();
            SelectConfigurationInCombo(null);
            UpdateConfigurationState();
        }

        private void UpdateConfigurationState()
        {
            bool hasConfiguration = !string.IsNullOrWhiteSpace(_activeConfigurationName);

            _toolsGrid.Enabled = hasConfiguration;
            _emptyStateLabel.Visible = !hasConfiguration;
            if (!hasConfiguration)
            {
                _emptyStateLabel.BringToFront();
            }

            _renameButton.Enabled = hasConfiguration;
            _deleteButton.Enabled = hasConfiguration;
            _saveButton.Enabled = hasConfiguration;
            _advancedSettingsButton.Enabled = true;
            _addButton.Enabled = hasConfiguration && _toolsGrid.Rows.Count < 8;
            _tabControl.Enabled = hasConfiguration;
        }

        private void RefreshConfigurationList()
        {
            _isLoading = true;
            try
            {
                _configurationCombo.Items.Clear();
                foreach (string name in WheelConfigurationService.GetConfigurationNames())
                {
                    _configurationCombo.Items.Add(name);
                }
                _configurationCombo.Items.Add("<Create New>");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SelectConfigurationInCombo(string name)
        {
            _isLoading = true;
            try
            {
                int index = _configurationCombo.Items.IndexOf(name);
                if (index >= 0)
                {
                    _configurationCombo.SelectedIndex = index;
                }
                else
                {
                    _configurationCombo.SelectedIndex = -1;
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void OnConfigurationChanged(object sender, EventArgs e)
        {
            if (_isLoading || _configurationCombo.SelectedItem == null)
            {
                return;
            }

            string selected = Convert.ToString(_configurationCombo.SelectedItem);
            if (selected == "<Create New>")
            {
                CreateNewConfiguration();
                return;
            }

            try
            {
                WheelConfiguration configuration = WheelConfigurationService.Load(selected);
                _activeConfigurationName = selected;
                WheelConfigurationService.SetActiveConfigurationName(selected);
                LoadConfiguration(configuration);
                UpdateConfigurationState();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "RadialButton",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SelectConfigurationInCombo(_activeConfigurationName);
            }
        }

        private void CreateNewConfiguration()
        {
            string name;
            if (!TryPromptForName("Create Configuration", "Configuration name:", string.Empty, out name))
            {
                SelectConfigurationInCombo(_activeConfigurationName);
                return;
            }

            try
            {
                WheelConfigurationService.ValidateConfigurationName(name);
                name = name.Trim();

                if (System.IO.File.Exists(WheelConfigurationService.GetConfigurationPath(name)))
                {
                    MessageBox.Show(this, "Configuration \"" + name + "\" already exists.",
                        "RadialButton", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SelectConfigurationInCombo(_activeConfigurationName);
                    return;
                }

                WheelConfiguration configuration = WheelConfigurationService.CreateDefault();
                WheelConfigurationService.SaveAs(name, configuration);
                WheelConfigurationService.SetActiveConfigurationName(name);
                _activeConfigurationName = name;
                RefreshConfigurationList();
                SelectConfigurationInCombo(name);
                LoadConfiguration(configuration);
                UpdateConfigurationState();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "RadialButton",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SelectConfigurationInCombo(_activeConfigurationName);
            }
        }

        private void OnRenameClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_activeConfigurationName))
            {
                return;
            }

            string newName;
            if (!TryPromptForName("Rename Configuration", "New configuration name:",
                _activeConfigurationName, out newName))
            {
                return;
            }

            try
            {
                WheelConfigurationService.ValidateConfigurationName(newName);
                newName = newName.Trim();
                if (string.Equals(newName, _activeConfigurationName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string oldPath = WheelConfigurationService.GetConfigurationPath(_activeConfigurationName);
                string newPath = WheelConfigurationService.GetConfigurationPath(newName);
                if (System.IO.File.Exists(newPath))
                {
                    DialogResult replace = MessageBox.Show(this,
                        "Configuration \"" + newName + "\" already exists. Replace it?",
                        "RadialButton", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (replace != DialogResult.Yes) return;
                    System.IO.File.Delete(newPath);
                }

                System.IO.File.Move(oldPath, newPath);
                _activeConfigurationName = newName;
                WheelConfigurationService.SetActiveConfigurationName(newName);
                RefreshConfigurationList();
                SelectConfigurationInCombo(newName);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "RadialButton",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnDeleteClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_activeConfigurationName))
            {
                return;
            }

            string name = _activeConfigurationName;
            DialogResult result = MessageBox.Show(
                this,
                "Delete configuration \"" + name + "\"?",
                "RadialButton",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                WheelConfigurationService.DeleteConfiguration(name);
                List<string> remaining = WheelConfigurationService.GetConfigurationNames();

                RefreshConfigurationList();

                if (remaining.Count == 0)
                {
                    WheelConfigurationService.ClearActiveConfiguration();
                    ShowEmptyState();
                    return;
                }

                string nextName = remaining[0];
                WheelConfiguration configuration = WheelConfigurationService.Load(nextName);
                WheelConfigurationService.SetActiveConfigurationName(nextName);
                _activeConfigurationName = nextName;
                SelectConfigurationInCombo(nextName);
                LoadConfiguration(configuration);
                UpdateConfigurationState();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "RadialButton",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadConfiguration(
            WheelConfiguration configuration)
        {
            _isLoading = true;

            try
            {
                _editingConfiguration = configuration;
                _activeTabId = configuration.Tabs.First(value => value.IsMain).Id;
                RefreshTabPages();
                LoadActiveTabGrid();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private WheelTabSettings GetActiveTab()
        {
            if (_editingConfiguration == null) return null;
            return _editingConfiguration.Tabs.FirstOrDefault(
                value => string.Equals(value.Id, _activeTabId, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshTabPages()
        {
            bool oldLoading = _isLoading;
            _isLoading = true;
            try
            {
                _tabControl.TabPages.Clear();
                foreach (WheelTabSettings tab in _editingConfiguration.Tabs)
                {
                    TabPage page = new TabPage(GetTabDisplayName(tab));
                    page.Tag = tab.Id;
                    _tabControl.TabPages.Add(page);
                    if (string.Equals(tab.Id, _activeTabId, StringComparison.OrdinalIgnoreCase))
                        _tabControl.SelectedTab = page;
                }
                TabPage addPage = new TabPage("+");
                addPage.Tag = AddTabPageTag;
                _tabControl.TabPages.Add(addPage);
                AttachEditorToSelectedTab();
                RefreshTabCommandChoices();
            }
            finally { _isLoading = oldLoading; }
        }

        private void AttachEditorToSelectedTab()
        {
            if (_tabControl.SelectedTab != null)
            {
                _editorPanel.Parent = _tabControl.SelectedTab;
                _editorPanel.Dock = DockStyle.Fill;
                SettingsTheme.ApplyEditor(_toolsGrid);
            }
        }

        private void RefreshTabCommandChoices()
        {
            _commandChoices.RemoveAll(value => value.CommandType == WheelCommandType.Tab);
            if (_editingConfiguration != null)
            {
                foreach (WheelTabSettings tab in _editingConfiguration.Tabs)
                    _commandChoices.Add(new CommandChoice(WheelCommandType.Tab, tab.Id,
                        "Tab: " + tab.Name, tab.Name));
            }

            DataGridViewComboBoxColumn column = _toolsGrid.Columns["Command"] as DataGridViewComboBoxColumn;
            if (column != null)
            {
                column.Items.Clear();
                foreach (CommandChoice choice in _commandChoices.OrderBy(value => value.DisplayName, StringComparer.OrdinalIgnoreCase))
                    column.Items.Add(choice.DisplayName);
            }
        }

        private void LoadActiveTabGrid()
        {
            WheelTabSettings tab = GetActiveTab();
            _toolsGrid.Rows.Clear();
            if (tab == null) return;

            foreach (WheelItemSettings item in tab.Items.OrderBy(value => value.Position))
            {
                CommandChoice choice = GetOrCreateChoice(item);
                _toolsGrid.Rows.Add((item.Position + 1).ToString(), item.Title,
                    choice.DisplayName, "↑", "↓", "−");
            }
            RefreshRowState();
        }

        private static string GetTabDisplayName(WheelTabSettings tab)
        {
            string marker = tab.IsMain && tab.IsFamily
                ? " [M/F]"
                : tab.IsMain
                    ? " [M]"
                    : tab.IsFamily ? " [F]" : string.Empty;

            return tab.Name + marker;
        }

        private void SaveGridToActiveTab()
        {
            WheelTabSettings tab = GetActiveTab();
            if (tab != null) tab.Items = ReadItemsFromGrid();
        }

        private void OnTabChanged(object sender, EventArgs e)
        {
            if (_isLoading || _tabControl.SelectedTab == null) return;
            string newTabId = Convert.ToString(_tabControl.SelectedTab.Tag);
            if (string.Equals(newTabId, _activeTabId, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                SaveGridToActiveTab();
                _activeTabId = newTabId;
                _isLoading = true;
                AttachEditorToSelectedTab();
                LoadActiveTabGrid();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "RadialButton", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                foreach (TabPage page in _tabControl.TabPages)
                    if (string.Equals(Convert.ToString(page.Tag), _activeTabId, StringComparison.OrdinalIgnoreCase))
                        _tabControl.SelectedTab = page;
            }
            finally { _isLoading = false; }
        }

        private void OnTabSelecting(object sender, TabControlCancelEventArgs e)
        {
            if (_isLoading || e.TabPage == null ||
                !string.Equals(Convert.ToString(e.TabPage.Tag), AddTabPageTag, StringComparison.Ordinal))
            {
                return;
            }

            e.Cancel = true;
            BeginInvoke(new Action(CreateNewTab));
        }

        private void OnTabMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_isLoading || _editingConfiguration == null) return;

            for (int index = 0; index < _tabControl.TabCount; index++)
            {
                if (!_tabControl.GetTabRect(index).Contains(e.Location)) continue;

                TabPage page = _tabControl.TabPages[index];
                string tabId = Convert.ToString(page.Tag);
                if (string.Equals(tabId, AddTabPageTag, StringComparison.Ordinal)) return;

                WheelTabSettings tab = _editingConfiguration.Tabs.FirstOrDefault(
                    value => string.Equals(value.Id, tabId, StringComparison.OrdinalIgnoreCase));
                if (tab != null) EditTab(tab);
                return;
            }
        }

        private void CreateNewTab()
        {
            string name;
            if (!TryPromptForName("Create Tab", "Tab name:", string.Empty, out name)) return;
            name = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_editingConfiguration.Tabs.Any(value => string.Equals(value.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, "A tab with this name already exists.", "RadialButton", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try { SaveGridToActiveTab(); }
            catch (Exception exception) { MessageBox.Show(this, exception.Message, "RadialButton"); return; }

            WheelTabSettings tab = new WheelTabSettings
            {
                Id = Guid.NewGuid().ToString("N"), Name = name, IsMain = false, IsFamily = false,
                Items = CreateStarterTabItems()
            };
            _editingConfiguration.Tabs.Add(tab);
            _activeTabId = tab.Id;
            RefreshTabPages();
            LoadActiveTabGrid();
            UpdateConfigurationState();
        }

        private List<WheelItemSettings> CreateStarterTabItems()
        {
            CommandChoice first = _commandChoices.First(value =>
                value.CommandType == WheelCommandType.Revit &&
                string.Equals(value.Command, PostableCommand.Move.ToString(), StringComparison.OrdinalIgnoreCase));
            CommandChoice second = _commandChoices.First(value =>
                value.CommandType == WheelCommandType.Revit &&
                string.Equals(value.Command, PostableCommand.Copy.ToString(), StringComparison.OrdinalIgnoreCase));
            return new List<WheelItemSettings>
            {
                new WheelItemSettings { Position = 0, Title = first.DefaultTitle, CommandType = first.CommandType.ToString(), Command = first.Command, CommandLabel = first.DisplayName },
                new WheelItemSettings { Position = 1, Title = second.DefaultTitle, CommandType = second.CommandType.ToString(), Command = second.Command, CommandLabel = second.DisplayName }
            };
        }

        private void EditTab(WheelTabSettings tab)
        {
            while (true)
            {
                using (TabSettingsDialog dialog = new TabSettingsDialog(
                    tab.Name, tab.IsMain, tab.IsFamily, _editingConfiguration.Tabs.Count > 1, Font))
                {
                    DialogResult result = dialog.ShowDialog(this);
                    if (result == DialogResult.Cancel) return;

                    if (dialog.DeleteRequested)
                    {
                        DeleteTab(tab);
                        return;
                    }

                    string name = (dialog.TabName ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        MessageBox.Show(this, "The tab name cannot be empty.", "RadialButton", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }
                    if (_editingConfiguration.Tabs.Any(value => value != tab && string.Equals(value.Name, name, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show(this, "A tab with this name already exists.", "RadialButton", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }
                    if (tab.IsMain && !dialog.SetAsMain)
                    {
                        MessageBox.Show(this, "A configuration must always have a Main Tab. Set another tab as Main first.", "RadialButton", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }
                    if (tab.IsFamily && !dialog.SetAsFamily)
                    {
                        MessageBox.Show(this, "A configuration must always have a Family Tab. Set another tab as Family first.", "RadialButton", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    tab.Name = name;
                    if (dialog.SetAsMain)
                        foreach (WheelTabSettings value in _editingConfiguration.Tabs) value.IsMain = value == tab;
                    if (dialog.SetAsFamily)
                        foreach (WheelTabSettings value in _editingConfiguration.Tabs) value.IsFamily = value == tab;

                    _activeTabId = tab.Id;
                    RefreshTabPages();
                    LoadActiveTabGrid();
                    return;
                }
            }
        }

        private void DeleteTab(WheelTabSettings tab)
        {
            if (_editingConfiguration.Tabs.Count <= 1) return;

            bool hasLinks = _editingConfiguration.Tabs.Any(t => t != tab && t.Items.Any(item =>
                GetSettingsCommandType(item) == WheelCommandType.Tab &&
                string.Equals(item.Command, tab.Id, StringComparison.OrdinalIgnoreCase)));
            string message = "Delete tab \"" + tab.Name + "\"?";
            if (hasLinks) message += "\n\nLinks to this tab from other tabs will also be removed.";
            if (MessageBox.Show(this, message, "RadialButton", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            foreach (WheelTabSettings otherTab in _editingConfiguration.Tabs.Where(value => value != tab))
            {
                otherTab.Items = otherTab.Items
                    .Where(item => GetSettingsCommandType(item) != WheelCommandType.Tab ||
                        !string.Equals(item.Command, tab.Id, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (otherTab.Items.Count < 2)
                {
                    foreach (WheelItemSettings starter in CreateStarterTabItems())
                    {
                        if (otherTab.Items.Count >= 2) break;
                        otherTab.Items.Add(starter);
                    }
                }
                for (int index = 0; index < otherTab.Items.Count; index++) otherTab.Items[index].Position = index;
            }

            bool wasMain = tab.IsMain;
            bool wasFamily = tab.IsFamily;
            _editingConfiguration.Tabs.Remove(tab);
            if (wasMain) _editingConfiguration.Tabs[0].IsMain = true;
            if (wasFamily) _editingConfiguration.Tabs[0].IsFamily = true;
            _activeTabId = _editingConfiguration.Tabs[0].Id;
            RefreshTabPages();
            LoadActiveTabGrid();
            UpdateConfigurationState();
        }

        private WheelConfiguration ReadConfigurationFromGrid()
        {
            // A DataGridViewComboBoxCell can still have its selected value in
            // the active editing control when Save is clicked immediately
            // after choosing a command.  Flush the editor first so the row
            // cells below always contain the current selection.
            CommitPendingGridEdit();

            SaveGridToActiveTab();

            WheelConfiguration configuration = _editingConfiguration;
            configuration.Product = "RadialButton";

            WheelConfigurationService.Validate(configuration);
            return configuration;
        }

        private List<WheelItemSettings> ReadItemsFromGrid()
        {
            CommitPendingGridEdit();
            List<WheelItemSettings> items = new List<WheelItemSettings>();

            for (int index = 0;
                 index < _toolsGrid.Rows.Count;
                 index++)
            {
                DataGridViewRow row =
                    _toolsGrid.Rows[index];

                string title =
                    Convert.ToString(
                        row.Cells["Title"].Value);

                string selectedCommand =
                    Convert.ToString(
                        row.Cells["Command"].Value);

                CommandChoice choice =
                    FindChoice(selectedCommand);

                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new InvalidOperationException(
                        "Sector " +
                        (index + 1) +
                        " does not have a displayed name.");
                }

                if (choice == null)
                {
                    throw new InvalidOperationException(
                        "Sector " +
                        (index + 1) +
                        " does not have a valid command.");
                }

                items.Add(
                    new WheelItemSettings
                    {
                        Position = index,
                        Title = title.Trim(),
                        CommandType =
                            choice.CommandType.ToString(),
                        Command = choice.Command,
                        CommandLabel = choice.DisplayName
                    });
            }

            return items;
        }

        private void CommitPendingGridEdit()
        {
            if (_toolsGrid.IsCurrentCellInEditMode)
            {
                if (_toolsGrid.IsCurrentCellDirty)
                {
                    _toolsGrid.CommitEdit(
                        DataGridViewDataErrorContexts.Commit);
                }

                _toolsGrid.EndEdit();
            }
        }

        private static string CreateDisplayName(
            string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                return string.Empty;
            }

            string result =
                Regex.Replace(
                    commandName,
                    "([a-z0-9])([A-Z])",
                    "$1 $2");

            result =
                Regex.Replace(
                    result,
                    "([A-Z]+)([A-Z][a-z])",
                    "$1 $2");

            return result.Trim();
        }

        private void OnGridCellEndEdit(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (_isLoading ||
                e.RowIndex < 0 ||
                e.ColumnIndex < 0 ||
                _toolsGrid.Columns[e.ColumnIndex].Name != "Command")
            {
                return;
            }

            string selectedCommand =
                Convert.ToString(
                    _toolsGrid.Rows[e.RowIndex]
                        .Cells["Command"].Value);

            CommandChoice choice =
                FindChoice(selectedCommand);

            _toolsGrid.Rows[e.RowIndex].Cells["Title"].Value =
                choice == null
                    ? string.Empty
                    : choice.DefaultTitle;
        }

        private CommandChoice FindChoice(
            string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return null;
            }

            return _commandChoices.FirstOrDefault(
                value => string.Equals(
                    value.DisplayName,
                    displayName,
                    StringComparison.OrdinalIgnoreCase));
        }

        private void OnAddClick(
            object sender,
            EventArgs e)
        {
            if (_toolsGrid.Rows.Count >= 8)
            {
                return;
            }

            _toolsGrid.Rows.Add(
                (_toolsGrid.Rows.Count + 1).ToString(),
                string.Empty,
                null,
                "↑",
                "↓",
                "−");

            RefreshRowState();

            int newRowIndex = _toolsGrid.Rows.Count - 1;
            _toolsGrid.CurrentCell =
                _toolsGrid.Rows[newRowIndex].Cells["Command"];

            _toolsGrid.BeginEdit(true);
        }

        private void OnGridCellContentClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 ||
                e.ColumnIndex < 0)
            {
                return;
            }

            string columnName =
                _toolsGrid.Columns[e.ColumnIndex].Name;

            if (columnName == "MoveUp")
            {
                MoveRow(e.RowIndex, e.RowIndex - 1);
                return;
            }

            if (columnName == "MoveDown")
            {
                MoveRow(e.RowIndex, e.RowIndex + 1);
                return;
            }

            if (columnName == "Remove" &&
                _toolsGrid.Rows.Count > 2)
            {
                _toolsGrid.Rows.RemoveAt(e.RowIndex);
                RefreshRowState();
            }
        }

        private void MoveRow(
            int sourceIndex,
            int targetIndex)
        {
            if (sourceIndex < 0 ||
                targetIndex < 0 ||
                sourceIndex >= _toolsGrid.Rows.Count ||
                targetIndex >= _toolsGrid.Rows.Count ||
                sourceIndex == targetIndex)
            {
                return;
            }

            DataGridViewRow sourceRow =
                _toolsGrid.Rows[sourceIndex];

            DataGridViewRow targetRow =
                _toolsGrid.Rows[targetIndex];

            object sourceTitle =
                sourceRow.Cells["Title"].Value;

            object sourceCommand =
                sourceRow.Cells["Command"].Value;

            _isLoading = true;

            try
            {
                sourceRow.Cells["Title"].Value =
                    targetRow.Cells["Title"].Value;

                sourceRow.Cells["Command"].Value =
                    targetRow.Cells["Command"].Value;

                targetRow.Cells["Title"].Value =
                    sourceTitle;

                targetRow.Cells["Command"].Value =
                    sourceCommand;
            }
            finally
            {
                _isLoading = false;
            }

            RefreshRowState();

            _toolsGrid.ClearSelection();
            _toolsGrid.Rows[targetIndex].Selected = true;
            _toolsGrid.CurrentCell =
                _toolsGrid.Rows[targetIndex].Cells["Title"];
        }

        private void RefreshRowState()
        {
            bool canRemove = _toolsGrid.Rows.Count > 2;

            for (int index = 0;
                 index < _toolsGrid.Rows.Count;
                 index++)
            {
                DataGridViewRow row = _toolsGrid.Rows[index];

                row.Cells["Position"].Value =
                    (index + 1).ToString();

                row.Cells["MoveUp"].Value =
                    index > 0 ? "↑" : string.Empty;

                row.Cells["MoveDown"].Value =
                    index < _toolsGrid.Rows.Count - 1
                        ? "↓"
                        : string.Empty;

                row.Cells["Remove"].Value =
                    canRemove ? "−" : string.Empty;
            }

            _addButton.Enabled = _toolsGrid.Rows.Count < 8;
            UpdateConfigurationState();
        }

        private void OnSaveClick(
            object sender,
            EventArgs e)
        {
            try
            {
                WheelConfiguration configuration =
                    ReadConfigurationFromGrid();

                WheelConfigurationService.Save(
                    configuration);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    exception.Message,
                    "RadialButton",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void OnCancelClick(
            object sender,
            EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnAdvancedSettingsClick(object sender, EventArgs e)
        {
            UserInterfaceSettings settings = UserInterfaceSettingsService.Load();
            using (AdvancedSettingsForm dialog = new AdvancedSettingsForm(
                settings,
                delegate { OnImportClick(this, EventArgs.Empty); },
                delegate { OnExportClick(this, EventArgs.Empty); },
                Font))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    SettingsTheme.Apply(this);
            }
        }

        private void OnHelpClick(object sender, EventArgs e)
        {
            string helpPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RadialButton",
                "Help",
                "Help.html");

            if (!File.Exists(helpPath))
            {
                MessageBox.Show(
                    this,
                    "Help.html was not found:\n\n" + helpPath,
                    "RadialButton",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = helpPath,
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    "The help page could not be opened.\n\n" + exception.Message,
                    "RadialButton",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnImportClick(
            object sender,
            EventArgs e)
        {
            using (OpenFileDialog dialog =
                   new OpenFileDialog())
            {
                dialog.Title =
                    "Import RadialButton Configuration";

                dialog.Filter =
                    "JSON configuration (*.json)|*.json|" +
                    "All files (*.*)|*.*";

                dialog.CheckFileExists = true;
                dialog.Multiselect = false;

                if (dialog.ShowDialog(this) !=
                    DialogResult.OK)
                {
                    return;
                }

                try
                {
                    WheelConfiguration configuration =
                        WheelConfigurationService
                            .LoadFromFile(
                                dialog.FileName);

                    string name =
                        System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);

                    WheelConfigurationService.ValidateConfigurationName(name);
                    string targetPath = WheelConfigurationService.GetConfigurationPath(name);

                    if (System.IO.File.Exists(targetPath) &&
                        !string.Equals(System.IO.Path.GetFullPath(targetPath),
                            System.IO.Path.GetFullPath(dialog.FileName),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        DialogResult replace = MessageBox.Show(
                            this,
                            "Configuration \"" + name + "\" already exists. Replace it?",
                            "RadialButton",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (replace != DialogResult.Yes)
                        {
                            return;
                        }
                    }

                    WheelConfigurationService.SaveAs(name, configuration);
                    WheelConfigurationService.SetActiveConfigurationName(name);
                    _activeConfigurationName = name;
                    RefreshConfigurationList();
                    SelectConfigurationInCombo(name);
                    LoadConfiguration(configuration);
                    UpdateConfigurationState();
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        this,
                        "The settings could not be imported.\n\n" +
                        exception.Message,
                        "RadialButton",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void OnExportClick(
            object sender,
            EventArgs e)
        {
            try
            {
                WheelConfiguration configuration =
                    ReadConfigurationFromGrid();

                using (SaveFileDialog dialog =
                       new SaveFileDialog())
                {
                    dialog.Title =
                        "Export RadialButton Configuration";

                    dialog.Filter =
                        "JSON configuration (*.json)|*.json";

                    dialog.DefaultExt = "json";
                    dialog.AddExtension = true;

                    dialog.FileName =
                        string.IsNullOrWhiteSpace(_activeConfigurationName)
                            ? "RadialButton"
                            : _activeConfigurationName;

                    if (dialog.ShowDialog(this) !=
                        DialogResult.OK)
                    {
                        return;
                    }

                    WheelConfigurationService.SaveToFile(
                        configuration,
                        dialog.FileName);

                    MessageBox.Show(
                        this,
                        "The settings were exported successfully.",
                        "RadialButton",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    exception.Message,
                    "RadialButton",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private bool TryPromptForName(
            string title,
            string labelText,
            string initialValue,
            out string value)
        {
            value = null;

            using (Form prompt = new Form())
            using (Label label = new Label())
            using (System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox())
            using (Button okButton = new SettingsButton { Primary = true })
            using (Button cancelButton = new SettingsButton())
            {
                prompt.Text = title;
                prompt.Width = 390;
                prompt.Height = 165;
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;
                prompt.ShowInTaskbar = false;
                prompt.Font = Font;

                label.Text = labelText;
                label.AutoSize = true;
                label.Left = 16;
                label.Top = 16;

                textBox.Left = 16;
                textBox.Top = 42;
                textBox.Width = 342;
                textBox.Text = initialValue ?? string.Empty;

                okButton.Text = "OK";
                okButton.Width = 82;
                okButton.Height = 30;
                okButton.Left = 188;
                okButton.Top = 78;
                okButton.DialogResult = DialogResult.OK;

                cancelButton.Text = "Cancel";
                cancelButton.Width = 82;
                cancelButton.Height = 30;
                cancelButton.Left = 276;
                cancelButton.Top = 78;
                cancelButton.DialogResult = DialogResult.Cancel;

                prompt.Controls.Add(label);
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(okButton);
                prompt.Controls.Add(cancelButton);
                prompt.AcceptButton = okButton;
                prompt.CancelButton = cancelButton;
                SettingsTheme.Apply(prompt);

                textBox.SelectAll();
                if (prompt.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }

                value = textBox.Text;
                return true;
            }
        }

        private static List<CommandChoice>
            CreateCommandChoices()
        {
            List<CommandChoice> result =
                new List<CommandChoice>();

            string[] commandNames =
                Enum.GetNames(
                    typeof(PostableCommand));

            foreach (string commandName
                     in commandNames)
            {
                result.Add(
                    new CommandChoice(
                        WheelCommandType.Revit,
                        commandName,
                        commandName,
                        CreateDisplayName(commandName)));
            }

            try
            {
                foreach (ExternalCommandInfo command
                         in ExternalCommandCatalog.GetCommands())
                {
                    result.Add(
                        new CommandChoice(
                            WheelCommandType.External,
                            command.CommandId,
                            command.DisplayName,
                            command.ButtonName));
                }
            }
            catch
            {
                // Built-in commands remain available even when a Revit
                // ribbon item from another add-in cannot be inspected.
            }

            return result
                .OrderBy(
                    value => value.DisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private CommandChoice GetOrCreateChoice(
            WheelItemSettings item)
        {
            WheelCommandType commandType =
                GetSettingsCommandType(item);

            CommandChoice choice =
                _commandChoices.FirstOrDefault(
                    value =>
                        value.CommandType == commandType &&
                        string.Equals(
                            value.Command,
                            item.Command,
                            StringComparison.OrdinalIgnoreCase));

            if (choice != null)
            {
                return choice;
            }

            string label =
                !string.IsNullOrWhiteSpace(item.CommandLabel)
                    ? item.CommandLabel
                    : item.Command;

            choice =
                new CommandChoice(
                    commandType,
                    item.Command,
                    label,
                    item.Title);

            _commandChoices.Add(choice);

            DataGridViewComboBoxColumn commandColumn =
                _toolsGrid.Columns["Command"]
                    as DataGridViewComboBoxColumn;

            if (commandColumn != null)
            {
                commandColumn.Items.Add(choice.DisplayName);
            }

            return choice;
        }

        private static WheelCommandType GetSettingsCommandType(
            WheelItemSettings item)
        {
            if (string.IsNullOrWhiteSpace(item.CommandType))
            {
                return WheelCommandType.Revit;
            }

            WheelCommandType commandType;

            if (!Enum.TryParse(
                    item.CommandType,
                    true,
                    out commandType))
            {
                throw new InvalidOperationException(
                    "Unknown command type: " +
                    item.CommandType);
            }

            return commandType;
        }

        private sealed class TabSettingsDialog : Form
        {
            private readonly System.Windows.Forms.TextBox _nameTextBox;
            private readonly CheckBox _mainCheckBox;
            private readonly CheckBox _familyCheckBox;

            public string TabName { get { return _nameTextBox.Text; } }
            public bool SetAsMain { get { return _mainCheckBox.Checked; } }
            public bool SetAsFamily { get { return _familyCheckBox.Checked; } }
            public bool DeleteRequested { get; private set; }

            public TabSettingsDialog(
                string tabName,
                bool isMain,
                bool isFamily,
                bool canDelete,
                Font ownerFont)
            {
                Text = "Tab Settings";
                ClientSize = new Size(430, 205);
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                Font = ownerFont;

                _nameTextBox = new System.Windows.Forms.TextBox();
                _nameTextBox.Location = new Point(18, 18);
                _nameTextBox.Size = new Size(394, 24);
                _nameTextBox.Text = tabName ?? string.Empty;

                _mainCheckBox = new CheckBox();
                _mainCheckBox.Text = "Set as Main Tab";
                _mainCheckBox.AutoSize = true;
                _mainCheckBox.Location = new Point(18, 60);
                _mainCheckBox.Checked = isMain;

                _familyCheckBox = new CheckBox();
                _familyCheckBox.Text = "Set as Family Tab";
                _familyCheckBox.AutoSize = true;
                _familyCheckBox.Location = new Point(18, 86);
                _familyCheckBox.Checked = isFamily;

                Button deleteButton = new SettingsButton { Destructive = true };
                deleteButton.Text = "Delete";
                deleteButton.Size = new Size(88, 32);
                deleteButton.Location = new Point(18, 155);
                deleteButton.ForeColor = Color.FromArgb(185, 38, 38);
                deleteButton.Enabled = canDelete;
                deleteButton.Click += delegate
                {
                    DeleteRequested = true;
                    DialogResult = DialogResult.Abort;
                    Close();
                };

                Button okButton = new SettingsButton { Primary = true };
                okButton.Text = "OK";
                okButton.Size = new Size(88, 32);
                okButton.Location = new Point(224, 155);
                okButton.DialogResult = DialogResult.OK;

                Button cancelButton = new SettingsButton();
                cancelButton.Text = "Cancel";
                cancelButton.Size = new Size(88, 32);
                cancelButton.Location = new Point(324, 155);
                cancelButton.DialogResult = DialogResult.Cancel;

                Controls.Add(_nameTextBox);
                Controls.Add(_mainCheckBox);
                Controls.Add(_familyCheckBox);
                Controls.Add(deleteButton);
                Controls.Add(okButton);
                Controls.Add(cancelButton);

                AcceptButton = okButton;
                CancelButton = cancelButton;
                SettingsTheme.Apply(this);
                _nameTextBox.SelectAll();
            }
        }

        private sealed class CommandChoice
        {
            public WheelCommandType CommandType { get; private set; }

            public string Command { get; private set; }

            public string DisplayName { get; private set; }

            public string DefaultTitle { get; private set; }

            public CommandChoice(
                WheelCommandType commandType,
                string command,
                string displayName,
                string defaultTitle)
            {
                CommandType = commandType;
                Command = command;
                DisplayName = displayName;
                DefaultTitle = defaultTitle;
            }
        }
    }
}
