namespace NEXUS_WindowsForms
{
    public partial class Form1 : Form
    {
        internal event EventHandler? MissionRequested;
        internal event EventHandler? RecordsRequested;
        internal event EventHandler? InformationRequested;
        internal event EventHandler? ExitRequested;

        public Form1()
        {
            InitializeComponent();

            // The menu artwork is the visible button. Keep its transparent hit target
            // aligned with that artwork; the Designer position currently sits above it.
            btnIniciar.Bounds = new Rectangle(91, 305, 548, 87);

            btnIniciar.AccessibleName = "Iniciar misión";
            btnIniciar.AccessibleDescription = "Comienza la exploración de Eridani-9.";
            btnRegistros.AccessibleName = "Explorar registros";
            btnRegistros.AccessibleDescription = "Consulta los resultados de misiones finalizadas.";
            btnInformacion.AccessibleName = "Información";
            btnInformacion.AccessibleDescription = "Abre la enciclopedia del universo NEXUS.";
            btnSalir.AccessibleName = "Salir";
            btnSalir.AccessibleDescription = "Cierra la aplicación NEXUS.";
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            MissionRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnRegistros_Click(object sender, EventArgs e)
        {
            RecordsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnInformacion_Click(object sender, EventArgs e)
        {
            InformationRequested?.Invoke(this, EventArgs.Empty);
        }
    }
    }

