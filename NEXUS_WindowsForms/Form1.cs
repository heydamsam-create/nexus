namespace NEXUS_WindowsForms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            FormMision mision = new FormMision();
            mision.Show();
            this.Hide();
        }

        private void btnRegistros_Click(object sender, EventArgs e)
        {
            FormRegistros registros = new FormRegistros();
            registros.Show();
            this.Hide();
        }

        private void btnInformacion_Click(object sender, EventArgs e)
        {
            FormInformacion informacion = new FormInformacion();
            informacion.Show();
            this.Hide();
        }
    }
    }

