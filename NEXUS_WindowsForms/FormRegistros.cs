using NEXUS_WindowsForms.Models;
using NEXUS_WindowsForms.Services;

namespace NEXUS_WindowsForms
{
    public partial class FormRegistros : Form
    {
        private static readonly Color BackgroundColor = Color.FromArgb(5, 12, 28);
        private static readonly Color PanelColor = Color.FromArgb(10, 22, 43);
        private static readonly Color AccentColor = Color.FromArgb(43, 218, 255);
        private static readonly Color TextColor = Color.FromArgb(225, 239, 250);
        private static readonly Color MutedTextColor = Color.FromArgb(139, 170, 195);

        private readonly MissionHistoryStore _historyStore;
        private readonly List<MissionResult> _results = new();
        private DataGridView _historyGrid = null!;
        private TextBox _detailsTextBox = null!;
        private Label _emptyMessageLabel = null!;
        private Control _historyContent = null!;

        internal event EventHandler? BackRequested;

        public FormRegistros() : this(new MissionHistoryStore())
        {
        }

        internal FormRegistros(MissionHistoryStore historyStore)
        {
            _historyStore = historyStore;
            InitializeComponent();
            ConfigureWindow();

            while (Controls.Count > 0)
            {
                Control existingControl = Controls[0];
                Controls.Remove(existingControl);
                existingControl.Dispose();
            }

            BuildInterface();
            RefreshHistory();
        }

        private void ConfigureWindow()
        {
            Text = "NEXUS | Historial de misiones";
            BackColor = BackgroundColor;
            ForeColor = TextColor;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimumSize = new Size(980, 680);
            ClientSize = new Size(1200, 760);
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void BuildInterface()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BackgroundColor,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(18)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColor,
                BorderStyle = BorderStyle.FixedSingle,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(14, 8, 12, 8)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            header.Controls.Add(new Label
            {
                Text = "NEXUS  //  REGISTRO DE EXPLORACIONES",
                Dock = DockStyle.Fill,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            }, 0, 0);

            var navigationArea = new Panel { Dock = DockStyle.Fill };
            var backButton = CreateButton("VOLVER AL MENÚ");
            backButton.Dock = DockStyle.Right;
            backButton.Width = 170;
            backButton.Click += btnVolver_Click;
            navigationArea.Controls.Add(backButton);
            header.Controls.Add(navigationArea, 1, 0);
            layout.Controls.Add(header, 0, 0);

            var body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BackgroundColor,
                Padding = new Padding(0, 14, 0, 0)
            };

            _historyContent = BuildHistoryContent();
            body.Controls.Add(_historyContent);

