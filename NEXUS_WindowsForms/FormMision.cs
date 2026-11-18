using NEXUS_WindowsForms.Models;
using NEXUS_WindowsForms.Services;

namespace NEXUS_WindowsForms
{
    public partial class FormMision : Form
    {
        private static readonly Color BackgroundColor = NexusTheme.Background;
        private static readonly Color PanelColor = NexusTheme.Panel;
        private static readonly Color CardColor = NexusTheme.Card;
        private static readonly Color AccentColor = NexusTheme.Accent;
        private static readonly Color TextColor = NexusTheme.Text;
        private static readonly Color MutedTextColor = NexusTheme.MutedText;

        private readonly MissionGame _game = new();
        private Label _phaseLabel = null!;
        private Label _stepLabel = null!;
        private Label _narrativeLabel = null!;
        private Label _routeValueLabel = null!;
        private Label _statusValueLabel = null!;
        private TableLayoutPanel _choicesPanel = null!;
        private Panel _missionProgressFill = null!;
        private Panel _missionProgressTrack = null!;
        private Label _oxygenValueLabel = null!;
        private Label _hullValueLabel = null!;
        private Label _dataValueLabel = null!;
        private Panel _oxygenFill = null!;
        private Panel _hullFill = null!;
        private Panel _dataFill = null!;

        internal event EventHandler? BackRequested;

        /// <summary>
        /// Se dispara al terminar la misión. El resultado queda listo para que
        /// una futura fase lo guarde en el historial.
        /// </summary>
        public event Action<MissionResult>? MissionCompleted;

        public MissionResult? LastResult { get; private set; }

        public FormMision()
        {
            InitializeComponent();
            ConfigureWindow();

            // La pantalla se compone aquí para mantener intacto el archivo Designer.
            while (Controls.Count > 0)
            {
                Control existingControl = Controls[0];
                Controls.Remove(existingControl);
                existingControl.Dispose();
            }
            BuildInterface();
            RenderMission();
        }

        private void ConfigureWindow()
        {
            Text = "NEXUS | Operación Umbral de Eos";
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
                RowCount = 3,
                Padding = new Padding(18)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
            Controls.Add(layout);

            layout.Controls.Add(BuildHeader(), 0, 0);
            layout.Controls.Add(BuildMainArea(), 0, 1);
            layout.Controls.Add(BuildDecisionArea(), 0, 2);
        }

        private Control BuildHeader()
        {
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

            var titleStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };
            titleStack.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            titleStack.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            titleStack.Controls.Add(new Label
            {
                Text = "NEXUS  //  EXPLORACIÓN DE LARGO ALCANCE",
                Dock = DockStyle.Fill,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            }, 0, 0);
            titleStack.Controls.Add(new Label
            {
                Text = "OPERACIÓN UMBRAL DE EOS  •  SECTOR ERIDANI-9",
                Dock = DockStyle.Fill,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            }, 0, 1);
            header.Controls.Add(titleStack, 0, 0);

            var navigationArea = new Panel { Dock = DockStyle.Fill };
            var backButton = NexusTheme.CreateActionButton("VOLVER AL MENÚ");
            backButton.Dock = DockStyle.Right;
            backButton.Width = 170;
            backButton.Margin = new Padding(0);
            backButton.Click += btnVolver_Click;
            navigationArea.Controls.Add(backButton);
            header.Controls.Add(navigationArea, 1, 0);

            return header;
        }

