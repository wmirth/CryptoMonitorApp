namespace CryptoMonitorApp
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            lblBtcData = new Label(); // 修改为 lblBtcData
            lblOtherCryptoData = new Label(); // 新增 lblOtherCryptoData
            dataRefreshTimer = new System.Windows.Forms.Timer(components);
            trayIcon = new NotifyIcon(components);
            SuspendLayout();
            //
            // lblBtcData
            //
            lblBtcData.Dock = DockStyle.Top; // 停靠在顶部
            lblBtcData.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblBtcData.Location = new Point(0, 0);
            lblBtcData.Name = "lblBtcData";
            lblBtcData.Size = new Size(800, 35); // 调整大小以适应一行
            lblBtcData.TabIndex = 0;
            lblBtcData.Text = "BTC: 加载中...";
            lblBtcData.TextAlign = ContentAlignment.TopCenter;
            //
            // lblOtherCryptoData
            //
            lblOtherCryptoData.Dock = DockStyle.Fill; // 填充剩余空间
            lblOtherCryptoData.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0); // 第二行字体可以不那么粗
            lblOtherCryptoData.Location = new Point(0, 35); // 位于 lblBtcData 下方
            lblOtherCryptoData.Name = "lblOtherCryptoData";
            lblOtherCryptoData.Size = new Size(800, 415); // 调整大小
            lblOtherCryptoData.TabIndex = 1; // 调整 TabIndex
            lblOtherCryptoData.Text = "次要币种: 加载中...";
            lblOtherCryptoData.TextAlign = ContentAlignment.TopCenter;
            //
            // dataRefreshTimer
            //
            // 保持不变
            //
            // trayIcon
            //
            // 保持不变
            //
            // MainForm
            //
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(450, 70); // 调整主窗体大小以适应两行数据
            Controls.Add(lblOtherCryptoData); // 先添加第二行，因为它会填充
            Controls.Add(lblBtcData); // 后添加第一行，它会停靠在顶部
            Name = "MainForm";
            Text = "加密货币监控器"; // 更改默认标题
            ResumeLayout(false);
        }

        #endregion

        private Label lblBtcData; // 修改为 lblBtcData
        private Label lblOtherCryptoData; // 新增 lblOtherCryptoData
        private System.Windows.Forms.Timer dataRefreshTimer;
        private NotifyIcon trayIcon;
    }
}