using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms; // ���� System.Windows.Forms.Timer
using System.Configuration; // ���ڶ�ȡ app.config
using System.Collections.Generic; // ���� Dictionary �� HashSet
using System.Linq; // ���� Linq �������� .ToList()
using System.Diagnostics;
using System.Runtime.InteropServices; // ���� P/Invoke

namespace CryptoMonitorApp
{
    //API
    //https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum,solana,okb,dogecoin,xrp&vs_currencies=usd&include_24hr_change=true
    //
    public partial class MainForm : Form
    {
        // =========================================================================
        // ��Ա���� - ���ڴ����϶�
        // =========================================================================
        private bool isDragging = false;
        private Point lastCursorPos;
        // =========================================================================
        // ����: �����Ĳ˵���ʱ��ͣ�ö���ʱ��
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
        // ����: �����Ĳ˵��ر�ʱ�ָ��ö���ʱ��
        // =========================================================================
        private void ContextMenu_Closed(object? sender, EventArgs e)
        {
            // �˵��رպ�������ڿɼ��������������ö���ʱ����ǿ���ö�һ��
            if (this.Visible && !_topMostEnforcerTimer.Enabled)
            {
                _topMostEnforcerTimer.Start();
                ForceWindowToTop(); // �����ö�һ�Σ��Է��ڹر��ڼ䱻�������ڸ���
                Debug.WriteLine("DEBUG: _topMostEnforcerTimer started and ForceWindowToTop called due to ContextMenu_Closed.");
            }
        }
        // =========================================================================
        // ��Ա���� - �����ֻ���ʾ�ڶ��н��׶�
        // =========================================================================
        private Dictionary<string, CryptoCurrencyDetails> _cachedOtherCryptoData = new Dictionary<string, CryptoCurrencyDetails>(StringComparer.OrdinalIgnoreCase);
        private List<string> _rotatingCryptoIds = new List<string>();
        private int _currentRotatingCryptoIndex = 0;

        private System.Windows.Forms.Timer _rotationTimer; // ���ڿ����ֻ��Ķ�ʱ��

        // =========================================================================
        // ����: ���ڳ���ǿ���ö��Ķ�ʱ��
        // =========================================================================
        private System.Windows.Forms.Timer _topMostEnforcerTimer;

        // =========================================================================
        // ����: BTC ����������س�Ա����
        // =========================================================================
        private decimal _btcAlertPrice; // BTC �����۸�
        private bool _btcAlertEnabled; // BTC ���������Ƿ�����
        private System.Windows.Forms.Timer _btcBlinkTimer; // BTC ��˸��ʱ��
        private bool _isBtcBlinking = false; // BTC �Ƿ�������˸
        private Color _originalBtcColor; // ��¼ BTC ��ǩ��ԭʼ��ɫ

