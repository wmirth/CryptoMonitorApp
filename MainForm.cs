using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms; // 包含 System.Windows.Forms.Timer
using System.Configuration; // 用于读取 app.config
using System.Collections.Generic; // 用于 Dictionary 和 HashSet
using System.Linq; // 用于 Linq 方法，如 .ToList()
using System.Diagnostics;
using System.Runtime.InteropServices; // 用于 P/Invoke

namespace CryptoMonitorApp
{
    //API
    //https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum,solana,okb,dogecoin,xrp&vs_currencies=usd&include_24hr_change=true
    //
    public partial class MainForm : Form
    {
        // =========================================================================
        // 成员变量 - 用于窗口拖动
        // =========================================================================
        private bool isDragging = false;
        private Point lastCursorPos;
        // =========================================================================
        // 新增: 上下文菜单打开时暂停置顶定时器
        // =========================================================================
        private void ContextMenu_Opened(object? sender, EventArgs e)
        {
            if (_topMostEnforcerTimer.Enabled)
            {
                _topMostEnforcerTimer.Stop();
                Debug.WriteLine("DEBUG: _topMostEnforcerTimer stopped due to ContextMenu_Opened.");
            }
        }

        // =========================================================================
        // 新增: 上下文菜单关闭时恢复置顶定时器
        // =========================================================================
        private void ContextMenu_Closed(object? sender, EventArgs e)
        {
            // 菜单关闭后，如果窗口可见，则重新启动置顶定时器并强制置顶一次
            if (this.Visible && !_topMostEnforcerTimer.Enabled)
            {
                _topMostEnforcerTimer.Start();
                ForceWindowToTop(); // 立即置顶一次，以防在关闭期间被其他窗口覆盖
                Debug.WriteLine("DEBUG: _topMostEnforcerTimer started and ForceWindowToTop called due to ContextMenu_Closed.");
            }
        }
        // =========================================================================
        // 成员变量 - 用于轮换显示第二行交易对
        // =========================================================================
        private Dictionary<string, CryptoCurrencyDetails> _cachedOtherCryptoData = new Dictionary<string, CryptoCurrencyDetails>(StringComparer.OrdinalIgnoreCase);
        private List<string> _rotatingCryptoIds = new List<string>();
        private int _currentRotatingCryptoIndex = 0;

        private System.Windows.Forms.Timer _rotationTimer; // 用于控制轮换的定时器

        // =========================================================================
        // 新增: 用于持续强制置顶的定时器
        // =========================================================================
        private System.Windows.Forms.Timer _topMostEnforcerTimer;

        // =========================================================================
        // 新增: BTC 报警功能相关成员变量
        // =========================================================================
        private decimal _btcAlertPrice; // BTC 报警价格
        private bool _btcAlertEnabled; // BTC 报警功能是否启用
        private System.Windows.Forms.Timer _btcBlinkTimer; // BTC 闪烁定时器
        private bool _isBtcBlinking = false; // BTC 是否正在闪烁
        private Color _originalBtcColor; // 记录 BTC 标签的原始颜色

