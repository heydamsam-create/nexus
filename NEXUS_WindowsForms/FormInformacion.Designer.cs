namespace NEXUS_WindowsForms
{
    partial class FormInformacion
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
            btnVolver = new Button();
            SuspendLayout();
            // 
            // btnVolver
            // 
            btnVolver.BackColor = Color.SteelBlue;
            btnVolver.FlatStyle = FlatStyle.Flat;
            btnVolver.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnVolver.ForeColor = Color.White;
            btnVolver.Location = new Point(297, 152);
            btnVolver.Name = "btnVolver";
            btnVolver.Size = new Size(200, 45);
            btnVolver.TabIndex = 0;
            btnVolver.Text = "VOLVER AL MENÚ";
            btnVolver.UseVisualStyleBackColor = false;
            btnVolver.Click += btnVolver_Click;
            // 
            // FormInformacion
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 35, 50);
            ClientSize = new Size(800, 450);
            Controls.Add(btnVolver);
            ForeColor = Color.White;
            Name = "FormInformacion";
            Text = "FormInformacion";
            ResumeLayout(false);
        }

        #endregion

        private Button btnVolver;
    }
}