        // =========================================================================
        // Windows API ���� - ����ǿ�ƴ����ö�
        // =========================================================================
        // ���� HWND ����������ֵ
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); // �����������з� TopMost ����֮��
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2); // ������������ TopMost ����֮�� (�ָ�����)

        // ���� SetWindowPos �� Flags
        const uint SWP_NOSIZE = 0x0001; // ���ı䴰�ڴ�С
        const uint SWP_NOMOVE = 0x0002; // ���ı䴰��λ��
        const uint SWP_SHOWWINDOW = 0x0040; // ��ʾ����

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        // =========================================================================

        // =========================================================================
        // ���캯�� - �����ʼ��
        // =========================================================================
        public MainForm()
        {
            InitializeComponent(); // ��������ɵ������ʼ������

            // �ֶ���ʼ�� trayIcon����Ϊ Designer.cs ͨ�������� NotifyIcon
            trayIcon = new NotifyIcon();

            // =====================================================================
            // 1. ���ô�������
            // =====================================================================
            this.FormBorderStyle = FormBorderStyle.None;         // �ޱ߿�
            this.ShowInTaskbar = false;                          // ������������ʾͼ��
            this.TopMost = true;                                 // �״�����Ϊʼ���ö��������ɶ�ʱ��ǿ��
            this.BackColor = Color.Black;                        // ����ɫ
            this.TransparencyKey = Color.Black;                  // ���ñ���ɫΪ͸������ʹ��͸��

            // ��ʽ���ô����С��ȷ�� SetWindowLocation ������ȷ������֤����ʾ��������
            this.Size = new Size(450, 70);

            // =====================================================================
            // 2. Ϊ lblBtcData �� lblOtherCryptoData ��ǩ����һЩ����
            // =====================================================================
            lblBtcData.Text = "BTC: ������...";
            lblBtcData.Font = new Font("Consolas", 12, FontStyle.Bold); // BTC�мӴ�
            lblBtcData.ForeColor = Color.White; // Ĭ�ϰ�ɫ

            lblOtherCryptoData.Text = "��Ҫ����: ������...";
            lblOtherCryptoData.Font = new Font("Consolas", 12, FontStyle.Regular); // ����������
            lblOtherCryptoData.ForeColor = Color.White; // Ĭ�ϰ�ɫ

            _originalBtcColor = lblBtcData.ForeColor; // ��¼ BTC ��ǩ��ԭʼ��ɫ

            // =====================================================================
            // 3. ��ʼ���������ö�ǿ��ˢ�¶�ʱ��
            // =====================================================================
            _topMostEnforcerTimer = new System.Windows.Forms.Timer();
            _topMostEnforcerTimer.Interval = 100; // ÿ 100 ����ǿ���ö�һ��
            _topMostEnforcerTimer.Tick += (s, e) => ForceWindowToTop();
            _topMostEnforcerTimer.Start(); // ������ʱ��

            // =====================================================================
            // 4. ��ʼ�� BTC ��˸��ʱ��
            // =====================================================================
            _btcBlinkTimer = new System.Windows.Forms.Timer();
            _btcBlinkTimer.Interval = 500; // ÿ 500 ������˸һ��
            _btcBlinkTimer.Tick += BtcBlinkTimer_Tick;


            // =====================================================================
            // 5. �����¼�����
            // =====================================================================
            this.Load += async (s, e) =>
            {
                SetWindowLocation();     // ���ó�ʼλ��
                ForceWindowToTop();      // ȷ�����ڼ���ʱ�����ö�

                // ��ʼ���ֻ���ʱ��
                _rotationTimer = new System.Windows.Forms.Timer();
                _rotationTimer.Interval = 5000; // ÿ5���ֻ�һ��
                _rotationTimer.Tick += RotationTimer_Tick;
                _rotationTimer.Start(); // �����ֻ���ʱ��

                // �� app.config ��ȡ��������
                LoadAlertSettings();

                await RefreshCryptoData(); // �״μ���ʱ����ˢ������

                // �� app.config ��ȡ����ˢ�¼�������API���ݻ�ȡ�Ķ�ʱ����
                if (int.TryParse(ConfigurationManager.AppSettings["RefreshIntervalMs"], out int interval))
                {
                    dataRefreshTimer.Interval = interval;
                }
                else
                {
                    dataRefreshTimer.Interval = 30000; // Ĭ��30��
                }
                dataRefreshTimer.Start();  // ��������ˢ�¶�ʱ��

                trayIcon.Visible = true;    // ��������ʱ������ͼ��ɼ�
            };

            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true; // ȡ���رգ�תΪ���ص�����
                    this.Hide();
                }
            };


            // =====================================================================
            // 6. ��ʱ���¼�����
            // =====================================================================
            dataRefreshTimer.Tick += async (s, e) => await RefreshCryptoData();


            // =====================================================================
            // 7. ϵͳ����ͼ���¼�����
            // =====================================================================
            if (trayIcon.Icon == null)
            {
                try
                {
                    // ȷ�� AppIcon.ico ��������Ŀ��Ŀ¼�����Ŀ¼
                    trayIcon.Icon = new Icon("AppIcon.ico");
                }
                catch
                {
                    trayIcon.Icon = SystemIcons.Application; // ���˵�ϵͳĬ��ͼ��
                }
            }
            trayIcon.Text = "���ܻ��Ҽ����"; // �����ͣ��ͼ���ϵ���ʾ�ı�

            trayIcon.DoubleClick += (s, e) =>
            {
                if (this.Visible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    SetWindowLocation(); // ���¼���λ�ã�ȷ����ʾ����ȷλ��
                    ForceWindowToTop(); // ��ʾʱҲǿ���ö�
                }
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("��ʾ/����", null, (s, e) =>
            {
                if (this.Visible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    SetWindowLocation(); // ���¼���λ��
                    ForceWindowToTop(); // ��ʾʱҲǿ���ö�
                }
            });
            contextMenu.Items.Add("�༭�ڶ��н��׶�", null, (s, e) => EditSecondaryCryptos());
            contextMenu.Items.Add("����BTC�����۸�", null, (s, e) => SetBtcAlertPrice()); // ���������۸�����ѡ��
            contextMenu.Items.Add("�ر�BTC��˸", null, (s, e) => StopBtcBlinking()); // �����ر���˸ѡ��

            contextMenu.Items.Add("-"); // �ָ���
            contextMenu.Items.Add("�˳�", null, (s, e) =>
            {
                trayIcon.Visible = false; // �˳�ǰ��������ͼ��
                Application.Exit();        // �˳�Ӧ�ó���
            });
            trayIcon.ContextMenuStrip = contextMenu; // �������Ĳ˵��󶨵�����ͼ��
            // =========================================================================
            // ����: Ϊ contextMenu ��� Opened �� Closed �¼�������
            // =========================================================================
            contextMenu.Opened += ContextMenu_Opened;
            contextMenu.Closed += ContextMenu_Closed;

            // =====================================================================
            // 8. �����϶����� (����ͬʱӦ����������ǩ)
            // =====================================================================
            this.MouseDown += OnFormMouseDown;
            this.MouseMove += OnFormMouseMove;
            this.MouseUp += OnFormMouseUp;

            // ȷ����ǩҲ���Դ����϶�
            lblBtcData.MouseDown += OnFormMouseDown;
            lblBtcData.MouseMove += OnFormMouseMove;
            lblBtcData.MouseUp += OnFormMouseUp;

            lblOtherCryptoData.MouseDown += OnFormMouseDown;
            lblOtherCryptoData.MouseMove += OnFormMouseMove;
            lblOtherCryptoData.MouseUp += OnFormMouseUp;
        }

        // =========================================================================
        // �϶��¼�������
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
                // ����Ǳ�ǩ�������϶�����Ҫ�����λ��ת��Ϊ��������
                Control? control = sender as Control;
                Point currentScreenPos = Cursor.Position;
                Point newLocation = new Point(currentScreenPos.X - lastCursorPos.X,
                                              currentScreenPos.Y - lastCursorPos.Y);

                if (control != null && control != this)
                {
                    // ������ӿؼ�����Ҫ��ȥ�ӿؼ�����ڸ��ؼ���λ��
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
        // ���� - ���ô����ʼλ��
        // =========================================================================
        private void SetWindowLocation()
        {
            Rectangle totalScreenBounds = Screen.PrimaryScreen.Bounds;
            // ������������С����������Ԫ�������ռ�� 150 ���ؿ�ȣ���һЩ�߾ࡣ
            int widgetPanelWidth = 150;
            int margin = 10;

            // �����ڶ�λ�����½ǣ��ܿ����ܵ����С������
            int newX = totalScreenBounds.Left + widgetPanelWidth + margin; // ����ƫ��
            int newY = totalScreenBounds.Bottom - this.Height - margin; // ��ײ�ƫ��

            this.Location = new Point(newX, newY);
        }

        // =========================================================================
        // ���� - ˢ�¼��ܻ������ݲ�����UI
        // =========================================================================
        private async Task RefreshCryptoData()
        {
            // �������б�ǩ���ı�Ϊ��������...��
            lblBtcData.Text = "BTC: ������...";
            lblBtcData.ForeColor = Color.Yellow;
            lblOtherCryptoData.Text = "��Ҫ����: ������...";
            lblOtherCryptoData.ForeColor = Color.Yellow;

            try
            {
                var (btcPrice, btcChange, otherCryptoData, isDataValid) = await GetCryptoPrices();

                Debug.WriteLine($"DEBUG: RefreshCryptoData - BTC Change = {btcChange:F4}%");

                if (isDataValid)
                {
                    // ����������������
                    _cachedOtherCryptoData = otherCryptoData;
                    if (_cachedOtherCryptoData.ContainsKey("ethereum"))
                    {
                        Debug.WriteLine("DEBUG: RefreshCryptoData - 'ethereum' found in cached data.");
                    }
                    else
                    {
                        Debug.WriteLine("DEBUG: RefreshCryptoData - 'ethereum' NOT found in cached data!");
                    }
                    // �����ֻ�ID�б�ȷ�����������رң����ر��ǵ�һ�У�
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

                    // ȷ��������Խ��
                    if (_currentRotatingCryptoIndex >= _rotatingCryptoIds.Count)
                    {
                        _currentRotatingCryptoIndex = 0; // ��������
                    }

                    // ���� BTC ����ʾ
                    UpdateBtcDisplay(btcPrice, btcChange);

                    DisplayCurrentRotatingCrypto(); // ������ʾ��ǰ�ֻ��ı�������
                    _rotationTimer.Start(); // ���������ֻ���ʱ�����Է����ڱ༭��ֹͣ
                }
                else
                {
                    lblBtcData.Text = "BTC: ������Ч�򲿷�����ȱʧ";
                    lblBtcData.ForeColor = Color.DarkGray;
                    lblOtherCryptoData.Text = "��Ҫ����: ������Ч�򲿷�����ȱʧ";
                    lblOtherCryptoData.ForeColor = Color.DarkGray;
                    _rotationTimer.Stop(); // ���������Ч��ֹͣ�ֻ�
                }
            }
            catch (Exception ex)
            {
                lblBtcData.Text = "BTC: ��������API����";
                lblBtcData.ForeColor = Color.OrangeRed;
                lblOtherCryptoData.Text = "��Ҫ����: ��������API����";
                lblOtherCryptoData.ForeColor = Color.OrangeRed;
                Debug.WriteLine($"Error during data refresh: {ex.Message}");
                _rotationTimer.Stop(); // �����������ֹͣ�ֻ�
            }
        }

        // =========================================================================
        // �������� - ���� BTC ����ʾ����鱨��
        // =========================================================================
        private void UpdateBtcDisplay(decimal btcPrice, decimal btcChange)
        {
            lblBtcData.Text = $"BTC: ${btcPrice:F2} ({btcChange:F2}%)";

            // �����ǵ������� BTC �ı���ɫ
            if (!_isBtcBlinking) // ���������˸���Ÿ����ǵ���������ɫ
            {
                if (btcChange >= 0)
                {
                    lblBtcData.ForeColor = Color.DarkOliveGreen;
                }
                else
                {
                    lblBtcData.ForeColor = Color.Red;
                }
                _originalBtcColor = lblBtcData.ForeColor; // ȷ��ÿ�θ���ʱ����¼��ǰ��ɫ
            }

            // ��鱨������
            CheckBtcAlert(btcPrice);
        }

        // =========================================================================
        // ���� - �ֻ���ʱ�� Tick �¼�
        // =========================================================================
        private void RotationTimer_Tick(object? sender, EventArgs e)
        {
            // ���û�п��ֻ��ı��֣�����������Ч���򲻽����ֻ�
            if (!_rotatingCryptoIds.Any() || !_cachedOtherCryptoData.Any())
            {
                DisplayCurrentRotatingCrypto(true); // ��ʹû������Ҳ���Ը��£�������ʾN/A
                return;
            }

            _currentRotatingCryptoIndex = (_currentRotatingCryptoIndex + 1) % _rotatingCryptoIds.Count;
            DisplayCurrentRotatingCrypto();
        }

        // =========================================================================
        // ���� - ��ʾ��ǰ�ֻ��Ľ��׶�����
        // =========================================================================
        // ����һ����ѡ���� forceNoRotation ������û�п��ֻ����ֵ����
        private void DisplayCurrentRotatingCrypto(bool forceNoRotation = false)
        {
            string secondLine = "N/A"; // Ĭ��N/A

            // ����ڶ����ֻ�����
            decimal currentSecondLineChange = 0; // ��ʼ��Ϊ 0
            if (_rotatingCryptoIds.Any() && _currentRotatingCryptoIndex < _rotatingCryptoIds.Count && !forceNoRotation)
            {
                string currentCryptoId = _rotatingCryptoIds[_currentRotatingCryptoIndex];
                if (_cachedOtherCryptoData.TryGetValue(currentCryptoId, out CryptoCurrencyDetails? details) && details != null)
                {
                    secondLine = $"{currentCryptoId.ToUpper()}: ${details.usd:F2} ({details.usd_24h_change ?? 0:F2}%)";
                    currentSecondLineChange = details.usd_24h_change ?? 0; // ��ȡ�ڶ��б��ֵ�24Сʱ�仯

                    Debug.WriteLine($"DEBUG: DisplayCurrentRotatingCrypto - Displaying {currentCryptoId}, Change = {currentSecondLineChange:F4}%");
                }
                else
                {
                    secondLine = $"{currentCryptoId.ToUpper()}: N/A (����ȱʧ)";
                    Debug.WriteLine($"DEBUG: DisplayCurrentRotatingCrypto - Data missing for {currentCryptoId}.");
                }
            }
            else if (forceNoRotation)
            {
                secondLine = "�ڶ���: N/A (�ޱ��ֻ�����)";
                currentSecondLineChange = 0; // ��ʱ�ǵ���Ҳ��Ϊ0
            }

            lblOtherCryptoData.Text = secondLine; // ���µڶ��б�ǩ

            // �����ǵ������õڶ����ı���ɫ
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
        // ���� - �򿪱༭�ڶ��н��׶ԵĴ���
        // =========================================================================
        private void EditSecondaryCryptos()
        {
            // ֹͣ����ˢ�¡��ֻ����ö���ʱ���������ڱ༭ʱ�������µ������ݲ�һ�»򽹵��ͻ
            dataRefreshTimer.Stop();
            _rotationTimer.Stop();
            _topMostEnforcerTimer.Stop();
            StopBtcBlinking(); // ֹͣ��˸���������

            using (var editorForm = new CryptoEditorForm())
            {
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    // ����û�����ˡ�ȷ�����������˸���
                    // ǿ��ˢ��API���ݲ������ֻ��б��UI
                    _ = RefreshCryptoData();
                }
                else
                {
                    // ����û�����ˡ�ȡ������ر��˱༭���ڣ������������ݺ��ֻ���ʱ��
                    dataRefreshTimer.Start();
                    _rotationTimer.Start();
                }
            }
            // ���۱༭������ιرգ������������ö�ǿ��ˢ�¶�ʱ��
            _topMostEnforcerTimer.Start();
            ForceWindowToTop(); // ȷ���ڱ༭���ڹرպ������ָ��ö�
        }

        // =========================================================================
        // �������� - ���� BTC �����۸�
        // =========================================================================
        private void SetBtcAlertPrice()
        {
            // ֹͣ���ж�ʱ��������������ʱˢ�»���˸
            dataRefreshTimer.Stop();
            _rotationTimer.Stop();
            _topMostEnforcerTimer.Stop();
            StopBtcBlinking(); // ֹͣ��˸

            using (var inputForm = new AlertPriceInputForm(_btcAlertPrice))
            {
                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    _btcAlertPrice = inputForm.AlertPrice;
                    _btcAlertEnabled = true; // ���ñ����۸���Զ����ñ���
                    SaveAlertSettings(); // ���浽 app.config
                    MessageBox.Show($"BTC �����۸�������Ϊ: ${_btcAlertPrice:F2}���������������á�", "���óɹ�", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            // ����������ʱ��
            dataRefreshTimer.Start();
            _rotationTimer.Start();
            _topMostEnforcerTimer.Start();
            ForceWindowToTop(); // �ָ��ö�
            // ����ˢ�����ݣ�����Ƿ񴥷�����
            _ = RefreshCryptoData();
        }

        // =========================================================================
        // �������� - �ر� BTC ��˸
        // =========================================================================
        private void StopBtcBlinking()
        {
            if (_isBtcBlinking)
            {
                _btcBlinkTimer.Stop();
                lblBtcData.ForeColor = _originalBtcColor; // �ָ���ԭʼ��ɫ
                _isBtcBlinking = false;
                _btcAlertEnabled = false; // �ر���˸ʱ��Ҳ�رձ�������
                SaveAlertSettings(); // ����״̬�� app.config
                MessageBox.Show("BTC ��˸�ѹرգ����������ѽ��á�", "�����ر�", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // =========================================================================
        // �������� - ��� BTC �۸��Ƿ�ﵽ��������
        // =========================================================================
        private void CheckBtcAlert(decimal currentBtcPrice)
        {
            if (_btcAlertEnabled && _btcAlertPrice > 0) // ȷ������������������������Ч�۸�
            {
                // ������赱ʵʱ�۸�ﵽ�򳬹������۸�ʱ��������
                // �����Ը��������޸�Ϊ���ڱ����۸񴥷�������һ���۸�����
                if (currentBtcPrice >= _btcAlertPrice)
                {
                    if (!_isBtcBlinking) // �����δ��˸����ʼ��˸
                    {
                        _isBtcBlinking = true;
                        _btcBlinkTimer.Start();
                        Debug.WriteLine("DEBUG: BTC �۸�ﵽ����ֵ����ʼ��˸��");
                    }
                }
                else
                {
                    // ����۸���ڱ���ֵ���ҵ�ǰ������˸����ֹͣ��˸
                    if (_isBtcBlinking)
                    {
                        StopBtcBlinking();
                        Debug.WriteLine("DEBUG: BTC �۸���ڱ���ֵ��ֹͣ��˸��");
                    }
                }
            }
            else
            {
                // �����������δ���û�۸���Ч��ȷ��ֹͣ��˸
                if (_isBtcBlinking)
                {
                    StopBtcBlinking();
                }
            }
        }

        // =========================================================================
        // �������� - BTC ��˸��ʱ�� Tick �¼�
        // =========================================================================
        private void BtcBlinkTimer_Tick(object? sender, EventArgs e)
        {
            // �л���ɫ��ʵ����˸Ч��
            if (lblBtcData.ForeColor == _originalBtcColor)
            {
                lblBtcData.ForeColor = Color.Cyan; // ��˸��ɫ�������Զ���
            }
            else
            {
                lblBtcData.ForeColor = _originalBtcColor;
            }
        }

        // =========================================================================
        // �������� - �� App.config ���ر�������
        // =========================================================================
        private void LoadAlertSettings()
        {
            ConfigurationManager.RefreshSection("appSettings"); // ȷ����ȡ��������
            if (decimal.TryParse(ConfigurationManager.AppSettings["BtcAlertPrice"], out decimal price))
            {
                _btcAlertPrice = price;
            }
            else
            {
                _btcAlertPrice = 0; // Ĭ��ֵ
            }

            if (bool.TryParse(ConfigurationManager.AppSettings["BtcAlertEnabled"], out bool enabled))
            {
                _btcAlertEnabled = enabled;
            }
            else
            {
                _btcAlertEnabled = false; // Ĭ��ֵ
            }
            Debug.WriteLine($"DEBUG: Loaded Alert Settings - Price: {_btcAlertPrice}, Enabled: {_btcAlertEnabled}");
        }

        // =========================================================================
        // �������� - ���汨�����õ� App.config
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
            ConfigurationManager.RefreshSection("appSettings"); // ǿ�����¼���
            Debug.WriteLine($"DEBUG: Saved Alert Settings - Price: {_btcAlertPrice}, Enabled: {_btcAlertEnabled}");
        }


        // =========================================================================
        // ���� - ��CoinGecko API��ȡ���ܻ��Ҽ۸�
        // =========================================================================
        private async Task<(decimal bitcoinPrice, decimal bitcoinChange, Dictionary<string, CryptoCurrencyDetails> otherCryptoData, bool isDataValid)> GetCryptoPrices()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            string baseUrl = ConfigurationManager.AppSettings["CoinGeckoApiBaseUrl"] ?? "https://api.coingecko.com/api/v3/";
            string vsCurrencies = ConfigurationManager.AppSettings["VsCurrencies"] ?? "usd";

            string allCryptoIds = "bitcoin";

            // ǿ�ƴ� ConfigurationManager ���¶�ȡ���Է� app.config ���ⲿ�޸ģ�����ͨ���༭���ڣ�
            ConfigurationManager.RefreshSection("appSettings"); // ȷ����ȡ��������
            string editableCryptoIds = ConfigurationManager.AppSettings["EditableCryptoCurrencyIds"] ?? "ethereum";

            // ����пɱ༭�ı��֣�����ӵ�����ID�У�ȷ�����ظ����ж��ŷָ�
            if (!string.IsNullOrWhiteSpace(editableCryptoIds))
            {
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ids.Add("bitcoin"); // ȷ�����ر�Ҳ�ڼ�����
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
                    foreach (var entry in data) // ���ڿ�������������
                    {
                        Debug.WriteLine($"  - {entry.Key}");
                    }

                    // ���Ի�ȡ���ر�����
                    if (data.TryGetValue("bitcoin", out CryptoCurrencyDetails? bitcoinDetails) && bitcoinDetails != null)
                    {
                        btcPrice = bitcoinDetails.usd;
                        btcChange = bitcoinDetails.usd_24h_change ?? 0;
                        isDataValid = true; // ֻҪ���ر�������Ч������Ϊ����������Ч
                    }
                    else
                    {
                        Debug.WriteLine("API��Ӧ��ȱ�ٱ��ر����ݡ�");
                        isDataValid = false;
                    }

                    // �������л�ȡ�������ݣ���� otherCryptoData �ֵ�
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
                    Debug.WriteLine("API��Ӧ����Ϊ�ջ��޷������л���");
                    isDataValid = false;
                }

                return (btcPrice, btcChange, otherCryptoData, isDataValid);
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine($"�����������: {e.Message}");
                return (0, 0, new Dictionary<string, CryptoCurrencyDetails>(), false);
            }
            catch (JsonException e)
            {
                Debug.WriteLine($"JSON��������: {e.Message}");
                Debug.WriteLine($"ԭʼ��Ӧ�����ڵ��ԣ�����Ϊ�գ�: {e.Message}");
                return (0, 0, new Dictionary<string, CryptoCurrencyDetails>(), false);
            }
            catch (Exception e) // ��������δ֪����
            {
                Debug.WriteLine($"����δ֪����: {e.Message}");
                return (0, 0, new Dictionary<string, CryptoCurrencyDetails>(), false);
            }
        }

        // =========================================================================
        // ���� - ǿ�ƴ���ʼ���ö�
        // =========================================================================
        private void ForceWindowToTop()
        {
            // ���� SetWindowPos ���������� HWND_TOPMOST (������������֮��)
            // SWP_NOSIZE �� SWP_NOMOVE ȷ�����ı䴰�ڴ�С��λ�á�
            // SWP_SHOWWINDOW ȷ�����ڿɼ���
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            // Debug.WriteLine("DEBUG: ForceWindowToTop called."); // ������Ƶ��������ע�͵����е�����Ϣ
        }
    }
}