        private Control BuildMainArea()
        {
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 14, 0, 14)
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 69));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));

            var storyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(22)
            };
            _phaseLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            _narrativeLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 13F, FontStyle.Regular),
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(0, 10, 8, 0),
                UseCompatibleTextRendering = true
            };
            storyPanel.Controls.Add(_narrativeLabel);
            storyPanel.Controls.Add(_phaseLabel);
            main.Controls.Add(storyPanel, 0, 0);
            main.Controls.Add(BuildStatusPanel(), 1, 0);

            return main;
        }

        private Control BuildStatusPanel()
        {
            var statusPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14, 12, 14, 8),
                Margin = new Padding(12, 0, 0, 0)
            };

            var content = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = PanelColor,
                Padding = Padding.Empty
            };
            statusPanel.Controls.Add(content);

            content.Controls.Add(new Label
            {
                Text = "ESTADO DE MISIÓN",
                Height = 32,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            });

            var objectiveCard = new Panel
            {
                Height = 102,
                BackColor = CardColor,
                Padding = new Padding(12),
                Margin = new Padding(0, 3, 0, 10)
            };
            objectiveCard.Controls.Add(new Label
            {
                Text = "Investigar la señal y asegurar los datos de la anomalía.",
                Dock = DockStyle.Fill,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                UseCompatibleTextRendering = true
            });
            objectiveCard.Controls.Add(new Label
            {
                Text = "OBJETIVO",
                Dock = DockStyle.Top,
                Height = 23,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            });
            content.Controls.Add(objectiveCard);

            _statusValueLabel = new Label
            {
                Height = 28,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            content.Controls.Add(_statusValueLabel);

            _routeValueLabel = new Label
            {
                Height = 25,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            content.Controls.Add(_routeValueLabel);

            content.Controls.Add(CreateMeterCard("OXÍGENO", AccentColor, out _oxygenValueLabel, out _oxygenFill));
            content.Controls.Add(CreateMeterCard("INTEGRIDAD DEL CASCO", Color.FromArgb(81, 181, 255), out _hullValueLabel, out _hullFill));
            content.Controls.Add(CreateMeterCard("DATOS RECUPERADOS", Color.FromArgb(101, 244, 194), out _dataValueLabel, out _dataFill));

            void ResizeCards(object? sender, EventArgs e)
            {
                int width = Math.Max(190, content.ClientSize.Width - 8);
                foreach (Control control in content.Controls)
                {
                    if (control is Panel panel)
                    {
                        panel.Width = width;
                    }
                    else
                    {
                        control.Width = width;
                    }
                }
            }

            content.SizeChanged += ResizeCards;
            ResizeCards(content, EventArgs.Empty);
            return statusPanel;
        }

        private static Panel CreateMeterCard(string title, Color fillColor, out Label valueLabel, out Panel fill)
        {
            var card = new Panel
            {
                Height = 72,
                BackColor = CardColor,
                Margin = new Padding(0, 0, 0, 9),
                Padding = new Padding(11, 7, 11, 8)
            };

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                AutoEllipsis = true
            };
            valueLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var track = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 8,
                BackColor = Color.FromArgb(29, 48, 70),
                Margin = Padding.Empty
            };
            var barFill = new Panel
            {
                Dock = DockStyle.Left,
                Width = 0,
                BackColor = fillColor
            };
            fill = barFill;
            track.Controls.Add(barFill);
            track.SizeChanged += (_, _) =>
            {
                int value = barFill.Tag is int currentValue ? currentValue : 0;
                barFill.Width = (int)(track.ClientSize.Width * value / 100.0);
            };
            card.Controls.Add(track);
            card.Controls.Add(valueLabel);
            card.Controls.Add(titleLabel);

            return card;
        }

        private Control BuildDecisionArea()
        {
            var area = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0, 0, 0, 2)
            };
            area.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            area.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var progressRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 6)
            };
            progressRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
            progressRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

            var progressBlock = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, 12, 0)
            };
            progressBlock.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            progressBlock.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            progressBlock.Controls.Add(new Label
            {
                Text = "PROGRESO DE LA MISIÓN",
                Dock = DockStyle.Fill,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            _missionProgressTrack = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(29, 48, 70),
                Margin = new Padding(0, 1, 0, 1)
            };
            _missionProgressFill = new Panel
            {
                Dock = DockStyle.Left,
                Width = 0,
                BackColor = AccentColor
            };
            _missionProgressTrack.Controls.Add(_missionProgressFill);
            _missionProgressTrack.SizeChanged += (_, _) => UpdateMissionProgress();
            progressBlock.Controls.Add(_missionProgressTrack, 0, 1);
            progressRow.Controls.Add(progressBlock, 0, 0);

            _stepLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            progressRow.Controls.Add(_stepLabel, 1, 0);
            area.Controls.Add(progressRow, 0, 0);

            _choicesPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BackgroundColor,
                ColumnCount = 1,
                RowCount = 1,
                Margin = Padding.Empty
            };
            area.Controls.Add(_choicesPanel, 0, 1);

            return area;
        }

        private void RenderMission()
        {
            _phaseLabel.Text = _game.PhaseName;
            _narrativeLabel.Text = _game.Narrative;
            _statusValueLabel.Text = _game.StatusText;
            _routeValueLabel.Text = $"RUTA: {_game.RouteName}";
            _oxygenValueLabel.Text = $"{_game.Oxygen}%";
            _hullValueLabel.Text = $"{_game.HullIntegrity}%";
            _dataValueLabel.Text = $"{_game.DataRecovered}%";

            SetMeter(_oxygenFill, _oxygenValueLabel, _game.Oxygen);
            SetMeter(_hullFill, _hullValueLabel, _game.HullIntegrity);
            SetMeter(_dataFill, _dataValueLabel, _game.DataRecovered);
            UpdateMissionProgress();
            RenderChoices();
        }

        private static void SetMeter(Panel fill, Label valueLabel, int value)
        {
            int boundedValue = Math.Clamp(value, 0, 100);
            valueLabel.Text = $"{boundedValue}%";
            fill.Tag = boundedValue;
            Panel? track = fill.Parent as Panel;
            if (track is not null)
            {
                fill.Width = (int)(track.ClientSize.Width * boundedValue / 100.0);
            }
        }

        private void UpdateMissionProgress()
        {
            if (_stepLabel is null || _missionProgressTrack is null || _missionProgressFill is null)
            {
                return;
            }

            _stepLabel.Text = $"ETAPA {_game.ProgressStep} / {_game.TotalSteps}";
            float ratio = (float)_game.ProgressStep / _game.TotalSteps;
            _missionProgressFill.Width = (int)(_missionProgressTrack.ClientSize.Width * ratio);
        }

        private void RenderChoices()
        {
            IReadOnlyList<MissionOption> options = _game.GetOptions();
            _choicesPanel.SuspendLayout();
            while (_choicesPanel.Controls.Count > 0)
            {
                Control oldChoice = _choicesPanel.Controls[0];
                _choicesPanel.Controls.Remove(oldChoice);
                oldChoice.Dispose();
            }
            _choicesPanel.ColumnStyles.Clear();
            _choicesPanel.RowStyles.Clear();
            _choicesPanel.ColumnCount = Math.Max(1, options.Count);
            _choicesPanel.RowCount = 1;

            if (options.Count == 0)
            {
                _choicesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                _choicesPanel.Controls.Add(new Label
                {
                    Text = "La misión ha finalizado.",
                    Dock = DockStyle.Fill,
                    ForeColor = MutedTextColor,
                    TextAlign = ContentAlignment.MiddleCenter
                }, 0, 0);
            }
            else
            {
                float width = 100F / options.Count;
                for (int index = 0; index < options.Count; index++)
                {
                    _choicesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, width));
                    MissionOption option = options[index];
                    var choiceButton = NexusTheme.CreateActionButton(
                        $"{option.Title}\r\n{option.Description}",
                        10F,
                        new Padding(14, 8, 12, 8));
                    choiceButton.Dock = DockStyle.Fill;
                    choiceButton.TextAlign = ContentAlignment.MiddleLeft;
                    choiceButton.UseCompatibleTextRendering = true;
                    choiceButton.AutoEllipsis = true;
                    choiceButton.Margin = new Padding(6, 2, 6, 2);
                    choiceButton.Click += (_, _) => SelectOption(option.Id);
                    _choicesPanel.Controls.Add(choiceButton, index, 0);
                }
            }

            _choicesPanel.ResumeLayout(true);
        }

        private void SelectOption(string optionId)
        {
            if (optionId == "RESTART")
            {
                _game.Reset();
                LastResult = null;
                RenderMission();
                return;
            }

            MissionResult? result = _game.SelectOption(optionId);
            if (result is not null)
            {
                LastResult = result;
                MissionCompleted?.Invoke(result);
            }

            RenderMission();
        }

        private void btnVolver_Click(object? sender, EventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
