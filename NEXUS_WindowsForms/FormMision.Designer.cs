namespace NEXUS_WindowsForms
{
    partial class FormMision
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblTitulo = new Label();
            lblSubtitulo = new Label();
            btnIniciar = new Button();
            btnVolver = new Button();
            lblNombreCadete = new Button();
            SuspendLayout();
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI", 28.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitulo.ForeColor = Color.White;
            lblTitulo.Location = new Point(388, 9);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(181, 62);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "NEXUS";
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSubtitulo.ForeColor = Color.LightSkyBlue;
            lblSubtitulo.Location = new Point(363, 71);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(271, 28);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "SISTEMA DE EXPLORACIÓN";
            // 
            // btnIniciar
            // 
            btnIniciar.BackColor = Color.SteelBlue;
            btnIniciar.FlatStyle = FlatStyle.Flat;
            btnIniciar.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnIniciar.ForeColor = Color.White;
            btnIniciar.Location = new Point(76, 136);
            btnIniciar.Name = "btnIniciar";
            btnIniciar.Size = new Size(250, 50);
            btnIniciar.TabIndex = 2;
            btnIniciar.Text = "INICIAR MISIÓN";
            btnIniciar.UseVisualStyleBackColor = false;
            // 
            // btnVolver
            // 
            btnVolver.AutoSize = true;
            btnVolver.BackColor = Color.SteelBlue;
            btnVolver.FlatStyle = FlatStyle.Flat;
            btnVolver.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnVolver.ForeColor = Color.White;
            btnVolver.Location = new Point(657, 139);
            btnVolver.Name = "btnVolver";
            btnVolver.Size = new Size(200, 45);
            btnVolver.TabIndex = 3;
            btnVolver.Text = "VOLVER AL MENÚ";
            btnVolver.UseVisualStyleBackColor = false;
            btnVolver.Click += btnVolver_Click;
            // 
            // lblNombreCadete
            // 
            lblNombreCadete.BackColor = Color.SteelBlue;
            lblNombreCadete.FlatStyle = FlatStyle.Flat;
            lblNombreCadete.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblNombreCadete.ForeColor = Color.White;
            lblNombreCadete.Location = new Point(363, 136);
            lblNombreCadete.Name = "lblNombreCadete";
            lblNombreCadete.Size = new Size(250, 50);
            lblNombreCadete.TabIndex = 4;
            lblNombreCadete.Text = "CADETE: EXPLORADOR";
            lblNombreCadete.UseVisualStyleBackColor = false;
            // 
            // FormMision
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 35, 50);
            ClientSize = new Size(982, 603);
            Controls.Add(lblNombreCadete);
            Controls.Add(btnVolver);
            Controls.Add(btnIniciar);
            Controls.Add(lblSubtitulo);
            Controls.Add(lblTitulo);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FormMision";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "NEXUS - Misión";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitulo;
        private Label lblSubtitulo;
        private Button btnIniciar;
        private Button btnVolver;
        private Button lblNombreCadete;
    }
}