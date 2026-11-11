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
            lblTitulo.Size = new Size(181, 62);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "NEXUS";
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSubtitulo.ForeColor = Color.LightSkyBlue;
            lblSubtitulo.Location = new Point(351, 97);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(203, 20);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "SISTEMA DE EXPLORACIÓN";
            // 
            // btnIniciar
            // 
            btnIniciar.BackColor = Color.SteelBlue;
            btnIniciar.FlatStyle = FlatStyle.Flat;
            btnIniciar.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnIniciar.ForeColor = Color.White;
            btnIniciar.Location = new Point(328, 151);
            btnIniciar.Name = "btnIniciar";
            btnIniciar.Size = new Size(250, 50);
            btnIniciar.TabIndex = 2;
            btnIniciar.Text = "INICIAR MISIÓN";
            btnIniciar.UseVisualStyleBackColor = false;
            // 
            // btnRegistros
            // 
            btnRegistros.BackColor = Color.SteelBlue;
            btnRegistros.FlatStyle = FlatStyle.Flat;
            btnRegistros.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRegistros.ForeColor = Color.White;
            btnRegistros.Location = new Point(328, 227);
            btnRegistros.Name = "btnRegistros";
            btnRegistros.Size = new Size(250, 50);
            btnRegistros.TabIndex = 3;
            btnRegistros.Text = "EXPLORAR REGISTROS";
            btnRegistros.UseVisualStyleBackColor = false;
            // 
            // btnInformacion
            // 
            btnInformacion.BackColor = Color.SteelBlue;
            btnInformacion.FlatStyle = FlatStyle.Flat;
            btnInformacion.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnInformacion.ForeColor = Color.White;
            btnInformacion.Location = new Point(328, 302);
            btnInformacion.Name = "btnInformacion";
            btnInformacion.Size = new Size(250, 50);
            btnInformacion.TabIndex = 4;
            btnInformacion.Text = "INFORMACIÓN";
            btnInformacion.UseVisualStyleBackColor = false;
            // 
            // btnSalir
            // 
            btnSalir.BackColor = Color.Firebrick;
            btnSalir.FlatStyle = FlatStyle.Flat;
            btnSalir.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSalir.ForeColor = Color.White;
            btnSalir.Location = new Point(328, 382);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(250, 50);
            btnSalir.TabIndex = 5;
            btnSalir.Text = "SALIR";
            btnSalir.UseVisualStyleBackColor = false;
            btnSalir.Click += btnSalir_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 35, 50);
            ClientSize = new Size(882, 553);
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
