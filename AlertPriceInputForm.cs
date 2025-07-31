using System;
using System.Drawing;
using System.Windows.Forms;

namespace CryptoMonitorApp
{
    public partial class AlertPriceInputForm : Form
    {
        // 公共属性，用于从主窗体获取/设置报警价格
        public decimal AlertPrice { get; private set; }

        public AlertPriceInputForm(decimal currentAlertPrice)
        {
            InitializeComponent();

            this.Text = "设置 BTC 报警价格";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // 固定边框，不可调整大小
            this.MaximizeBox = false; // 禁用最大化按钮
            this.MinimizeBox = false; // 禁用最小化按钮
            this.ShowInTaskbar = false; // 不在任务栏显示

            // 初始化 TextBox，显示当前设置的报警价格
            txtAlertPrice.Text = currentAlertPrice.ToString("F2"); // 保留两位小数显示

            // 确保 Enter 键按下时触发确定按钮，Escape 键按下时触发取消按钮
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // 为确定按钮添加事件处理
            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (decimal.TryParse(txtAlertPrice.Text, out decimal price) && price >= 0)
            {
                AlertPrice = price;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("请输入一个有效的非负数字作为报警价格。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAlertPrice.Focus(); // 重新聚焦输入框
                txtAlertPrice.SelectAll(); // 选中所有文本方便用户修改
            }
        }
    }
}