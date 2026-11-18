namespace NEXUS_WindowsForms
{
    /// <summary>
    /// Colores y estilo compartido por las pantallas de NEXUS.
    /// </summary>
    internal static class NexusTheme
    {
        public static readonly Color Background = Color.FromArgb(5, 12, 28);
        public static readonly Color Panel = Color.FromArgb(10, 22, 43);
        public static readonly Color Card = Color.FromArgb(14, 30, 55);
        public static readonly Color Accent = Color.FromArgb(43, 218, 255);
        public static readonly Color Text = Color.FromArgb(225, 239, 250);
        public static readonly Color MutedText = Color.FromArgb(139, 170, 195);
        public static readonly Color Success = Color.FromArgb(101, 244, 194);
        public static readonly Color Warning = Color.FromArgb(255, 201, 112);
        public static readonly Color Error = Color.FromArgb(255, 142, 142);
        public static readonly Color ButtonHover = Color.FromArgb(20, 51, 78);
        public static readonly Color ButtonPressed = Color.FromArgb(14, 76, 99);

        public static Button CreateActionButton(
            string text,
            float fontSize = 9.5F,
            Padding? padding = null)
        {
            var button = new Button
            {
                Text = text,
                BackColor = Card,
                ForeColor = Text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", fontSize, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand,
                Padding = padding ?? new Padding(12, 4, 8, 4)
            };
            button.FlatAppearance.BorderColor = Accent;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = ButtonHover;
            button.FlatAppearance.MouseDownBackColor = ButtonPressed;
            return button;
        }
    }
}
