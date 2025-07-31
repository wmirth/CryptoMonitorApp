namespace CryptoMonitorApp
{
    partial class AlertPriceInputForm
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
            this.lblInstructions = new System.Windows.Forms.Label();
            this.txtAlertPrice = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblInstructions
            //
            this.lblInstructions.AutoSize = true;
            this.lblInstructions.Location = new System.Drawing.Point(20, 20);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new System.Drawing.Size(209, 28); // 适当调整大小
            this.lblInstructions.TabIndex = 0;
            this.lblInstructions.Text = "请输入 BTC 报警价格:";
            //
            // txtAlertPrice
            //
            this.txtAlertPrice.Location = new System.Drawing.Point(25, 60);
            this.txtAlertPrice.Name = "txtAlertPrice";
            this.txtAlertPrice.Size = new System.Drawing.Size(200, 34);
            this.txtAlertPrice.TabIndex = 1;
            //
            // btnOK
            //
            this.btnOK.Location = new System.Drawing.Point(25, 110);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(90, 35);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            //
            // btnCancel
            //
            this.btnCancel.Location = new System.Drawing.Point(135, 110);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 35);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // AlertPriceInputForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 28F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(250, 165); // 调整窗口大小
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtAlertPrice);
            this.Controls.Add(this.lblInstructions);
            this.Name = "AlertPriceInputForm";
            this.Text = "AlertPriceInputForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label lblInstructions;
        private TextBox txtAlertPrice;
        private Button btnOK;
        private Button btnCancel;
    }
}