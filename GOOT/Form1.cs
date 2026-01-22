using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace GOOT
{
    public partial class Form1 : Form
    {
        // --- BIẾN GIAO DIỆN ---
        private TabControl tabControl;
        private TabPage tabCountdown, tabTime, tabProcess, tabAbout;
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

        private NumericUpDown numHoursCD, numMinutesCD, numSecondsCD;
        private NumericUpDown numHoursTime, numMinutesTime, numSecondsTime;

        // Tab Chặn App
        private Label lblProcessHint;
        private TextBox txtProcessName;
        private Button btnStartCountdown, btnStartTime, btnStartProcess, btnDonate;

        private Label lblAppInfo;
        private LinkLabel lnkAuthor;

        private NotifyIcon notifyIcon;
        private System.Windows.Forms.Timer mainTimer;
        private System.Windows.Forms.Timer clockTimer;

        // --- BIẾN LOGIC ---
        private DateTime _targetTime;
        private string _targetProcessName = "";
        private int _mode = 0;
        private bool _isRunning = false;
        private bool _isEnglish = false;

        // Kích thước Form
        private readonly Size _compactSize = new Size(500, 330);
        private readonly Size _expandedSize = new Size(500, 480);

        public Form1()
        {
            this.Text = $"GOOT - Get Off On Time (v{AppVersionShort})";
            this.Size = _compactSize;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            try { this.Icon = Properties.Resources.icon; } catch { this.Icon = SystemIcons.Application; }

            InitializeUI();
            InitializeLogic();
            UpdateLanguage();
        }

        private void InitializeUI()
        {
            Font mainFont = new Font("Segoe UI", 9F);
            Font boldFont = new Font("Segoe UI", 9F, FontStyle.Bold);

            // 1. THANH TAB
            tabControl = new TabControl { Dock = DockStyle.Top, Height = 220, Font = mainFont };
            tabCountdown = new TabPage { BackColor = Color.White };
            tabTime = new TabPage { BackColor = Color.White };
            tabProcess = new TabPage { BackColor = Color.White };
            tabAbout = new TabPage { BackColor = Color.White };

            // --- TAB 1: ĐẾM NGƯỢC ---
            int startX = 50, gap = 110;
            numHoursCD = new NumericUpDown { Location = new Point(startX, 45), Width = 70, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center, Maximum = 99 };
            numMinutesCD = new NumericUpDown { Location = new Point(startX + gap, 45), Width = 70, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center, Maximum = 59 };
            numSecondsCD = new NumericUpDown { Location = new Point(startX + (gap * 2), 45), Width = 70, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center, Maximum = 59 };
            btnStartCountdown = CreateButton("START", Color.Teal, new Point(130, 110));
            tabCountdown.Controls.AddRange(new Control[] { numHoursCD, numMinutesCD, numSecondsCD, btnStartCountdown });

            // --- TAB 2: CHỌN GIỜ ---
            numHoursTime = new NumericUpDown { Location = new Point(startX, 45), Width = 70, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center, Maximum = 23, Value = DateTime.Now.Hour };
            numMinutesTime = new NumericUpDown { Location = new Point(startX + gap, 45), Width = 70, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center, Maximum = 59, Value = DateTime.Now.Minute };
            numSecondsTime = new NumericUpDown { Location = new Point(startX + (gap * 2), 45), Width = 70, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center, Maximum = 59 };
            btnStartTime = CreateButton("SET", Color.RoyalBlue, new Point(130, 110));
            tabTime.Controls.AddRange(new Control[] { numHoursTime, numMinutesTime, numSecondsTime, btnStartTime });

            // --- TAB 3: TẮT KHI MỞ APP ---
            lblProcessHint = new Label
            {
                Location = new Point(50, 25),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
            txtProcessName = new TextBox { Location = new Point(50, 50), Width = 380, Font = new Font("Segoe UI", 12F) };
            btnStartProcess = CreateButton("MONITOR", Color.DarkOrange, new Point(130, 100));
            tabProcess.Controls.AddRange(new Control[] { lblProcessHint, txtProcessName, btnStartProcess });

            // --- TAB 4: GIỚI THIỆU ---
            lblAppInfo = new Label
            {
                Text = "GOOT - Get Off On Time",
                Location = new Point(0, 30),
                Size = new Size(500, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = boldFont
            };
            lnkAuthor = new LinkLabel
            {
                Text = "Tác giả: ToanBB.Pro",
                Location = new Point(0, 80),
                Size = new Size(500, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            btnDonate = CreateButton("☕ Buy me a coffee", Color.Gold, new Point(130, 120));
            btnDonate.ForeColor = Color.Black;
            tabAbout.Controls.AddRange(new Control[] { lblAppInfo, lnkAuthor, btnDonate });

            tabControl.TabPages.AddRange(new TabPage[] { tabCountdown, tabTime, tabProcess, tabAbout });

            // 2. NÚT LÁ CỜ
            btnLangFlag = new Button
            {
                Size = new Size(36, 22),
                Location = new Point(445, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.WhiteSmoke,
                BackgroundImageLayout = ImageLayout.Zoom
            };
            btnLangFlag.FlatAppearance.BorderSize = 0;

            // 3. NÚT ẨN/HIỆN BẢO MẬT
            btnToggleSecurity = new Button
            {
                Text = "Show Security Options",
                Location = new Point(10, 225),
                AutoSize = true, // Tự động co giãn theo độ dài chữ
                AutoSizeMode = AutoSizeMode.GrowAndShrink, // Chỉ lớn bằng nội dung
                BackColor = Color.WhiteSmoke,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnToggleSecurity.FlatAppearance.BorderColor = Color.Silver;

            // 4. KHU VỰC BẢO MẬT (Mặc định ẩn)
            grpSecurity = new GroupBox { Text = "Security", Location = new Point(10, 260), Size = new Size(465, 145), Visible = false };
            chkPassword = new CheckBox { Text = "Password", Location = new Point(20, 30), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(140, 28), Width = 300, PasswordChar = '•' };
            btnCancel = new Button { Text = "CANCEL", Location = new Point(130, 75), Size = new Size(200, 45), FlatStyle = FlatStyle.Flat, BackColor = Color.IndianRed, ForeColor = Color.White, Font = boldFont };
            btnCancel.FlatAppearance.BorderSize = 0;
            grpSecurity.Controls.AddRange(new Control[] { chkPassword, txtPassword, btnCancel });

            // 5. STATUS STRIP
            statusStrip = new StatusStrip();
            statusStrip.SizingGrip = false; // Tắt biểu tượng kéo giãn góc phải dưới
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

            btnLangFlag.Click += (s, e) => { _isEnglish = !_isEnglish; UpdateLanguage(); };

            // Logic Ẩn/Hiện Bảo mật khi nhấn Nút
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
            btnCancel.Click += BtnCancel_Click;
            btnDonate.Click += (s, e) => ShowDonationDialog();
            lnkAuthor.Click += (s, e) => { try { Process.Start(new ProcessStartInfo { FileName = "https://www.facebook.com/toanbb.pro/", UseShellExecute = true }); } catch { } };

            notifyIcon = new NotifyIcon { Icon = this.Icon, Text = "GOOT", Visible = false };
            notifyIcon.MouseDoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; notifyIcon.Visible = false; };
        }

        private void UpdateSecurityButtonText()
        {
            if (_isEnglish)
                btnToggleSecurity.Text = grpSecurity.Visible ? "Hide Security Options" : "Show Security Options";
            else
                btnToggleSecurity.Text = grpSecurity.Visible ? "Ẩn tùy chọn bảo mật" : "Hiện tùy chọn bảo mật";
        }

        private void UpdateLanguage()
        {
            string ver = Application.ProductVersion.Substring(0, Math.Min(15, Application.ProductVersion.Length));
            if (_isEnglish)
            {
                try { btnLangFlag.BackgroundImage = Properties.Resources.flag_us; } catch { btnLangFlag.Text = "US"; }

                UpdateSecurityButtonText();

                tabCountdown.Text = "Countdown"; tabTime.Text = "Schedule"; tabProcess.Text = "Auto Close"; tabAbout.Text = "About";
                btnStartCountdown.Text = "START"; btnStartTime.Text = "SET"; btnStartProcess.Text = "MONITOR";

                lblProcessHint.Text = "Enter process name (e.g., chrome, notepad)";

                lblAppInfo.Text = $"GOOT - Get Off On Time\nVersion {ver}";
                lnkAuthor.Text = "Author: ToanBB.Pro";
                grpSecurity.Text = "Security Control"; chkPassword.Text = "Require Password";
                btnCancel.Text = _isRunning ? "STOP NOW" : "CANCEL";
                if (!_isRunning) lblStatusLeft.Text = "Ready";
            }
            else
            {
                try { btnLangFlag.BackgroundImage = Properties.Resources.flag_vn; } catch { btnLangFlag.Text = "VN"; }

                UpdateSecurityButtonText();

                tabCountdown.Text = "Đếm ngược"; tabTime.Text = "Chọn giờ"; tabProcess.Text = "Tắt khi mở app"; tabAbout.Text = "Giới thiệu";
                btnStartCountdown.Text = "BẮT ĐẦU"; btnStartTime.Text = "HẸN GIỜ"; btnStartProcess.Text = "THEO DÕI";

                lblProcessHint.Text = "Tên app cần theo dõi (ví dụ: chrome, notepad)";

                lblAppInfo.Text = $"GOOT - Get Off On Time\nPhiên bản {ver}";
                lnkAuthor.Text = "Tác giả: ToanBB.Pro";
                grpSecurity.Text = "Bảo mật"; chkPassword.Text = "Dùng mật khẩu";
                btnCancel.Text = _isRunning ? "DỪNG NGAY" : "HỦY HẸN GIỜ";
                if (!_isRunning) lblStatusLeft.Text = "Sẵn sàng";
            }
        }

        private string AppVersionShort
        {
            get
            {
                try
                {
                    string rawVersion = Application.ProductVersion.Split('+')[0];
                    Version v = new Version(rawVersion);
                    return $"{v.Major}.{v.Minor}";
                }
                catch { return "1.1"; }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized) { this.Hide(); notifyIcon.Visible = true; }
        }

        private void StartSystem(int mode, string msg)
        {
            _mode = mode; _isRunning = true;
            lblStatusLeft.Text = msg; lblStatusLeft.ForeColor = Color.Red;
            tabControl.Enabled = false; mainTimer.Start();
            UpdateLanguage();

            // Tự động mở rộng form và hiện phần bảo mật để người dùng thấy nút Hủy
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
            tabControl.Enabled = true; lblStatusLeft.ForeColor = Color.Black;
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
                if (remain.TotalSeconds <= 0) ShutdownNow();
                else lblStatusLeft.Text = (_isEnglish ? "Remaining: " : "Còn lại: ") + remain.ToString(@"hh\:mm\:ss");
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