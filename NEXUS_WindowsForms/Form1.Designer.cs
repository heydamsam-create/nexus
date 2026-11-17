namespace NEXUS_WindowsForms
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblTitulo = new Label();
            lblSubtitulo = new Label();
            btnIniciar = new Button();
            btnRegistros = new Button();
            btnInformacion = new Button();
            btnSalir = new Button();
            SuspendLayout();
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI", 28.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitulo.ForeColor = Color.White;
            lblTitulo.Location = new Point(364, 35);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(0, 62);
            lblTitulo.TabIndex = 0;
            lblTitulo.Visible = false;
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSubtitulo.ForeColor = Color.LightSkyBlue;
            lblSubtitulo.Location = new Point(351, 97);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(0, 20);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Visible = false;
            // 
            // btnIniciar
            // 
            btnIniciar.BackColor = Color.Transparent;
            btnIniciar.Cursor = Cursors.Hand;
            btnIniciar.FlatAppearance.BorderSize = 0;
            btnIniciar.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnIniciar.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnIniciar.FlatStyle = FlatStyle.Flat;
            btnIniciar.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnIniciar.ForeColor = Color.White;
            btnIniciar.Location = new Point(200, 120);
            btnIniciar.Name = "btnIniciar";
            btnIniciar.Size = new Size(250, 50);
            btnIniciar.TabIndex = 2;
            btnIniciar.UseVisualStyleBackColor = false;
            btnIniciar.Click += btnIniciar_Click;
            // 
            // btnRegistros
            // 
            btnRegistros.BackColor = Color.Transparent;
            btnRegistros.Cursor = Cursors.Hand;
            btnRegistros.FlatAppearance.BorderSize = 0;
            btnRegistros.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnRegistros.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnRegistros.FlatStyle = FlatStyle.Flat;
            btnRegistros.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRegistros.ForeColor = Color.White;
            btnRegistros.Location = new Point(238, 424);
            btnRegistros.Name = "btnRegistros";
            btnRegistros.Size = new Size(250, 50);
            btnRegistros.TabIndex = 3;
            btnRegistros.UseVisualStyleBackColor = false;
            btnRegistros.Click += btnRegistros_Click;
            // 
            // btnInformacion
            // 
            btnInformacion.BackColor = Color.Transparent;
            btnInformacion.Cursor = Cursors.Hand;
            btnInformacion.FlatAppearance.BorderSize = 0;
            btnInformacion.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnInformacion.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnInformacion.FlatStyle = FlatStyle.Flat;
            btnInformacion.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnInformacion.ForeColor = Color.White;
            btnInformacion.Location = new Point(238, 518);
            btnInformacion.Name = "btnInformacion";
            btnInformacion.Size = new Size(250, 50);
            btnInformacion.TabIndex = 4;
            btnInformacion.UseVisualStyleBackColor = false;
            btnInformacion.Click += btnInformacion_Click;
            // 
            // btnSalir
            // 
            btnSalir.BackColor = Color.Transparent;
            btnSalir.Cursor = Cursors.Hand;
            btnSalir.FlatAppearance.BorderSize = 0;
            btnSalir.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnSalir.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnSalir.FlatStyle = FlatStyle.Flat;
            btnSalir.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSalir.ForeColor = Color.White;
            btnSalir.Location = new Point(229, 616);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(250, 50);
            btnSalir.TabIndex = 5;
            btnSalir.UseVisualStyleBackColor = false;
            btnSalir.Click += btnSalir_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 35, 50);
            BackgroundImage = Properties.Resources.nexusfondo2;
            ClientSize = new Size(1596, 819);
            Controls.Add(btnSalir);
            Controls.Add(btnInformacion);
            Controls.Add(btnRegistros);
            Controls.Add(btnIniciar);
            Controls.Add(lblSubtitulo);
            Controls.Add(lblTitulo);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "NEXUS - Sistema de Exploración";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitulo;
        private Label lblSubtitulo;
        private Button btnIniciar;
        private Button btnRegistros;
        private Button btnInformacion;
        private Button btnSalir;
    }
}