            _emptyMessageLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColor,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 13F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = true,
                Visible = false
            };
            body.Controls.Add(_emptyMessageLabel);
            layout.Controls.Add(body, 0, 1);
        }

        private Control BuildHistoryContent()
        {
            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BackgroundColor
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));

            var listPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };
            var listTitle = new Label
            {
                Text = "MISIONES FINALIZADAS",
                Dock = DockStyle.Top,
                Height = 38,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _historyGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = PanelColor,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(30, 55, 78),
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 38 }
            };
            _historyGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(14, 30, 55);
            _historyGrid.ColumnHeadersDefaultCellStyle.ForeColor = AccentColor;
            _historyGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(14, 30, 55);
            _historyGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _historyGrid.DefaultCellStyle.BackColor = PanelColor;
            _historyGrid.DefaultCellStyle.ForeColor = TextColor;
            _historyGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(19, 66, 91);
            _historyGrid.DefaultCellStyle.SelectionForeColor = Color.White;
            _historyGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "dateColumn",
                HeaderText = "FECHA",
                FillWeight = 32
            });
            _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "outcomeColumn",
                HeaderText = "DESENLACE",
                FillWeight = 50
            });
            _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "scoreColumn",
                HeaderText = "PUNTOS",
                FillWeight = 18
            });
            _historyGrid.SelectionChanged += HistoryGrid_SelectionChanged;

            listPanel.Controls.Add(_historyGrid);
            listPanel.Controls.Add(listTitle);
            split.Controls.Add(listPanel, 0, 0);

            var detailPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(16),
                Margin = new Padding(12, 0, 0, 0)
            };
            var detailTitle = new Label
            {
                Text = "DETALLES DE LA MISIÓN",
                Dock = DockStyle.Top,
                Height = 38,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _detailsTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = PanelColor,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Padding = new Padding(4),
                TabStop = false
            };
            detailPanel.Controls.Add(_detailsTextBox);
            detailPanel.Controls.Add(detailTitle);
            split.Controls.Add(detailPanel, 1, 0);

            return split;
        }

        internal void RefreshHistory()
        {
            if (!_historyStore.TryLoad(out List<MissionResult> loadedResults, out string errorMessage))
            {
                _results.Clear();
                _historyContent.Visible = false;
                _emptyMessageLabel.Text = errorMessage;
                _emptyMessageLabel.ForeColor = Color.FromArgb(255, 142, 142);
                _emptyMessageLabel.Visible = true;
                _emptyMessageLabel.BringToFront();
                return;
            }

            _results.Clear();
            _results.AddRange(loadedResults);
            _historyGrid.Rows.Clear();
            _detailsTextBox.Clear();

            if (_results.Count == 0)
            {
                _historyContent.Visible = false;
                _emptyMessageLabel.Text = "AÚN NO HAY MISIONES FINALIZADAS\r\n\r\nCompleta una misión para que su resultado aparezca aquí.";
                _emptyMessageLabel.ForeColor = MutedTextColor;
                _emptyMessageLabel.Visible = true;
                _emptyMessageLabel.BringToFront();
                return;
            }

            _emptyMessageLabel.Visible = false;
            _historyContent.Visible = true;
            foreach (MissionResult result in _results)
            {
                int rowIndex = _historyGrid.Rows.Add(
                    result.CompletedAt.ToString("dd/MM/yyyy HH:mm"),
                    result.Outcome,
                    result.Score.ToString());
                _historyGrid.Rows[rowIndex].Tag = result;
            }

            _historyGrid.ClearSelection();
            _historyGrid.Rows[0].Selected = true;
            _historyGrid.CurrentCell = _historyGrid.Rows[0].Cells[0];
            ShowDetails(_results[0]);
        }

        private void HistoryGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_historyGrid.SelectedRows.Count == 0)
            {
                return;
            }

            if (_historyGrid.SelectedRows[0].Tag is MissionResult result)
            {
                ShowDetails(result);
            }
        }

        private void ShowDetails(MissionResult result)
        {
            List<string> decisions = result.Decisions ?? new List<string>();
            string decisionsText = decisions.Count == 0
                ? "Sin decisiones registradas."
                : string.Join(Environment.NewLine, decisions.Select(decision => $"• {decision}"));

            _detailsTextBox.Text =
                $"MISIÓN\r\n{result.MissionName}\r\n\r\n" +
                $"FECHA\r\n{result.CompletedAt:dd/MM/yyyy HH:mm:ss}\r\n\r\n" +
                $"DESENLACE\r\n{result.Outcome}\r\n\r\n" +
                $"INFORME\r\n{result.Summary}\r\n\r\n" +
                $"RECURSOS AL REGRESO\r\n" +
                $"Oxígeno: {result.OxygenRemaining}%\r\n" +
                $"Integridad del casco: {result.HullIntegrity}%\r\n" +
                $"Datos recuperados: {result.DataRecovered}%\r\n" +
                $"Puntuación: {result.Score}\r\n\r\n" +
                $"DECISIONES\r\n{decisionsText}";
        }

        private static Button CreateButton(string text)
        {
            var button = new Button
            {
                Text = text,
                BackColor = Color.FromArgb(14, 30, 55),
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = AccentColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 51, 78);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(14, 76, 99);
            return button;
        }

        private void btnVolver_Click(object? sender, EventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