        // =========================================================================
        // Windows API 定义 - 用于强制窗口置顶
        // =========================================================================
        // 定义 HWND 参数的特殊值
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); // 窗口置于所有非 TopMost 窗口之上
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2); // 窗口置于所有 TopMost 窗口之下 (恢复正常)

        // 定义 SetWindowPos 的 Flags
        const uint SWP_NOSIZE = 0x0001; // 不改变窗口大小
        const uint SWP_NOMOVE = 0x0002; // 不改变窗口位置
        const uint SWP_SHOWWINDOW = 0x0040; // 显示窗口

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        // =========================================================================

        // =========================================================================
        // 构造函数 - 窗体初始化
        // =========================================================================
        public MainForm()
        {
            InitializeComponent(); // 设计器生成的组件初始化代码

            // 手动初始化 trayIcon，因为 Designer.cs 通常不处理 NotifyIcon
            trayIcon = new NotifyIcon();

            // =====================================================================
            // 1. 设置窗体属性
            // =====================================================================
            this.FormBorderStyle = FormBorderStyle.None;         // 无边框
            this.ShowInTaskbar = false;                          // 不在任务栏显示图标
            this.TopMost = true;                                 // 首次设置为始终置顶，后续由定时器强制
            this.BackColor = Color.Black;                        // 背景色
            this.TransparencyKey = Color.Black;                  // 设置背景色为透明键，使其透明

            // 显式设置窗体大小以确保 SetWindowLocation 计算正确，并保证能显示两行数据
            this.Size = new Size(450, 70);

            // =====================================================================
            // 2. 为 lblBtcData 和 lblOtherCryptoData 标签设置一些属性
            // =====================================================================
            lblBtcData.Text = "BTC: 加载中...";
            lblBtcData.Font = new Font("Consolas", 12, FontStyle.Bold); // BTC行加粗
            lblBtcData.ForeColor = Color.White; // 默认白色

            lblOtherCryptoData.Text = "次要币种: 加载中...";
            lblOtherCryptoData.Font = new Font("Consolas", 12, FontStyle.Regular); // 其他行正常
            lblOtherCryptoData.ForeColor = Color.White; // 默认白色

            _originalBtcColor = lblBtcData.ForeColor; // 记录 BTC 标签的原始颜色

            // =====================================================================
            // 3. 初始化并启动置顶强制刷新定时器
            // =====================================================================
            _topMostEnforcerTimer = new System.Windows.Forms.Timer();
            _topMostEnforcerTimer.Interval = 100; // 每 100 毫秒强制置顶一次
            _topMostEnforcerTimer.Tick += (s, e) => ForceWindowToTop();
            _topMostEnforcerTimer.Start(); // 启动定时器

            // =====================================================================
            // 4. 初始化 BTC 闪烁定时器
            // =====================================================================
            _btcBlinkTimer = new System.Windows.Forms.Timer();
            _btcBlinkTimer.Interval = 500; // 每 500 毫秒闪烁一次
            _btcBlinkTimer.Tick += BtcBlinkTimer_Tick;


            // =====================================================================
            // 5. 窗体事件处理
            // =====================================================================
            this.Load += async (s, e) =>
            {
                SetWindowLocation();     // 设置初始位置
                ForceWindowToTop();      // 确保它在加载时立即置顶

                // 初始化轮换定时器
                _rotationTimer = new System.Windows.Forms.Timer();
                _rotationTimer.Interval = 5000; // 每5秒轮换一次
                _rotationTimer.Tick += RotationTimer_Tick;
                _rotationTimer.Start(); // 启动轮换定时器

                // 从 app.config 读取报警设置
                LoadAlertSettings();

                await RefreshCryptoData(); // 首次加载时立即刷新数据

                // 从 app.config 读取数据刷新间隔（针对API数据获取的定时器）
                if (int.TryParse(ConfigurationManager.AppSettings["RefreshIntervalMs"], out int interval))
                {
                    dataRefreshTimer.Interval = interval;
                }
                else
                {
                    dataRefreshTimer.Interval = 30000; // 默认30秒
                }
                dataRefreshTimer.Start();  // 启动数据刷新定时器

                trayIcon.Visible = true;    // 程序启动时，托盘图标可见
            };

            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true; // 取消关闭，转为隐藏到托盘
                    this.Hide();
                }
            };


            // =====================================================================
            // 6. 定时器事件处理
            // =====================================================================
            dataRefreshTimer.Tick += async (s, e) => await RefreshCryptoData();


            // =====================================================================
            // 7. 系统托盘图标事件处理
            // =====================================================================
            if (trayIcon.Icon == null)
            {
                try
                {
                    // 确保 AppIcon.ico 存在于项目根目录或输出目录
                    trayIcon.Icon = new Icon("AppIcon.ico");
                }
                catch
                {
                    trayIcon.Icon = SystemIcons.Application; // 回退到系统默认图标
                }
            }
            trayIcon.Text = "加密货币监控器"; // 鼠标悬停在图标上的提示文本

            trayIcon.DoubleClick += (s, e) =>
            {
                if (this.Visible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    SetWindowLocation(); // 重新计算位置，确保显示在正确位置
                    ForceWindowToTop(); // 显示时也强制置顶
                }
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("显示/隐藏", null, (s, e) =>
            {
                if (this.Visible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    SetWindowLocation(); // 重新计算位置
                    ForceWindowToTop(); // 显示时也强制置顶
                }
            });
            contextMenu.Items.Add("编辑第二行交易对", null, (s, e) => EditSecondaryCryptos());
            contextMenu.Items.Add("设置BTC报警价格", null, (s, e) => SetBtcAlertPrice()); // 新增报警价格设置选项
            contextMenu.Items.Add("关闭BTC闪烁", null, (s, e) => StopBtcBlinking()); // 新增关闭闪烁选项

            contextMenu.Items.Add("-"); // 分隔线
            contextMenu.Items.Add("退出", null, (s, e) =>
            {
                trayIcon.Visible = false; // 退出前隐藏托盘图标
                Application.Exit();        // 退出应用程序
            });
            trayIcon.ContextMenuStrip = contextMenu; // 将上下文菜单绑定到托盘图标
            // =========================================================================
            // 新增: 为 contextMenu 添加 Opened 和 Closed 事件处理器
            // =========================================================================
            contextMenu.Opened += ContextMenu_Opened;
            contextMenu.Closed += ContextMenu_Closed;

            // =====================================================================
            // 8. 窗口拖动功能 (现在同时应用于两个标签)
            // =====================================================================
            this.MouseDown += OnFormMouseDown;
            this.MouseMove += OnFormMouseMove;
            this.MouseUp += OnFormMouseUp;

            // 确保标签也可以触发拖动
            lblBtcData.MouseDown += OnFormMouseDown;
            lblBtcData.MouseMove += OnFormMouseMove;
            lblBtcData.MouseUp += OnFormMouseUp;

            lblOtherCryptoData.MouseDown += OnFormMouseDown;
            lblOtherCryptoData.MouseMove += OnFormMouseMove;
            lblOtherCryptoData.MouseUp += OnFormMouseUp;
        }

        // =========================================================================
        // 拖动事件处理方法
        // =========================================================================
        private void OnFormMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastCursorPos = new Point(e.X, e.Y);
            }
        }

        private void OnFormMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // 如果是标签触发的拖动，需要将鼠标位置转换为窗体坐标
                Control? control = sender as Control;
                Point currentScreenPos = Cursor.Position;
                Point newLocation = new Point(currentScreenPos.X - lastCursorPos.X,
                                              currentScreenPos.Y - lastCursorPos.Y);

                if (control != null && control != this)
                {
                    // 如果是子控件，需要减去子控件相对于父控件的位置
                    Point controlLocationOnForm = control.Location;
                    newLocation = new Point(currentScreenPos.X - (controlLocationOnForm.X + lastCursorPos.X),
                                              currentScreenPos.Y - (controlLocationOnForm.Y + lastCursorPos.Y));
                }
                this.Location = newLocation;
            }
        }

        private void OnFormMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
            }
        }

        // =========================================================================
        // 方法 - 设置窗体初始位置
        // =========================================================================
        private void SetWindowLocation()
        {
            Rectangle totalScreenBounds = Screen.PrimaryScreen.Bounds;
            // 假设您的桌面小部件或其他元素在左侧占据 150 像素宽度，留一些边距。
            int widgetPanelWidth = 150;
            int margin = 10;

            // 将窗口定位在右下角，避开可能的左侧小部件。
            int newX = totalScreenBounds.Left + widgetPanelWidth + margin; // 向左偏移
            int newY = totalScreenBounds.Bottom - this.Height - margin; // 向底部偏移

            this.Location = new Point(newX, newY);
        }

        // =========================================================================
        // 方法 - 刷新加密货币数据并更新UI
        // =========================================================================
        private async Task RefreshCryptoData()
        {
            // 更新所有标签的文本为“更新中...”
            lblBtcData.Text = "BTC: 更新中...";
            lblBtcData.ForeColor = Color.Yellow;
            lblOtherCryptoData.Text = "次要币种: 更新中...";
            lblOtherCryptoData.ForeColor = Color.Yellow;

            try
            {
                var (btcPrice, btcChange, otherCryptoData, isDataValid) = await GetCryptoPrices();

                Debug.WriteLine($"DEBUG: RefreshCryptoData - BTC Change = {btcChange:F4}%");

                if (isDataValid)
                {
                    // 缓存其他币种数据
                    _cachedOtherCryptoData = otherCryptoData;
                    if (_cachedOtherCryptoData.ContainsKey("ethereum"))
                    {
                        Debug.WriteLine("DEBUG: RefreshCryptoData - 'ethereum' found in cached data.");
                    }
                    else
                    {
                        Debug.WriteLine("DEBUG: RefreshCryptoData - 'ethereum' NOT found in cached data!");
                    }
                    // 更新轮换ID列表，确保不包含比特币（比特币是第一行）
                    _rotatingCryptoIds = otherCryptoData.Keys
                                                       .Where(id => !id.Equals("bitcoin", StringComparison.OrdinalIgnoreCase))
                                                       .ToList();

                    Debug.WriteLine("DEBUG: RefreshCryptoData - Rotating crypto IDs list:");
                    if (_rotatingCryptoIds.Any())
                    {
                        foreach (var id in _rotatingCryptoIds)
                        {
                            Debug.WriteLine($"  - {id}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("  (No secondary cryptos for rotation in list)");
                    }

                    // 确保索引不越界
                    if (_currentRotatingCryptoIndex >= _rotatingCryptoIds.Count)
                    {
                        _currentRotatingCryptoIndex = 0; // 重置索引
                    }

                    // 更新 BTC 行显示
                    UpdateBtcDisplay(btcPrice, btcChange);

                    DisplayCurrentRotatingCrypto(); // 立即显示当前轮换的币种数据
                    _rotationTimer.Start(); // 重新启动轮换定时器，以防其在编辑后被停止
                }
                else
                {
                    lblBtcData.Text = "BTC: 数据无效或部分数据缺失";
                    lblBtcData.ForeColor = Color.DarkGray;
                    lblOtherCryptoData.Text = "次要币种: 数据无效或部分数据缺失";
                    lblOtherCryptoData.ForeColor = Color.DarkGray;
                    _rotationTimer.Stop(); // 如果数据无效，停止轮换
                }
            }
            catch (Exception ex)
            {
                lblBtcData.Text = "BTC: 网络错误或API问题";
                lblBtcData.ForeColor = Color.OrangeRed;
                lblOtherCryptoData.Text = "次要币种: 网络错误或API问题";
                lblOtherCryptoData.ForeColor = Color.OrangeRed;
                Debug.WriteLine($"Error during data refresh: {ex.Message}");
                _rotationTimer.Stop(); // 如果发生错误，停止轮换
            }
        }

        // =========================================================================
        // 新增方法 - 更新 BTC 行显示并检查报警
        // =========================================================================
        private void UpdateBtcDisplay(decimal btcPrice, decimal btcChange)
        {
            lblBtcData.Text = $"BTC: ${btcPrice:F2} ({btcChange:F2}%)";

            // 根据涨跌幅设置 BTC 文本颜色
            if (!_isBtcBlinking) // 如果不在闪烁，才根据涨跌幅设置颜色
            {
                if (btcChange >= 0)
                {
                    lblBtcData.ForeColor = Color.DarkOliveGreen;
                }
                else
                {
                    lblBtcData.ForeColor = Color.Red;
                }
                _originalBtcColor = lblBtcData.ForeColor; // 确保每次更新时都记录当前颜色
            }

            // 检查报警条件
            CheckBtcAlert(btcPrice);
        }

        // =========================================================================
        // 方法 - 轮换定时器 Tick 事件
        // =========================================================================
        private void RotationTimer_Tick(object? sender, EventArgs e)
        {
            // 如果没有可轮换的币种，或者数据无效，则不进行轮换
            if (!_rotatingCryptoIds.Any() || !_cachedOtherCryptoData.Any())
            {
                DisplayCurrentRotatingCrypto(true); // 即使没有数据也尝试更新，可能显示N/A
                return;
            }

            _currentRotatingCryptoIndex = (_currentRotatingCryptoIndex + 1) % _rotatingCryptoIds.Count;
            DisplayCurrentRotatingCrypto();
        }

        // =========================================================================
        // 方法 - 显示当前轮换的交易对数据
        // =========================================================================
        // 增加一个可选参数 forceNoRotation 来处理没有可轮换币种的情况
        private void DisplayCurrentRotatingCrypto(bool forceNoRotation = false)
        {
            string secondLine = "N/A"; // 默认N/A

            // 处理第二行轮换数据
            decimal currentSecondLineChange = 0; // 初始化为 0
            if (_rotatingCryptoIds.Any() && _currentRotatingCryptoIndex < _rotatingCryptoIds.Count && !forceNoRotation)
            {
                string currentCryptoId = _rotatingCryptoIds[_currentRotatingCryptoIndex];
                if (_cachedOtherCryptoData.TryGetValue(currentCryptoId, out CryptoCurrencyDetails? details) && details != null)
                {
                    secondLine = $"{currentCryptoId.ToUpper()}: ${details.usd:F2} ({details.usd_24h_change ?? 0:F2}%)";
                    currentSecondLineChange = details.usd_24h_change ?? 0; // 获取第二行币种的24小时变化

                    Debug.WriteLine($"DEBUG: DisplayCurrentRotatingCrypto - Displaying {currentCryptoId}, Change = {currentSecondLineChange:F4}%");
                }
                else
                {
                    secondLine = $"{currentCryptoId.ToUpper()}: N/A (数据缺失)";
                    Debug.WriteLine($"DEBUG: DisplayCurrentRotatingCrypto - Data missing for {currentCryptoId}.");
                }
            }
            else if (forceNoRotation)
            {
                secondLine = "第二行: N/A (无币种或数据)";
                currentSecondLineChange = 0; // 此时涨跌幅也视为0
            }

            lblOtherCryptoData.Text = secondLine; // 更新第二行标签

            // 根据涨跌幅设置第二行文本颜色
            if (currentSecondLineChange >= 0)
            {
                lblOtherCryptoData.ForeColor = Color.DarkOliveGreen;
            }
            else
            {
                lblOtherCryptoData.ForeColor = Color.Red;
            }
        }

        // =========================================================================
        // 方法 - 打开编辑第二行交易对的窗口
        // =========================================================================
        private void EditSecondaryCryptos()
        {
            // 停止数据刷新、轮换和置顶定时器，避免在编辑时触发更新导致数据不一致或焦点冲突
            dataRefreshTimer.Stop();
            _rotationTimer.Stop();
            _topMostEnforcerTimer.Stop();
            StopBtcBlinking(); // 停止闪烁，避免干扰

            using (var editorForm = new CryptoEditorForm())
            {
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    // 如果用户点击了“确定”并保存了更改
                    // 强制刷新API数据并更新轮换列表和UI
                    _ = RefreshCryptoData();
                }
                else
                {
                    // 如果用户点击了“取消”或关闭了编辑窗口，重新启动数据和轮换定时器
                    dataRefreshTimer.Start();
                    _rotationTimer.Start();
                }
            }
            // 无论编辑窗口如何关闭，都重新启动置顶强制刷新定时器
            _topMostEnforcerTimer.Start();
            ForceWindowToTop(); // 确保在编辑窗口关闭后立即恢复置顶
        }

        // =========================================================================
        // 新增方法 - 设置 BTC 报警价格
        // =========================================================================
        private void SetBtcAlertPrice()
        {
            // 停止所有定时器，避免在输入时刷新或闪烁
            dataRefreshTimer.Stop();
            _rotationTimer.Stop();
            _topMostEnforcerTimer.Stop();
            StopBtcBlinking(); // 停止闪烁

            using (var inputForm = new AlertPriceInputForm(_btcAlertPrice))
            {
                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    _btcAlertPrice = inputForm.AlertPrice;
                    _btcAlertEnabled = true; // 设置报警价格后自动启用报警
                    SaveAlertSettings(); // 保存到 app.config
                    MessageBox.Show($"BTC 报警价格已设置为: ${_btcAlertPrice:F2}。报警功能已启用。", "设置成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            // 重新启动定时器
            dataRefreshTimer.Start();
            _rotationTimer.Start();
            _topMostEnforcerTimer.Start();
            ForceWindowToTop(); // 恢复置顶
            // 立即刷新数据，检查是否触发报警
            _ = RefreshCryptoData();
        }

        // =========================================================================
        // 新增方法 - 关闭 BTC 闪烁
        // =========================================================================
        private void StopBtcBlinking()
        {
            if (_isBtcBlinking)
            {
                _btcBlinkTimer.Stop();
                lblBtcData.ForeColor = _originalBtcColor; // 恢复到原始颜色
                _isBtcBlinking = false;
                _btcAlertEnabled = false; // 关闭闪烁时，也关闭报警功能
                SaveAlertSettings(); // 保存状态到 app.config
                MessageBox.Show("BTC 闪烁已关闭，报警功能已禁用。", "报警关闭", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // =========================================================================
        // 新增方法 - 检查 BTC 价格是否达到报警条件
        // =========================================================================
        private void CheckBtcAlert(decimal currentBtcPrice)
        {
            if (_btcAlertEnabled && _btcAlertPrice > 0) // 确保报警功能启用且设置了有效价格
            {
                // 这里假设当实时价格达到或超过报警价格时触发报警
                // 您可以根据需求修改为低于报警价格触发，或者一个价格区间
                if (currentBtcPrice >= _btcAlertPrice)
                {
                    if (!_isBtcBlinking) // 如果尚未闪烁，则开始闪烁
                    {
                        _isBtcBlinking = true;
                        _btcBlinkTimer.Start();
                        Debug.WriteLine("DEBUG: BTC 价格达到报警值，开始闪烁！");
                    }
                }
                else
                {
                    // 如果价格低于报警值，且当前正在闪烁，则停止闪烁
                    if (_isBtcBlinking)
                    {
                        StopBtcBlinking();
                        Debug.WriteLine("DEBUG: BTC 价格低于报警值，停止闪烁。");
                    }
                }
            }
            else
            {
                // 如果报警功能未启用或价格无效，确保停止闪烁
                if (_isBtcBlinking)
                {
                    StopBtcBlinking();
                }
            }
        }

        // =========================================================================
        // 新增方法 - BTC 闪烁定时器 Tick 事件
        // =========================================================================
        private void BtcBlinkTimer_Tick(object? sender, EventArgs e)
        {
            // 切换颜色，实现闪烁效果
            if (lblBtcData.ForeColor == _originalBtcColor)
            {
                lblBtcData.ForeColor = Color.Cyan; // 闪烁颜色，可以自定义
            }
            else
            {
                lblBtcData.ForeColor = _originalBtcColor;
            }
        }

        // =========================================================================
        // 新增方法 - 从 App.config 加载报警设置
        // =========================================================================
        private void LoadAlertSettings()
        {
            ConfigurationManager.RefreshSection("appSettings"); // 确保获取最新配置
            if (decimal.TryParse(ConfigurationManager.AppSettings["BtcAlertPrice"], out decimal price))
            {
                _btcAlertPrice = price;
            }
            else
            {
                _btcAlertPrice = 0; // 默认值
            }

            if (bool.TryParse(ConfigurationManager.AppSettings["BtcAlertEnabled"], out bool enabled))
            {
                _btcAlertEnabled = enabled;
            }
            else
            {
                _btcAlertEnabled = false; // 默认值
            }
            Debug.WriteLine($"DEBUG: Loaded Alert Settings - Price: {_btcAlertPrice}, Enabled: {_btcAlertEnabled}");
        }

        // =========================================================================
        // 新增方法 - 保存报警设置到 App.config
        // =========================================================================
        private void SaveAlertSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            AppSettingsSection appSettings = config.AppSettings;

            if (appSettings.Settings["BtcAlertPrice"] == null)
            {
                appSettings.Settings.Add("BtcAlertPrice", _btcAlertPrice.ToString());
            }
            else
            {
                appSettings.Settings["BtcAlertPrice"].Value = _btcAlertPrice.ToString();
            }

            if (appSettings.Settings["BtcAlertEnabled"] == null)
            {
                appSettings.Settings.Add("BtcAlertEnabled", _btcAlertEnabled.ToString());
            }
            else
            {
                appSettings.Settings["BtcAlertEnabled"].Value = _btcAlertEnabled.ToString();
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings"); // 强制重新加载
            Debug.WriteLine($"DEBUG: Saved Alert Settings - Price: {_btcAlertPrice}, Enabled: {_btcAlertEnabled}");
        }


        // =========================================================================
        // 方法 - 从CoinGecko API获取加密货币价格
        // =========================================================================
        private async Task<(decimal bitcoinPrice, decimal bitcoinChange, Dictionary<string, CryptoCurrencyDetails> otherCryptoData, bool isDataValid)> GetCryptoPrices()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            string baseUrl = ConfigurationManager.AppSettings["CoinGeckoApiBaseUrl"] ?? "https://api.coingecko.com/api/v3/";
            string vsCurrencies = ConfigurationManager.AppSettings["VsCurrencies"] ?? "usd";

            string allCryptoIds = "bitcoin";

            // 强制从 ConfigurationManager 重新读取，以防 app.config 被外部修改（例如通过编辑窗口）
            ConfigurationManager.RefreshSection("appSettings"); // 确保获取最新配置
            string editableCryptoIds = ConfigurationManager.AppSettings["EditableCryptoCurrencyIds"] ?? "ethereum";

            // 如果有可编辑的币种，则添加到请求ID中，确保不重复且有逗号分隔
            if (!string.IsNullOrWhiteSpace(editableCryptoIds))
            {
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ids.Add("bitcoin"); // 确保比特币也在集合中
                foreach (var id in editableCryptoIds.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    ids.Add(id.Trim());
                }
                allCryptoIds = string.Join(",", ids);
            }
            Debug.WriteLine($"DEBUG: GetCryptoPrices - Requesting IDs: {allCryptoIds}");

            string apiUrl = $"{baseUrl}simple/price?ids={allCryptoIds}&vs_currencies={vsCurrencies}&include_24hr_change=true";

            decimal btcPrice = 0;
            decimal btcChange = 0;
            Dictionary<string, CryptoCurrencyDetails> otherCryptoData = new Dictionary<string, CryptoCurrencyDetails>(StringComparer.OrdinalIgnoreCase);
            bool isDataValid = false;

            try
            {
                var response = await client.GetStringAsync(apiUrl);
                Debug.WriteLine($"DEBUG: GetCryptoPrices - Raw API Response (first 200 chars): {response.Substring(0, Math.Min(response.Length, 200))}");

                var data = JsonSerializer.Deserialize<Dictionary<string, CryptoCurrencyDetails>>(response);

                if (data != null)
                {
                    Debug.WriteLine("DEBUG: GetCryptoPrices - Fetched data from API (keys):");
                    foreach (var entry in data) // 现在可以正常遍历了
                    {
                        Debug.WriteLine($"  - {entry.Key}");
                    }

                    // 尝试获取比特币数据
                    if (data.TryGetValue("bitcoin", out CryptoCurrencyDetails? bitcoinDetails) && bitcoinDetails != null)
                    {
                        btcPrice = bitcoinDetails.usd;
                        btcChange = bitcoinDetails.usd_24h_change ?? 0;
                        isDataValid = true; // 只要比特币数据有效，就认为基础数据有效
                    }
                    else
                    {
                        Debug.WriteLine("API响应中缺少比特币数据。");
                        isDataValid = false;
                    }

                    // 遍历所有获取到的数据，填充 otherCryptoData 字典
                    foreach (var entry in data)
                    {
                        if (entry.Value != null)
                        {
                            otherCryptoData[entry.Key] = entry.Value;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("API响应数据为空或无法反序列化。");
                    isDataValid = false;
                }

                return (btcPrice, btcChange, otherCryptoData, isDataValid);
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine($"网络请求错误: {e.Message}");
                return (0, 0, new Dictionary<string, CryptoCurrencyDetails>(), false);
            }
            catch (JsonException e)
            {
                Debug.WriteLine($"JSON解析错误: {e.Message}");
                Debug.WriteLine($"原始响应（用于调试，可能为空）: {e.Message}");
                return (0, 0, new Dictionary<string, CryptoCurrencyDetails>(), false);
            }
            catch (Exception e) // 捕获其他未知错误
            {
                Debug.WriteLine($"发生未知错误: {e.Message}");
                return (0, 0, new Dictionary<string, CryptoCurrencyDetails>(), false);
            }
        }

        // =========================================================================
        // 方法 - 强制窗口始终置顶
        // =========================================================================
        private void ForceWindowToTop()
        {
            // 调用 SetWindowPos 将窗口置于 HWND_TOPMOST (所有其他窗口之上)
            // SWP_NOSIZE 和 SWP_NOMOVE 确保不改变窗口大小和位置。
            // SWP_SHOWWINDOW 确保窗口可见。
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            // Debug.WriteLine("DEBUG: ForceWindowToTop called."); // 如果输出频繁，可以注释掉这行调试信息
        }
    }
}