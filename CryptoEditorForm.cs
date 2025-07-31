using System;
using System.Configuration; // 用于读取和保存 app.config
using System.Windows.Forms;

namespace CryptoMonitorApp // <<< 确保这里是你的项目命名空间
{
    public partial class CryptoEditorForm : Form // <<< 确保这里有 'partial' 关键字
    {
        public CryptoEditorForm()
        {
            InitializeComponent(); // 这会自动加载设计器中添加的控件

            this.Text = "编辑第二行交易对"; // 设置窗口标题
            this.Width = 800; // 设置窗口宽度
            this.Height = 500; // 设置窗口高度
            this.StartPosition = FormStartPosition.CenterParent; // 居中显示

            // 为加载和保存按钮添加事件处理
            // 确保你在设计器中添加了名为 'txtCryptoIds', 'btnSave', 'btnCancel' 的控件
            this.Load += (s, e) => LoadCryptoIds();
            btnSave.Click += (s, e) => SaveCryptoIds();
        }

        private void LoadCryptoIds()
        {
            // 从 app.config 读取当前的 EditableCryptoCurrencyIds
            txtCryptoIds.Text = ConfigurationManager.AppSettings["EditableCryptoCurrencyIds"] ?? "ethereum,solana,bnb";
        }

        private void SaveCryptoIds()
        {
            // 获取当前配置文件的路径
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // 获取 appSettings 节
            AppSettingsSection appSettings = config.AppSettings;

            // 更新或添加 EditableCryptoCurrencyIds
            if (appSettings.Settings["EditableCryptoCurrencyIds"] == null)
            {
                appSettings.Settings.Add("EditableCryptoCurrencyIds", txtCryptoIds.Text);
            }
            else
            {
                appSettings.Settings["EditableCryptoCurrencyIds"].Value = txtCryptoIds.Text;
            }

            // 保存更改到配置文件
            config.Save(ConfigurationSaveMode.Modified);

            // 强制重新加载配置节，以便 ConfigurationManager.AppSettings 能够获取到最新值
            ConfigurationManager.RefreshSection("appSettings");

            MessageBox.Show("交易对已保存。应用程序将重新加载数据。", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // DialogResult 已经设置为 OK，点击按钮后窗口会自动关闭并返回 OK
        }
    }
}