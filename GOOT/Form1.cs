using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GOOT
{
    public partial class Form1 : Form
    {
        // --- BIẾN GIAO DIỆN ---
        private TabControl tabControl;
        private TabPage tabCountdown, tabTime, tabProcess, tabWeekly, tabAbout;
        private Button btnLangFlag;

        // Phần bảo mật
        private Button btnToggleSecurity;
        private GroupBox grpSecurity;
        private TextBox txtPassword;
        private CheckBox chkPassword;
        private Button btnCancel;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatusLeft;
        private ToolStripStatusLabel lblStatusRight;

        // Các biến nhập liệu thời gian
        private NumericUpDown numHoursCD, numMinutesCD, numSecondsCD;
        private NumericUpDown numHoursTime, numMinutesTime, numSecondsTime;

        // Tab Lịch biểu tuần
        private CheckedListBox chkListDays;
        private CheckBox chkSelectAllDays;
        private NumericUpDown numHoursWeekly, numMinutesWeekly;
        private Button btnStartWeekly;

        // Tab Chặn App
        private Label lblProcessHint;
        private TextBox txtProcessName;
        private Button btnStartCountdown, btnStartTime, btnStartProcess, btnDonate;

        // Tab Giới thiệu
        private Label lblAppInfo;
        private LinkLabel lnkAuthor;
        private LinkLabel lnkRepo;
        private CheckBox chkStartWithWindows;

        private NotifyIcon notifyIcon;
        private System.Windows.Forms.Timer mainTimer;
        private System.Windows.Forms.Timer clockTimer;

        // --- BIẾN LOGIC ---
        private DateTime _targetTime;
        private string _targetProcessName = "";
        private int _mode = 0; // 0: Idle, 1: Countdown, 2: SetTime, 3: Process, 4: Weekly
        private bool _isRunning = false;
        private bool _isEnglish = false;

        // Key Registry
        private const string REG_PATH = @"SOFTWARE\GOOT\Settings";

        // Kích thước Form
        private readonly Size _compactSize = new Size(500, 350);
        private readonly Size _expandedSize = new Size(500, 500);

        public Form1()
        {
            this.Text = "GOOT - Get Off On Time (v1.3.2)";
            this.Size = _compactSize;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            try { this.Icon = Properties.Resources.icon; } catch { this.Icon = SystemIcons.Application; }

            InitializeUI();
            InitializeLogic();
            UpdateLanguage();

            // Tải cấu hình cũ sau khi khởi tạo xong
            LoadSettings();
        }

        private void InitializeUI()
        {
            Font mainFont = new Font("Segoe UI", 9F);
            Font boldFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            Font bigFont = new Font("Segoe UI", 14F, FontStyle.Bold);

            // 1. THANH TAB
            tabControl = new TabControl { Dock = DockStyle.Top, Height = 240, Font = mainFont };
            tabCountdown = new TabPage { BackColor = Color.White };
            tabTime = new TabPage { BackColor = Color.White };
            tabProcess = new TabPage { BackColor = Color.White };
            tabWeekly = new TabPage { BackColor = Color.White };
            tabAbout = new TabPage { BackColor = Color.White };

            // --- XỬ LÝ SỰ KIỆN CHUYỂN TAB THÔNG MINH (NEW v1.3.2) ---
            tabControl.Selecting += (s, e) => {
                if (_isRunning)
                {
                    // Cho phép vào Tab About
                    if (e.TabPage == tabAbout) return;

                    // Cho phép vào Tab đang chạy chức năng hiện tại (để xem, nhưng nội dung sẽ bị disable)
                    if (_mode == 1 && e.TabPage == tabCountdown) return;
                    if (_mode == 2 && e.TabPage == tabTime) return;
                    if (_mode == 3 && e.TabPage == tabProcess) return;
                    if (_mode == 4 && e.TabPage == tabWeekly) return;

                    // Còn lại thì chặn
                    e.Cancel = true;
                    MessageBox.Show(_isEnglish ? "Task is running. Please stop it first." : "Đang chạy tác vụ. Vui lòng hủy trước khi chuyển chức năng!", "GOOT Notification");
                }
            };

            // --- TÍNH TOÁN CĂN GIỮA ---
            int numW = 70;
            int gap = 20;
            int startX = 120;

            // --- TAB 1: ĐẾM NGƯỢC ---
            numHoursCD = new NumericUpDown { Location = new Point(startX, 45), Width = numW, Font = bigFont, TextAlign = HorizontalAlignment.Center };
            numMinutesCD = new NumericUpDown { Location = new Point(startX + numW + gap, 45), Width = numW, Font = bigFont, TextAlign = HorizontalAlignment.Center };
            numSecondsCD = new NumericUpDown { Location = new Point(startX + (numW + gap) * 2, 45), Width = numW, Font = bigFont, TextAlign = HorizontalAlignment.Center };

            btnStartCountdown = CreateButton("START", Color.Teal, new Point(130, 110));
            tabCountdown.Controls.AddRange(new Control[] { numHoursCD, numMinutesCD, numSecondsCD, btnStartCountdown });

            // --- TAB 2: CHỌN GIỜ ---
            numHoursTime = new NumericUpDown { Location = new Point(startX, 45), Width = numW, Font = bigFont, TextAlign = HorizontalAlignment.Center, Value = DateTime.Now.Hour };
            numMinutesTime = new NumericUpDown { Location = new Point(startX + numW + gap, 45), Width = numW, Font = bigFont, TextAlign = HorizontalAlignment.Center, Value = DateTime.Now.Minute };
            numSecondsTime = new NumericUpDown { Location = new Point(startX + (numW + gap) * 2, 45), Width = numW, Font = bigFont, TextAlign = HorizontalAlignment.Center };

            btnStartTime = CreateButton("SET", Color.RoyalBlue, new Point(130, 110));
            tabTime.Controls.AddRange(new Control[] { numHoursTime, numMinutesTime, numSecondsTime, btnStartTime });

            // --- TAB 3: TẮT KHI MỞ APP ---
            lblProcessHint = new Label { Location = new Point(50, 25), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9F, FontStyle.Italic) };
            txtProcessName = new TextBox { Location = new Point(50, 50), Width = 380, Font = new Font("Segoe UI", 12F) };
            btnStartProcess = CreateButton("MONITOR", Color.DarkOrange, new Point(130, 100));
            tabProcess.Controls.AddRange(new Control[] { lblProcessHint, txtProcessName, btnStartProcess });

            // --- TAB 4: LỊCH BIỂU TUẦN ---
            chkListDays = new CheckedListBox { Location = new Point(20, 20), Width = 150, Height = 130, CheckOnClick = true, Font = new Font("Segoe UI", 9F) };
            chkSelectAllDays = new CheckBox { Text = "Select All", Location = new Point(20, 155), AutoSize = true, Font = new Font("Segoe UI", 8F) };

            numHoursWeekly = new NumericUpDown { Location = new Point(200, 50), Width = 70, Font = bigFont, TextAlign = HorizontalAlignment.Center, Value = 22 };
            numMinutesWeekly = new NumericUpDown { Location = new Point(290, 50), Width = 70, Font = bigFont, TextAlign = HorizontalAlignment.Center };
            Label lblColon = new Label { Text = ":", Location = new Point(273, 50), AutoSize = true, Font = bigFont };

            btnStartWeekly = CreateButton("SET SCHEDULE", Color.Purple, new Point(190, 110));
            btnStartWeekly.Size = new Size(240, 40);

            tabWeekly.Controls.AddRange(new Control[] { chkListDays, chkSelectAllDays, numHoursWeekly, lblColon, numMinutesWeekly, btnStartWeekly });

            // --- TAB 5: GIỚI THIỆU ---
            lblAppInfo = new Label { Text = "GOOT", Location = new Point(0, 15), Size = new Size(500, 30), TextAlign = ContentAlignment.MiddleCenter, Font = boldFont };
            lnkRepo = new LinkLabel { Location = new Point(0, 50), Size = new Size(500, 20), TextAlign = ContentAlignment.MiddleCenter, LinkBehavior = LinkBehavior.HoverUnderline };
            lnkAuthor = new LinkLabel { Text = "Author: ToanBB.Pro", Location = new Point(0, 75), Size = new Size(500, 20), TextAlign = ContentAlignment.MiddleCenter, LinkBehavior = LinkBehavior.HoverUnderline };

            btnDonate = CreateButton("☕ Buy me a coffee", Color.Gold, new Point(130, 115));
            btnDonate.ForeColor = Color.Black;

            chkStartWithWindows = new CheckBox { Text = "Start with Windows", Location = new Point(10, 185), AutoSize = true, Font = new Font("Segoe UI", 9F) };

            tabAbout.Controls.AddRange(new Control[] { lblAppInfo, lnkRepo, lnkAuthor, btnDonate, chkStartWithWindows });

            tabControl.TabPages.AddRange(new TabPage[] { tabCountdown, tabTime, tabWeekly, tabProcess, tabAbout });

            // CÁC NÚT KHÁC
            btnLangFlag = new Button { Size = new Size(36, 22), Location = new Point(445, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = Color.WhiteSmoke, BackgroundImageLayout = ImageLayout.Zoom };
            btnLangFlag.FlatAppearance.BorderSize = 0;

            btnToggleSecurity = new Button { Text = "Show Security Options", Location = new Point(10, 245), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Color.WhiteSmoke, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
            btnToggleSecurity.FlatAppearance.BorderColor = Color.Silver;

            grpSecurity = new GroupBox { Text = "Security", Location = new Point(10, 280), Size = new Size(465, 145), Visible = false };
            chkPassword = new CheckBox { Text = "Password", Location = new Point(20, 30), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(140, 28), Width = 300, PasswordChar = '•' };
            btnCancel = new Button { Text = "CANCEL", Location = new Point(130, 75), Size = new Size(200, 45), FlatStyle = FlatStyle.Flat, BackColor = Color.IndianRed, ForeColor = Color.White, Font = boldFont };
            btnCancel.FlatAppearance.BorderSize = 0;
            grpSecurity.Controls.AddRange(new Control[] { chkPassword, txtPassword, btnCancel });

            statusStrip = new StatusStrip();
            statusStrip.SizingGrip = false;
            lblStatusLeft = new ToolStripStatusLabel { Text = "Ready", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            lblStatusRight = new ToolStripStatusLabel { Text = "00:00:00", BorderSides = ToolStripStatusLabelBorderSides.Left, Padding = new Padding(10, 0, 0, 0) };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatusLeft, lblStatusRight });

            this.Controls.Add(btnLangFlag);
            this.Controls.Add(btnToggleSecurity);
            this.Controls.Add(grpSecurity);
            this.Controls.Add(tabControl);
            this.Controls.Add(statusStrip);
            btnLangFlag.BringToFront();
        }

        private Button CreateButton(string text, Color color, Point loc) =>
            new Button { Text = text, Location = loc, Size = new Size(220, 40), FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 } };

        private void InitializeLogic()
        {
            mainTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            mainTimer.Tick += MainTimer_Tick;

            clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) => { lblStatusRight.Text = DateTime.Now.ToString("HH:mm:ss"); };
            clockTimer.Start();

            CheckStartupState();

            // Cấu hình tuần hoàn
            MakeCyclic(numHoursCD, 99); MakeCyclic(numMinutesCD, 59); MakeCyclic(numSecondsCD, 59);
            MakeCyclic(numHoursTime, 23); MakeCyclic(numMinutesTime, 59); MakeCyclic(numSecondsTime, 59);
            MakeCyclic(numHoursWeekly, 23); MakeCyclic(numMinutesWeekly, 59);

            chkSelectAllDays.CheckedChanged += (s, e) =>
            {
                for (int i = 0; i < chkListDays.Items.Count; i++)
                    chkListDays.SetItemChecked(i, chkSelectAllDays.Checked);
            };

            btnLangFlag.Click += (s, e) => { _isEnglish = !_isEnglish; UpdateLanguage(); };
            chkStartWithWindows.CheckedChanged += (s, e) => SetStartup(chkStartWithWindows.Checked);

            btnToggleSecurity.Click += (s, e) =>
            {
                bool isShowing = !grpSecurity.Visible;
                grpSecurity.Visible = isShowing;
                this.Size = isShowing ? _expandedSize : _compactSize;
                UpdateSecurityButtonText();
            };

            btnStartCountdown.Click += (s, e) =>
            {
                _targetTime = DateTime.Now.AddHours((int)numHoursCD.Value).AddMinutes((int)numMinutesCD.Value).AddSeconds((int)numSecondsCD.Value);
                StartSystem(1, (_isEnglish ? "Shutdown at: " : "Tắt lúc: ") + _targetTime.ToString("HH:mm:ss"));
            };

            btnStartTime.Click += (s, e) =>
            {
                DateTime now = DateTime.Now;
                DateTime selected = new DateTime(now.Year, now.Month, now.Day, (int)numHoursTime.Value, (int)numMinutesTime.Value, (int)numSecondsTime.Value);
                if (selected < now) selected = selected.AddDays(1);
                _targetTime = selected;
                StartSystem(2, (_isEnglish ? "Shutdown at: " : "Tắt lúc: ") + _targetTime.ToString("HH:mm:ss"));
            };

            btnStartProcess.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtProcessName.Text)) return;
                _targetProcessName = txtProcessName.Text.Trim().ToLower().Replace(".exe", "");
                StartSystem(3, (_isEnglish ? "Watching: " : "Đang canh: ") + _targetProcessName);
            };

            btnStartWeekly.Click += (s, e) =>
            {
                if (chkListDays.CheckedItems.Count == 0)
                {
                    MessageBox.Show(_isEnglish ? "Please select at least one day." : "Vui lòng chọn ít nhất một ngày.");
                    return;
                }

                DateTime? nextRun = GetNextWeeklyTime((int)numHoursWeekly.Value, (int)numMinutesWeekly.Value);
                if (nextRun.HasValue)
                {
                    _targetTime = nextRun.Value;
                    // Khi người dùng bấm nút Set, lưu trạng thái Active
                    SaveSettings(true);
                    StartSystem(4, (_isEnglish ? "Next run: " : "Lần chạy tới: ") + _targetTime.ToString("dd/MM HH:mm"));
                }
            };

            btnCancel.Click += BtnCancel_Click;
            btnDonate.Click += (s, e) => ShowDonationDialog();
            lnkAuthor.Click += (s, e) => { try { Process.Start(new ProcessStartInfo { FileName = "https://www.facebook.com/toanbb.pro/", UseShellExecute = true }); } catch { } };
            lnkRepo.Click += (s, e) => { try { Process.Start(new ProcessStartInfo { FileName = "https://github.com/toanbbpro/GOOT", UseShellExecute = true }); } catch { } };

            notifyIcon = new NotifyIcon { Icon = this.Icon, Text = "GOOT", Visible = false };
            notifyIcon.MouseDoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; notifyIcon.Visible = false; };
        }

        // --- HÀM LƯU / TẢI SETTINGS ---
        private void SaveSettings(bool isActive)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_PATH);
                key.SetValue("WeeklyHour", (int)numHoursWeekly.Value);
                key.SetValue("WeeklyMinute", (int)numMinutesWeekly.Value);

                string checkedIndices = string.Join(",", chkListDays.CheckedIndices.Cast<int>());
                key.SetValue("WeeklyDays", checkedIndices);

                if (chkPassword.Checked && !string.IsNullOrEmpty(txtPassword.Text))
                {
                    key.SetValue("Password", txtPassword.Text);
                    key.SetValue("UsePassword", 1);
                }
                else
                {
                    key.DeleteValue("Password", false);
                    key.SetValue("UsePassword", 0);
                }

                key.SetValue("IsWeeklyActive", isActive ? 1 : 0);
                key.Close();
            }
            catch { }
        }

        private void LoadSettings()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_PATH);
                if (key != null)
                {
                    if (key.GetValue("WeeklyHour") != null) numHoursWeekly.Value = (int)key.GetValue("WeeklyHour");
                    if (key.GetValue("WeeklyMinute") != null) numMinutesWeekly.Value = (int)key.GetValue("WeeklyMinute");

                    if (key.GetValue("UsePassword") != null) chkPassword.Checked = (int)key.GetValue("UsePassword") == 1;
                    if (key.GetValue("Password") != null) txtPassword.Text = (string)key.GetValue("Password");

                    string daysStr = key.GetValue("WeeklyDays") as string;
                    if (!string.IsNullOrEmpty(daysStr))
                    {
                        if (chkListDays.Items.Count == 0) UpdateLanguage();

                        string[] indices = daysStr.Split(',');
                        foreach (string idx in indices)
                        {
                            if (int.TryParse(idx, out int i) && i >= 0 && i < chkListDays.Items.Count)
                            {
                                chkListDays.SetItemChecked(i, true);
                            }
                        }
                    }

                    // AUTO RESUME
                    int isActive = (int)key.GetValue("IsWeeklyActive", 0);
                    if (isActive == 1)
                    {
                        tabControl.SelectedTab = tabWeekly;
                        DateTime? nextRun = GetNextWeeklyTime((int)numHoursWeekly.Value, (int)numMinutesWeekly.Value);
                        if (nextRun.HasValue)
                        {
                            _targetTime = nextRun.Value;
                            StartSystem(4, (_isEnglish ? "Next run: " : "Lần chạy tới: ") + _targetTime.ToString("dd/MM HH:mm"));
                        }
                    }
                    key.Close();
                }
            }
            catch { }
        }

        private void MakeCyclic(NumericUpDown nud, int max)
        {
            nud.Minimum = -1;
            nud.Maximum = max + 1;
            nud.ValueChanged += (s, e) =>
            {
                if (nud.Value >= max + 1) nud.Value = 0;
                if (nud.Value <= -1) nud.Value = max;
            };
        }

        private void CheckStartupState()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (rk.GetValue("GOOT") != null) chkStartWithWindows.Checked = true;
            }
            catch { }
        }

        private void SetStartup(bool enable)
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (enable) rk.SetValue("GOOT", Application.ExecutablePath);
                else rk.DeleteValue("GOOT", false);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private DateTime? GetNextWeeklyTime(int hour, int minute)
        {
            DateTime now = DateTime.Now;
            DateTime checkDate = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);

            for (int i = 0; i < 8; i++)
            {
                if (i == 0 && checkDate < now)
                {
                    checkDate = checkDate.AddDays(1);
                    continue;
                }

                int dayIndex = (int)checkDate.DayOfWeek;
                if (chkListDays.GetItemChecked(dayIndex))
                    return checkDate;

                checkDate = checkDate.AddDays(1);
            }
            return null;
        }

        private void UpdateSecurityButtonText()
        {
            if (_isEnglish) btnToggleSecurity.Text = grpSecurity.Visible ? "Hide Security Options" : "Show Security Options";
            else btnToggleSecurity.Text = grpSecurity.Visible ? "Ẩn tùy chọn bảo mật" : "Hiện tùy chọn bảo mật";
        }

        private void UpdateLanguage()
        {
            string ver = "1.3.2";

            bool[] checkedDays = new bool[7];
            if (chkListDays.Items.Count > 0)
                for (int i = 0; i < 7; i++) checkedDays[i] = chkListDays.GetItemChecked(i);

            chkListDays.Items.Clear();

            if (_isEnglish)
            {
                try { btnLangFlag.BackgroundImage = Properties.Resources.flag_us; } catch { btnLangFlag.Text = "US"; }
                UpdateSecurityButtonText();

                tabCountdown.Text = "Countdown"; tabTime.Text = "Schedule"; tabProcess.Text = "Auto Close";
                tabWeekly.Text = "Weekly"; tabAbout.Text = "About";

                btnStartCountdown.Text = "START"; btnStartTime.Text = "SET"; btnStartProcess.Text = "MONITOR";
                btnStartWeekly.Text = "SET SCHEDULE";

                lblProcessHint.Text = "Enter process name (e.g., chrome, notepad)";
                lblAppInfo.Text = $"GOOT - Get Off On Time\nVersion {ver}";
                lnkRepo.Text = "Homepage: GitHub Repo";
                lnkAuthor.Text = "Author: ToanBB.Pro";
                grpSecurity.Text = "Security Control"; chkPassword.Text = "Require Password";
                btnCancel.Text = _isRunning ? "STOP NOW" : "CANCEL";
                chkStartWithWindows.Text = "Start with Windows";
                chkSelectAllDays.Text = "Select All";

                string[] daysEn = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
                chkListDays.Items.AddRange(daysEn);
                if (!_isRunning) lblStatusLeft.Text = "Ready";
            }
            else
            {
                try { btnLangFlag.BackgroundImage = Properties.Resources.flag_vn; } catch { btnLangFlag.Text = "VN"; }
                UpdateSecurityButtonText();

                tabCountdown.Text = "Đếm ngược"; tabTime.Text = "Chọn giờ"; tabProcess.Text = "Tắt khi mở app";
                tabWeekly.Text = "Lịch tuần"; tabAbout.Text = "Giới thiệu";

                btnStartCountdown.Text = "BẮT ĐẦU"; btnStartTime.Text = "HẸN GIỜ"; btnStartProcess.Text = "THEO DÕI";
                btnStartWeekly.Text = "ĐẶT LỊCH TẮT";

                lblProcessHint.Text = "Tên app cần theo dõi (ví dụ: chrome, notepad)";
                lblAppInfo.Text = $"GOOT - Get Off On Time\nPhiên bản {ver}";
                lnkRepo.Text = "Trang chủ: GitHub Repo";
                lnkAuthor.Text = "Tác giả: ToanBB.Pro";
                grpSecurity.Text = "Bảo mật"; chkPassword.Text = "Dùng mật khẩu";
                btnCancel.Text = _isRunning ? "DỪNG NGAY" : "HỦY HẸN GIỜ";
                chkStartWithWindows.Text = "Chạy cùng Windows";
                chkSelectAllDays.Text = "Chọn tất cả";

                string[] daysVn = { "Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy" };
                chkListDays.Items.AddRange(daysVn);
                if (!_isRunning) lblStatusLeft.Text = "Sẵn sàng";
            }

            for (int i = 0; i < 7; i++) chkListDays.SetItemChecked(i, checkedDays[i]);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized) { this.Hide(); notifyIcon.Visible = true; }
        }

        // --- HÀM KHÓA/MỞ CONTROL TRONG TAB ---
        private void ToggleCurrentTabControls(TabPage page, bool enable)
        {
            foreach (Control c in page.Controls)
            {
                c.Enabled = enable;
            }
        }

        private void StartSystem(int mode, string msg)
        {
            _mode = mode; _isRunning = true;
            lblStatusLeft.Text = msg; lblStatusLeft.ForeColor = Color.Red;

            // Khóa Input bảo mật
            chkPassword.Enabled = false;
            txtPassword.Enabled = false;

            // Khóa Input trong Tab đang chạy
            if (mode == 1) ToggleCurrentTabControls(tabCountdown, false);
            if (mode == 2) ToggleCurrentTabControls(tabTime, false);
            if (mode == 3) ToggleCurrentTabControls(tabProcess, false);
            if (mode == 4)
            {
                ToggleCurrentTabControls(tabWeekly, false);

                // ĐỒNG BỘ STARTUP: Nếu chạy Weekly -> Bắt buộc bật Startup và khóa lại
                chkStartWithWindows.Checked = true;
                SetStartup(true);
                chkStartWithWindows.Enabled = false;
            }

            mainTimer.Start();
            UpdateLanguage();

            if (!grpSecurity.Visible)
            {
                grpSecurity.Visible = true;
                this.Size = _expandedSize;
                UpdateSecurityButtonText();
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (!_isRunning) return;

            if (chkPassword.Checked && Prompt.ShowDialog("Pass:", "Verify") != txtPassword.Text) return;

            mainTimer.Stop(); _isRunning = false;

            // Mở lại Input bảo mật
            chkPassword.Enabled = true;
            txtPassword.Enabled = true;

            // Mở lại các Control trong Tab
            if (_mode == 1) ToggleCurrentTabControls(tabCountdown, true);
            if (_mode == 2) ToggleCurrentTabControls(tabTime, true);
            if (_mode == 3) ToggleCurrentTabControls(tabProcess, true);
            if (_mode == 4)
            {
                ToggleCurrentTabControls(tabWeekly, true);
                // Mở lại ô Startup để user tùy chỉnh
                chkStartWithWindows.Enabled = true;
                SaveSettings(false); // Xóa active state
            }

            lblStatusLeft.ForeColor = Color.Black;
            UpdateLanguage();
        }

        private void MainTimer_Tick(object sender, EventArgs e)
        {
            if (_mode == 3)
            {
                if (Process.GetProcessesByName(_targetProcessName).Length > 0) ShutdownNow();
            }
            else
            {
                TimeSpan remain = _targetTime - DateTime.Now;
                if (remain.TotalSeconds <= 0)
                {
                    ShutdownNow();
                }
                else
                {
                    string prefix = _isEnglish ? "Remaining: " : "Còn lại: ";
                    if (_mode == 4 && remain.TotalHours > 24)
                        lblStatusLeft.Text = prefix + remain.ToString(@"dd\.hh\:mm\:ss");
                    else
                        lblStatusLeft.Text = prefix + remain.ToString(@"hh\:mm\:ss");
                }
            }
        }

        private void ShutdownNow() { Process.Start(new ProcessStartInfo("shutdown", "/s /f /t 0") { CreateNoWindow = true, UseShellExecute = false }); }

        private void ShowDonationDialog()
        {
            Form f = new Form { Text = "Donate", Size = new Size(320, 440), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedToolWindow, BackColor = Color.White };
            PictureBox pb = new PictureBox { Size = new Size(240, 240), Location = new Point(35, 15), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            try { pb.Image = Properties.Resources.qr; } catch { }

            Label lb = new Label
            {
                Text = "TP BANK: 03024836402\nContent: Buy coffee",
                Location = new Point(10, 270),
                Size = new Size(300, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            Button btn = new Button { Text = "Close", Location = new Point(110, 345), Size = new Size(100, 35) };
            btn.Click += (s, e) => f.Close();

            f.Controls.AddRange(new Control[] { pb, lb, btn });
            f.ShowDialog();
        }
    }

    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form() { Width = 300, Height = 150, Text = caption, StartPosition = FormStartPosition.CenterScreen, FormBorderStyle = FormBorderStyle.FixedDialog };
            TextBox t = new TextBox() { Left = 20, Top = 45, Width = 240, PasswordChar = '•' };
            Button b = new Button() { Text = "OK", Left = 180, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            prompt.Controls.AddRange(new Control[] { t, b });
            return prompt.ShowDialog() == DialogResult.OK ? t.Text : "";
        }
    }
}