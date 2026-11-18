using NEXUS_WindowsForms.Content;

namespace NEXUS_WindowsForms
{
    public partial class FormInformacion : Form
    {
        private static readonly Color BackgroundColor = Color.FromArgb(5, 12, 28);
        private static readonly Color PanelColor = Color.FromArgb(10, 22, 43);
        private static readonly Color CardColor = Color.FromArgb(14, 30, 55);
        private static readonly Color AccentColor = Color.FromArgb(43, 218, 255);
        private static readonly Color TextColor = Color.FromArgb(225, 239, 250);
        private static readonly Color MutedTextColor = Color.FromArgb(139, 170, 195);

        private readonly List<Button> _sectionButtons = new();
        private Label _categoryLabel = null!;
        private Label _titleLabel = null!;
        private Label _leadLabel = null!;
        private RichTextBox _bodyTextBox = null!;
        private Label _sourceNoteLabel = null!;
        private Label _sectionCounterLabel = null!;
        private Button? _selectedButton;

        internal event EventHandler? BackRequested;

        public FormInformacion()
        {
            InitializeComponent();
            ConfigureWindow();

            // La enciclopedia se construye desde código para conservar intacto el Designer.
            while (Controls.Count > 0)
            {
                Control existingControl = Controls[0];
                Controls.Remove(existingControl);
                existingControl.Dispose();
            }

            BuildInterface();
            ShowSection(0);
        }

        private void ConfigureWindow()
        {
            Text = "NEXUS | Enciclopedia del universo";
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
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            Controls.Add(layout);

            layout.Controls.Add(BuildHeader(), 0, 0);
            layout.Controls.Add(BuildKnowledgeArea(), 0, 1);
            layout.Controls.Add(BuildFooter(), 0, 2);
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
            header.Controls.Add(new Label
            {
                Text = "NEXUS  //  ENCICLOPEDIA DEL UNIVERSO",
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

            return header;
        }

        private Control BuildKnowledgeArea()
        {
            var columns = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 14, 0, 8)
            };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 71));
            columns.Controls.Add(BuildSectionIndex(), 0, 0);
            columns.Controls.Add(BuildArticlePanel(), 1, 0);
            return columns;
        }

        private Control BuildSectionIndex()
        {
            var indexPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 12, 0)
            };
            var indexTitle = new Label
            {
                Text = "ARCHIVO DE DATOS",
                Dock = DockStyle.Top,
                Height = 42,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var buttonList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = PanelColor,
                Padding = new Padding(0, 4, 0, 0)
            };

            for (int index = 0; index < NexusEncyclopedia.Sections.Count; index++)
            {
                int sectionIndex = index;
                EncyclopediaSection section = NexusEncyclopedia.Sections[index];
                var sectionButton = CreateButton(section.ShortTitle);
                sectionButton.Height = 48;
                sectionButton.Width = 280;
                sectionButton.Margin = new Padding(0, 0, 0, 8);
                sectionButton.TextAlign = ContentAlignment.MiddleLeft;
                sectionButton.Click += (_, _) => ShowSection(sectionIndex);
                buttonList.Controls.Add(sectionButton);
                _sectionButtons.Add(sectionButton);
            }

            void ResizeSectionButtons(object? sender, EventArgs e)
            {
                int width = Math.Max(180, buttonList.ClientSize.Width - 8);
                foreach (Button button in _sectionButtons)
                {
                    button.Width = width;
                }
            }

            buttonList.SizeChanged += ResizeSectionButtons;
            indexPanel.Controls.Add(buttonList);
            indexPanel.Controls.Add(indexTitle);
            ResizeSectionButtons(buttonList, EventArgs.Empty);
            return indexPanel;
        }

        private Control BuildArticlePanel()
        {
            var articlePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(22, 18, 22, 16)
            };
            var articleLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Margin = Padding.Empty
            };
            articleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            articleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            articleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            articleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            articleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            _categoryLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            _titleLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            _leadLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(0, 5, 8, 8),
                UseCompatibleTextRendering = true
            };
            _bodyTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = false,
                BorderStyle = BorderStyle.None,
                BackColor = PanelColor,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                TabStop = false
            };
            _sourceNoteLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(101, 244, 194),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            articleLayout.Controls.Add(_categoryLabel, 0, 0);
            articleLayout.Controls.Add(_titleLabel, 0, 1);
            articleLayout.Controls.Add(_leadLabel, 0, 2);
            articleLayout.Controls.Add(_bodyTextBox, 0, 3);
            articleLayout.Controls.Add(_sourceNoteLabel, 0, 4);
            articlePanel.Controls.Add(articleLayout);
            return articlePanel;
        }

        private Control BuildFooter()
        {
            _sectionCounterLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            return _sectionCounterLabel;
        }

        private void ShowSection(int index)
        {
            if (index < 0 || index >= NexusEncyclopedia.Sections.Count)
            {
                return;
            }

            EncyclopediaSection section = NexusEncyclopedia.Sections[index];
            _categoryLabel.Text = $"ARCHIVO NEXUS  /  {section.Category.ToUpperInvariant()}";
            _titleLabel.Text = section.Title;
            _leadLabel.Text = section.Lead;
            _bodyTextBox.Text = section.Content;
            _bodyTextBox.SelectionStart = 0;
            _bodyTextBox.ScrollToCaret();
            _sourceNoteLabel.Text = section.Note;
            _sourceNoteLabel.ForeColor = section.IsEstablished
                ? Color.FromArgb(101, 244, 194)
                : Color.FromArgb(255, 201, 112);
            _sectionCounterLabel.Text = $"SECCIÓN {index + 1:00}  /  {NexusEncyclopedia.Sections.Count:00}";

            if (_selectedButton is not null)
            {
                SetButtonSelected(_selectedButton, false);
            }

            _selectedButton = _sectionButtons[index];
            SetButtonSelected(_selectedButton, true);
        }

        private static void SetButtonSelected(Button button, bool selected)
        {
            button.BackColor = selected ? Color.FromArgb(16, 56, 79) : CardColor;
            button.ForeColor = selected ? AccentColor : TextColor;
            button.FlatAppearance.BorderSize = selected ? 2 : 1;
        }

        private static Button CreateButton(string text)
        {
            var button = new Button
            {
                Text = text,
                BackColor = CardColor,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand,
                Padding = new Padding(12, 4, 8, 4)
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
