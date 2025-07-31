namespace CryptoMonitorApp
{
    partial class CryptoEditorForm
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
            txtCryptoIds = new TextBox();
            btnSave = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // txtCryptoIds
            // 
            txtCryptoIds.Dock = DockStyle.Top;
            txtCryptoIds.Location = new Point(0, 0);
            txtCryptoIds.Multiline = true;
            txtCryptoIds.Name = "txtCryptoIds";
            txtCryptoIds.ScrollBars = ScrollBars.Both;
            txtCryptoIds.Size = new Size(853, 311);
            txtCryptoIds.TabIndex = 0;
            // 
            // btnSave
            // 
            btnSave.DialogResult = DialogResult.OK;
            btnSave.Location = new Point(476, 339);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(131, 40);
            btnSave.TabIndex = 1;
            btnSave.Text = "保存";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(640, 339);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(131, 40);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "取消";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // CryptoEditorForm
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(853, 402);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(txtCryptoIds);
            Name = "CryptoEditorForm";
            Text = "CryptoEditorForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtCryptoIds;
        private Button btnSave;
        private Button btnCancel;
    }
}