using NEXUS_WindowsForms.Models;
using NEXUS_WindowsForms.Services;
using NEXUS_WindowsForms.Controls;
using System.Diagnostics;
using System.Text.Json;

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
        private readonly DatabaseService _db = new();
        private readonly HashSet<Keys> _heldKeys = new();
        private readonly System.Windows.Forms.Timer _gameTimer = new() { Interval = 16 };
        private readonly System.Windows.Forms.Timer _autosaveTimer = new() { Interval = 1200 };
        private readonly Stopwatch _frameClock = Stopwatch.StartNew();
        private int? _currentExpeditionId;
        private string _currentCadetName = "Cadete Vance";
        private bool _pendingAutoSave;

        private Label _phaseLabel = null!;
        private Label _stepLabel = null!;
        private Label _narrativeLabel = null!;
        private Label _routeValueLabel = null!;
        private Label _statusValueLabel = null!;
        private Label _saveStatusLabel = null!;
        private TableLayoutPanel _choicesPanel = null!;
        private Panel _missionProgressFill = null!;
        private Panel _missionProgressTrack = null!;
        private Label _oxygenValueLabel = null!;
        private Label _hullValueLabel = null!;
        private Label _dataValueLabel = null!;
        private Label _objectiveValueLabel = null!;
        private Panel _oxygenFill = null!;
        private Panel _hullFill = null!;
        private Panel _dataFill = null!;
        private Panel _storyContentPanel = null!;
        private ExplorationView _explorationView = null!;

        internal event EventHandler? BackRequested;

        public event Action<MissionResult>? MissionCompleted;

        public MissionResult? LastResult { get; private set; }

        public FormMision()
        {
            InitializeComponent();
            ConfigureWindow();

            while (Controls.Count > 0)
            {
                Control existingControl = Controls[0];
                Controls.Remove(existingControl);
                existingControl.Dispose();
            }
            BuildInterface();
            KeyPreview = true;
            KeyDown += FormMision_KeyDown;
            KeyUp += FormMision_KeyUp;
            Deactivate += (_, _) => _heldKeys.Clear();
            _gameTimer.Tick += GameTimer_Tick;
            _autosaveTimer.Tick += (_, _) =>
            {
                _autosaveTimer.Stop();
                if (_pendingAutoSave)
                {
                    SaveCurrentExpedition(showDialog: false);
                }
            };
            VisibleChanged += (_, _) =>
            {
                if (!Visible)
                {
                    StopGameLoop();
                }
                else if (_game.CurrentStageKey == "Exploration")
                {
                    StartGameLoop();
                }
            };
            FormClosing += (_, _) =>
            {
                StopGameLoop();
                _autosaveTimer.Stop();
                if (_game.CurrentStageKey != "Completed")
                {
                    SaveCurrentExpedition(showDialog: false);
                }
            };
            Disposed += (_, _) =>
            {
                _gameTimer.Dispose();
                _autosaveTimer.Dispose();
            };
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
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));

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
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
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

            var navigationArea = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 4)
            };

            var backButton = NexusTheme.CreateActionButton("VOLVER AL MENÚ");
            backButton.Width = 155;
            backButton.Height = 42;
            backButton.Margin = new Padding(6, 0, 0, 0);
            backButton.Click += btnVolver_Click;

            var saveButton = NexusTheme.CreateActionButton("GUARDAR PARTIDA");
            saveButton.Width = 165;
            saveButton.Height = 42;
            saveButton.Margin = new Padding(6, 0, 0, 0);
            saveButton.Click += (_, _) => SaveCurrentExpedition(showDialog: true);

            var loadButton = NexusTheme.CreateActionButton("CARGAR PARTIDA");
            loadButton.Width = 160;
            loadButton.Height = 42;
            loadButton.Margin = new Padding(6, 0, 0, 0);
            loadButton.Click += (_, _) => OpenLoadExpeditionDialog();

            navigationArea.Controls.Add(backButton);
            navigationArea.Controls.Add(saveButton);
            navigationArea.Controls.Add(loadButton);
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
            _explorationView = new ExplorationView
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Margin = new Padding(0)
            };
            _explorationView.StateChanged += (_, _) =>
            {
                _objectiveValueLabel.Text = _explorationView.ObjectiveSummary;
                _missionProgressFill.Invalidate();
                ScheduleAutoSave();
            };
            _explorationView.ActionCompleted += ExplorationView_ActionCompleted;
            _storyContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColor,
                Margin = new Padding(0)
            };
            _storyContentPanel.Controls.Add(_narrativeLabel);
            _storyContentPanel.Controls.Add(_explorationView);
            _saveStatusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                ForeColor = Color.FromArgb(101, 244, 194),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "SQL SERVER: LISTO PARA GUARDAR EXPEDICIÓN"
            };
            storyPanel.Controls.Add(_storyContentPanel);
            storyPanel.Controls.Add(_saveStatusLabel);
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
            _objectiveValueLabel = new Label
            {
                Text = "Investigar la señal y asegurar los datos de la anomalía.",
                Dock = DockStyle.Fill,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                UseCompatibleTextRendering = true,
                Padding = new Padding(0, 4, 0, 0)
            };
            objectiveCard.Controls.Add(_objectiveValueLabel);
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
                    control.Width = width;
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
            bool isExploringShip = _game.CurrentStageKey == "Exploration";
            _explorationView.Visible = isExploringShip;
            _narrativeLabel.Visible = !isExploringShip;
            if (isExploringShip)
            {
                _explorationView.BringToFront();
                _objectiveValueLabel.Text = _explorationView.ObjectiveSummary;
            }
            else
            {
                _narrativeLabel.BringToFront();
                _objectiveValueLabel.Text = "Investigar la señal y asegurar los datos de la anomalía.";
            }
            _statusValueLabel.Text = _game.StatusText;
            _routeValueLabel.Text = $"RUTA: {_game.RouteName}";
            _oxygenValueLabel.Text = $"{_game.Oxygen}%";
            _hullValueLabel.Text = $"{_game.HullIntegrity}%";
            _dataValueLabel.Text = $"{_game.DataRecovered}%";

            _explorationView.SetMissionVitals(_game.Oxygen, _game.HullIntegrity, _game.DataRecovered);

            _explorationView.SetMissionVitals(_game.Oxygen, _game.HullIntegrity, _game.DataRecovered);

            SetMeter(_oxygenFill, _oxygenValueLabel, _game.Oxygen);
            SetMeter(_hullFill, _hullValueLabel, _game.HullIntegrity);
            SetMeter(_dataFill, _dataValueLabel, _game.DataRecovered);
            UpdateMissionProgress();
            RenderChoices();
            if (isExploringShip && Visible)
            {
                StartGameLoop();
            }
            else
            {
                StopGameLoop();
            }
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
                    Text = _game.CurrentStageKey == "Exploration"
                        ? "W A S D / FLECHAS: MOVER    •    E: INTERACTUAR"
                        : "La misión ha finalizado.",
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
                _explorationView.RestoreState(new ExplorationSaveState());
                _currentExpeditionId = null;
                LastResult = null;
                _saveStatusLabel.Text = "SQL SERVER: NUEVA EXPEDICIÓN INICIADA";
                RenderMission();
                return;
            }

            MissionResult? result = _game.SelectOption(optionId);
            if (result is not null)
            {
                LastResult = result;
                try
                {
                    _db.GuardarResultadoMision(result, _currentExpeditionId, _currentCadetName);
                    _saveStatusLabel.ForeColor = Color.FromArgb(101, 244, 194);
                    _saveStatusLabel.Text = $"SQL SERVER: MISIÓN COMPLETADA Y REGISTRADA EN BITÁCORA ({DateTime.Now:HH:mm:ss})";
                }
                catch (Exception ex)
                {
                    _saveStatusLabel.ForeColor = NexusTheme.Error;
                    _saveStatusLabel.Text = $"ERROR SQL AL GUARDAR RESULTADO: {ex.Message}";
                }

                _currentExpeditionId = null;
                MissionCompleted?.Invoke(result);
            }
            else
            {
                SaveCurrentExpedition(showDialog: false);
            }

            RenderMission();
        }

        private void SaveCurrentExpedition(bool showDialog)
        {
            if (_game.CurrentStageKey == "Completed")
            {
                if (showDialog)
                {
                    MessageBox.Show(
                        "La misión actual ya finalizó y su resultado fue registrado en la Bitácora de SQL Server.",
                        "NEXUS // Bitácora",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                return;
            }

            try
            {
                string slotName = $"Expedición Eridani-9 ({_game.PhaseName})";
                _currentExpeditionId = _db.GuardarExpedicionEnCurso(
                    _currentExpeditionId,
                    _currentCadetName,
                    slotName,
                    _game.CurrentStageKey,
                    _game.Oxygen,
                    _game.HullIntegrity,
                    _game.DataRecovered,
                    _game.ScannedFromOrbit,
                    _game.DescendedEarly,
                    _game.TrustedSignal,
                    _game.SentProbe,
                    _game.Decisions,
                    JsonSerializer.Serialize(_explorationView.State));

                _saveStatusLabel.ForeColor = Color.FromArgb(101, 244, 194);
                _saveStatusLabel.Text = $"SQL SERVER: PARTIDA GUARDADA (ID #{_currentExpeditionId} • {DateTime.Now:HH:mm:ss})";
                _pendingAutoSave = false;

                if (showDialog)
                {
                    MessageBox.Show(
                        $"Expedición guardada correctamente en SQL Server.\n\nRanura ID: #{_currentExpeditionId}\nEtapa: {_game.PhaseName}\nOxígeno: {_game.Oxygen}% | Casco: {_game.HullIntegrity}% | Datos: {_game.DataRecovered}%",
                        "NEXUS // Partida Guardada",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _saveStatusLabel.ForeColor = NexusTheme.Error;
                _saveStatusLabel.Text = $"ERROR SQL: {ex.Message}";
                _pendingAutoSave = false;
                if (showDialog)
                {
                    MessageBox.Show($"No se pudo guardar en SQL Server:\n{ex.Message}", "Error SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OpenLoadExpeditionDialog()
        {
            try
            {
                List<SavedExpeditionInfo> savedList = _db.ObtenerExpedicionesEnCurso();
                if (savedList.Count == 0)
                {
                    MessageBox.Show(
                        "No hay expediciones en curso guardadas en SQL Server.\nInicia una misión y avanza una etapa o presiona 'GUARDAR PARTIDA'.",
                        "NEXUS // Cargar Partida",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                using var picker = new Form
                {
                    Text = "NEXUS // Seleccionar Expedición Guardada",
                    Size = new Size(640, 380),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = BackgroundColor,
                    ForeColor = TextColor,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var listBox = new ListBox
                {
                    Dock = DockStyle.Fill,
                    BackColor = PanelColor,
                    ForeColor = TextColor,
                    Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                    BorderStyle = BorderStyle.FixedSingle
                };

                foreach (SavedExpeditionInfo exp in savedList)
                {
                    listBox.Items.Add($"[ID #{exp.IdExpedicion}] {exp.NombreCadete} — Etapa: {exp.EtapaActual} | O2:{exp.Oxigeno}% Casco:{exp.IntegridadCasco}% Datos:{exp.DatosRecuperados}% ({exp.FechaUltimoGuardado:dd/MM HH:mm})");
                }
                listBox.SelectedIndex = 0;

                var confirmBtn = NexusTheme.CreateActionButton("CARGAR EXPEDICIÓN SELECCIONADA");
                confirmBtn.Dock = DockStyle.Bottom;
                confirmBtn.Height = 46;
                confirmBtn.Click += (_, _) =>
                {
                    if (listBox.SelectedIndex >= 0)
                    {
                        SavedExpeditionInfo chosen = savedList[listBox.SelectedIndex];
                        _currentExpeditionId = chosen.IdExpedicion;
                        _currentCadetName = chosen.NombreCadete;
                        _game.RestoreFromSave(
                            chosen.EtapaActual,
                            chosen.Oxigeno,
                            chosen.IntegridadCasco,
                            chosen.DatosRecuperados,
                            chosen.EscaneoOrbital,
                            chosen.DescensoDirecto,
                            chosen.SenalConfiable,
                            chosen.SondaEnviada,
                            chosen.Decisiones);
                        ExplorationSaveState? explorationState;
                        try
                        {
                            explorationState = string.IsNullOrWhiteSpace(chosen.EstadoExploracionJson)
                                ? new ExplorationSaveState()
                                : JsonSerializer.Deserialize<ExplorationSaveState>(chosen.EstadoExploracionJson);
                        }
                        catch (JsonException)
                        {
                            explorationState = new ExplorationSaveState();
                            MessageBox.Show(
                                "El estado guardado del mapa no se pudo leer. Se iniciará la exploración desde el comienzo.",
                                "NEXUS // Estado de exploración",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                        _explorationView.RestoreState(explorationState ?? new ExplorationSaveState());

                        LastResult = null;
                        _saveStatusLabel.ForeColor = Color.FromArgb(101, 244, 194);
                        _saveStatusLabel.Text = $"SQL SERVER: EXPEDICIÓN #{chosen.IdExpedicion} RESTAURADA ({chosen.EtapaActual})";
                        RenderMission();
                        picker.DialogResult = DialogResult.OK;
                        picker.Close();
                    }
                };

                picker.Controls.Add(listBox);
                picker.Controls.Add(confirmBtn);
                picker.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar partidas en SQL Server:\n{ex.Message}", "Error SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnVolver_Click(object? sender, EventArgs e)
        {
            StopGameLoop();
            if (_game.CurrentStageKey != "Completed")
            {
                SaveCurrentExpedition(showDialog: false);
            }
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void FormMision_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_game.CurrentStageKey != "Exploration")
            {
                return;
            }

            if (e.KeyCode == Keys.I)
            {
                _explorationView.ToggleInventory();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (_explorationView.InventoryOpen)
            {
                if (e.KeyCode is Keys.Up or Keys.W)
                    _explorationView.MoveInventorySelection(-1);
                else if (e.KeyCode is Keys.Down or Keys.S)
                    _explorationView.MoveInventorySelection(1);
                else if (e.KeyCode is Keys.E or Keys.Enter)
                    _explorationView.UseSelectedInventoryItem();

                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            bool firstKeyDown = _heldKeys.Add(e.KeyCode);
            if (e.KeyCode == Keys.E && firstKeyDown)
            {
                _explorationView.Interact();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Up or Keys.Down or Keys.Left or Keys.Right)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void FormMision_KeyUp(object? sender, KeyEventArgs e)
        {
            _heldKeys.Remove(e.KeyCode);
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            if (!Visible || _game.CurrentStageKey != "Exploration")
            {
                StopGameLoop();
                return;
            }

            float elapsed = (float)_frameClock.Elapsed.TotalSeconds;
            _frameClock.Restart();
            int horizontal = IsKeyHeld(Keys.A, Keys.Left) ? -1 : IsKeyHeld(Keys.D, Keys.Right) ? 1 : 0;
            int vertical = IsKeyHeld(Keys.W, Keys.Up) ? -1 : IsKeyHeld(Keys.S, Keys.Down) ? 1 : 0;
            if (_explorationView.MovePlayer(horizontal, vertical, elapsed))
            {
                ScheduleAutoSave();
            }
        }

        private bool IsKeyHeld(Keys first, Keys alternate) =>
            _heldKeys.Contains(first) || _heldKeys.Contains(alternate);

        private void StartGameLoop()
        {
            if (!_gameTimer.Enabled)
            {
                _frameClock.Restart();
                _gameTimer.Start();
            }
        }

        private void StopGameLoop()
        {
            _gameTimer.Stop();
            _heldKeys.Clear();
        }

        private void ScheduleAutoSave()
        {
            if (_game.CurrentStageKey == "Completed")
            {
                return;
            }

            _pendingAutoSave = true;
            _autosaveTimer.Stop();
            _autosaveTimer.Start();
        }

        private void ExplorationView_ActionCompleted(object? sender, ExplorationActionEventArgs e)
        {
            _game.RecordExplorationAction(e.Decision, e.OxygenChange, e.HullChange, e.DataChange);
            if (e.CompletesExploration)
            {
                _game.CompleteExploration();
            }
            RenderMission();
            ScheduleAutoSave();
        }

        private void FormMision_Load(object sender, EventArgs e)
        {

        }
    }
}
