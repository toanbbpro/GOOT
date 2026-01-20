using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace GOOT
{
    public partial class Form1 : Form
    {
        // --- CÁC CONTROL GIAO DIỆN ---
        private TabControl tabControl;
        private TabPage tabCountdown, tabTime, tabProcess, tabAbout;

        // Tab 1: Đếm ngược (Thêm giây)
        private NumericUpDown numHoursCD, numMinutesCD, numSecondsCD;
        private Button btnStartCountdown;

        // Tab 2: Chọn giờ (Thêm giây)
        private NumericUpDown numHoursTime, numMinutesTime, numSecondsTime;
        private Button btnStartTime;
        private Label lblCurrentTime;

        // Tab 3: Chặn App
        private TextBox txtProcessName;
        private Button btnStartProcess;

        // Tab 4: Giới thiệu
        private LinkLabel lnkAuthor;

        // Common Controls
        private CheckBox chkPassword;
        private TextBox txtPassword;
        private Label lblStatus;
        private Button btnCancel;
        private NotifyIcon notifyIcon;

        private System.Windows.Forms.Timer mainTimer;
        private System.Windows.Forms.Timer clockTimer;

        // --- BIẾN LOGIC ---
        private DateTime _targetTime;
        private string _targetProcessName = "";
        private int _mode = 0;
        private bool _isRunning = false;

        public Form1()
        {
            // Cấu hình Form chính
            this.Text = "GOOT - Get Off On Time";
            // Yêu cầu 4: Cửa sổ to hơn về chiều ngang (400 -> 520)
            this.Size = new Size(520, 480);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // --- YÊU CẦU 1: NHÚNG ICON ---
            // Code này lấy icon trực tiếp từ Resource bạn đã Add ở Phần 1
            // Nếu bạn chưa Add Resource tên là 'icon', dòng dưới sẽ báo lỗi đỏ.
            try
            {
                this.Icon = Properties.Resources.icon;
            }
            catch
            {
                // Nếu quên add resource thì dùng tạm icon mặc định để không lỗi code
                this.Icon = SystemIcons.Application;
            }

            InitializeUI();
            InitializeLogic();
        }

        private void InitializeUI()
        {
            Font mainFont = new Font("Segoe UI", 10F, FontStyle.Regular);
            Font boldFont = new Font("Segoe UI", 10F, FontStyle.Bold);
            Font bigFont = new Font("Segoe UI", 14F, FontStyle.Bold);

            // TẠO TAB CONTROL
            tabControl = new TabControl { Dock = DockStyle.Top, Height = 240, Font = mainFont };

            // =========================================================
            // TAB 1: Đếm ngược (Cập nhật thêm giây)
            // =========================================================
            tabCountdown = new TabPage("⏳ Đếm ngược");
            tabCountdown.BackColor = Color.White;

            // Layout 3 cột: Giờ - Phút - Giây (Căn chỉnh lại tọa độ X cho cân với chiều rộng 520)
            int startX = 60;
            int gap = 110; // Khoảng cách giữa các ô

            Label lblH1 = new Label { Text = "Giờ", Location = new Point(startX, 30), AutoSize = true };
            numHoursCD = new NumericUpDown { Location = new Point(startX, 55), Width = 70, Maximum = 99, Font = bigFont, TextAlign = HorizontalAlignment.Center };

            Label lblM1 = new Label { Text = "Phút", Location = new Point(startX + gap, 30), AutoSize = true };
            numMinutesCD = new NumericUpDown { Location = new Point(startX + gap, 55), Width = 70, Maximum = 59, Font = bigFont, TextAlign = HorizontalAlignment.Center };

            // Yêu cầu 2: Thêm ô Giây
            Label lblS1 = new Label { Text = "Giây", Location = new Point(startX + gap * 2, 30), AutoSize = true };
            numSecondsCD = new NumericUpDown { Location = new Point(startX + gap * 2, 55), Width = 70, Maximum = 59, Font = bigFont, TextAlign = HorizontalAlignment.Center };

            btnStartCountdown = CreateModernButton("BẮT ĐẦU ĐẾM NGƯỢC", Color.Teal, new Point(140, 120)); // Căn giữa nút

            tabCountdown.Controls.AddRange(new Control[] { lblH1, numHoursCD, lblM1, numMinutesCD, lblS1, numSecondsCD, btnStartCountdown });

            // =========================================================
            // TAB 2: Chọn giờ (Cập nhật thêm giây)
            // =========================================================
            tabTime = new TabPage("⏰ Chọn giờ");
            tabTime.BackColor = Color.White;

            lblCurrentTime = new Label { Text = "Hiện tại: " + DateTime.Now.ToString("HH:mm:ss"), Location = new Point(0, 15), Size = new Size(500, 20), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Gray };

            Label lblH2 = new Label { Text = "Giờ", Location = new Point(startX, 45), AutoSize = true };
            numHoursTime = new NumericUpDown { Location = new Point(startX, 70), Width = 70, Maximum = 23, Font = bigFont, TextAlign = HorizontalAlignment.Center, Value = DateTime.Now.Hour };

            Label lblM2 = new Label { Text = "Phút", Location = new Point(startX + gap, 45), AutoSize = true };
            numMinutesTime = new NumericUpDown { Location = new Point(startX + gap, 70), Width = 70, Maximum = 59, Font = bigFont, TextAlign = HorizontalAlignment.Center, Value = DateTime.Now.Minute };

            // Thêm ô Giây cho tab chọn giờ
            Label lblS2 = new Label { Text = "Giây", Location = new Point(startX + gap * 2, 45), AutoSize = true };
            numSecondsTime = new NumericUpDown { Location = new Point(startX + gap * 2, 70), Width = 70, Maximum = 59, Font = bigFont, TextAlign = HorizontalAlignment.Center, Value = 0 };

            btnStartTime = CreateModernButton("HẸN GIỜ TẮT", Color.RoyalBlue, new Point(140, 130));

            tabTime.Controls.AddRange(new Control[] { lblCurrentTime, lblH2, numHoursTime, lblM2, numMinutesTime, lblS2, numSecondsTime, btnStartTime });

            // =========================================================
            // TAB 3: Chặn App (Yêu cầu 3: Đổi tên tab)
            // =========================================================
            tabProcess = new TabPage("🚫 Tắt máy khi mở App");
            tabProcess.BackColor = Color.White;

            Label lblP = new Label { Text = "Tên ứng dụng (VD: chrome, lol):", Location = new Point(60, 30), AutoSize = true };
            txtProcessName = new TextBox { Location = new Point(60, 55), Width = 380, Font = new Font("Segoe UI", 12F) }; // Kéo dài textbox
            Label lblNote = new Label { Text = "Máy sẽ tự tắt ngay lập tức nếu app này được bật lên", Location = new Point(60, 85), ForeColor = Color.Gray, Font = new Font("Segoe UI", 9F) };

            btnStartProcess = CreateModernButton("BẮT ĐẦU THEO DÕI", Color.DarkOrange, new Point(140, 120));

            tabProcess.Controls.AddRange(new Control[] { lblP, txtProcessName, lblNote, btnStartProcess });

            // =========================================================
            // TAB 4: Giới thiệu
            // =========================================================
            tabAbout = new TabPage("ℹ️ Giới thiệu");
            tabAbout.BackColor = Color.White;

            Label lblAppInfo = new Label { Text = "GOOT - Get Off On Time\nPhiên bản 1.1", Location = new Point(0, 40), Size = new Size(500, 50), TextAlign = ContentAlignment.MiddleCenter, Font = boldFont };

            lnkAuthor = new LinkLabel();
            lnkAuthor.Text = "Tác giả: ToanBB.Pro\n(Click để ghé thăm Facebook)";
            lnkAuthor.Location = new Point(0, 90);
            lnkAuthor.Size = new Size(500, 50);
            lnkAuthor.TextAlign = ContentAlignment.MiddleCenter;
            lnkAuthor.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
            lnkAuthor.LinkBehavior = LinkBehavior.HoverUnderline;

            tabAbout.Controls.AddRange(new Control[] { lblAppInfo, lnkAuthor });

            // Thêm tabs
            tabControl.TabPages.Add(tabCountdown);
            tabControl.TabPages.Add(tabTime);
            tabControl.TabPages.Add(tabProcess);
            tabControl.TabPages.Add(tabAbout);

            // --- PHẦN DƯỚI: BẢO MẬT & TRẠNG THÁI ---
            // Tăng chiều ngang GroupBox theo Form
            GroupBox grpSecurity = new GroupBox { Text = "🛡️ Bảo mật & Trạng thái", Location = new Point(10, 260), Size = new Size(485, 160), Font = mainFont };

            chkPassword = new CheckBox { Text = "Khóa mật khẩu", Location = new Point(50, 30), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(180, 28), Width = 250, PasswordChar = '•', PlaceholderText = "Nhập mật khẩu..." };

            lblStatus = new Label { Text = "Sẵn sàng", Location = new Point(10, 65), Size = new Size(465, 30), TextAlign = ContentAlignment.MiddleCenter, Font = boldFont, ForeColor = Color.DarkSlateGray };

            btnCancel = new Button { Text = "HỦY HẸN GIỜ", Location = new Point(165, 110), Size = new Size(160, 40), FlatStyle = FlatStyle.Flat, BackColor = Color.IndianRed, ForeColor = Color.White, Font = boldFont, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0;

            grpSecurity.Controls.Add(chkPassword);
            grpSecurity.Controls.Add(txtPassword);
            grpSecurity.Controls.Add(lblStatus);
            grpSecurity.Controls.Add(btnCancel);

            this.Controls.Add(tabControl);
            this.Controls.Add(grpSecurity);
        }

        private Button CreateModernButton(string text, Color color, Point loc)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = loc;
            btn.Size = new Size(220, 40);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // --- PHẦN 2: LOGIC XỬ LÝ (BACKEND) ---
        private void InitializeLogic()
        {
            mainTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            mainTimer.Tick += MainTimer_Tick;

            clockTimer = new System.Windows.Forms.Timer { Interval = 1000 }; // Cập nhật mỗi giây để hiện giây hiện tại
            clockTimer.Tick += (s, e) => { if (!_isRunning && lblCurrentTime != null) lblCurrentTime.Text = "Hiện tại: " + DateTime.Now.ToString("HH:mm:ss"); };
            clockTimer.Start();

            // === LOGIC TAB 1: Đếm ngược (Cộng thêm giây) ===
            btnStartCountdown.Click += (s, e) => {
                if (numHoursCD.Value == 0 && numMinutesCD.Value == 0 && numSecondsCD.Value == 0) return;

                _targetTime = DateTime.Now
                    .AddHours((int)numHoursCD.Value)
                    .AddMinutes((int)numMinutesCD.Value)
                    .AddSeconds((int)numSecondsCD.Value);

                StartSystem(1, $"Tắt lúc: {_targetTime:HH:mm:ss}");
            };

            // === LOGIC TAB 2: Chọn giờ cụ thể (Cộng thêm giây) ===
            btnStartTime.Click += (s, e) => {
                DateTime now = DateTime.Now;
                DateTime selected = new DateTime(now.Year, now.Month, now.Day,
                    (int)numHoursTime.Value,
                    (int)numMinutesTime.Value,
                    (int)numSecondsTime.Value);

                if (selected < now) selected = selected.AddDays(1);

                _targetTime = selected;
                StartSystem(2, $"Tắt lúc: {_targetTime:HH:mm:ss}");
            };

            btnStartProcess.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtProcessName.Text)) { MessageBox.Show("Nhập tên app!"); return; }
                _targetProcessName = txtProcessName.Text.Trim().ToLower().Replace(".exe", "");
                StartSystem(3, $"Đang canh app: {_targetProcessName}");
            };

            lnkAuthor.Click += (s, e) => {
                try { Process.Start(new ProcessStartInfo { FileName = "https://www.facebook.com/toanbb.pro/", UseShellExecute = true }); }
                catch { MessageBox.Show("Không mở được trình duyệt!"); }
            };

            btnCancel.Click += BtnCancel_Click;

            // System Tray - Sử dụng Icon từ Resource
            notifyIcon = new NotifyIcon { Icon = this.Icon, Text = "GOOT", Visible = false };
            notifyIcon.MouseDoubleClick += (s, e) => ShowForm();

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Mở giao diện", null, (s, e) => ShowForm());
            menu.Items.Add("Thoát hẳn", null, (s, e) => ExitApp());
            notifyIcon.ContextMenuStrip = menu;
        }

        private void StartSystem(int mode, string msg)
        {
            if (chkPassword.Checked && string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu bảo vệ trước khi bắt đầu!");
                return;
            }

            _mode = mode;
            _isRunning = true;
            lblStatus.Text = msg;
            lblStatus.ForeColor = Color.Red;
            ToggleInputs(false);
            mainTimer.Start();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (!_isRunning) return;

            if (chkPassword.Checked)
            {
                string pass = Prompt.ShowDialog("Nhập mật khẩu để hủy:", "Bảo mật");
                if (pass != txtPassword.Text) { MessageBox.Show("Sai mật khẩu!"); return; }
            }
            StopSystem();
        }

        private void StopSystem()
        {
            mainTimer.Stop();
            _isRunning = false;
            _mode = 0;
            lblStatus.Text = "Đã hủy hẹn giờ";
            lblStatus.ForeColor = Color.DarkSlateGray;
            ToggleInputs(true);
        }

        private void ToggleInputs(bool enable)
        {
            tabControl.Enabled = enable;
            chkPassword.Enabled = enable;
            txtPassword.Enabled = enable;
            btnCancel.Text = enable ? "HỦY HẸN GIỜ" : "DỪNG (Cần Pass)";
        }

        private void MainTimer_Tick(object sender, EventArgs e)
        {
            switch (_mode)
            {
                case 1:
                case 2:
                    TimeSpan remain = _targetTime - DateTime.Now;
                    if (remain.TotalSeconds <= 0) ShutdownNow();
                    else lblStatus.Text = $"Còn: {remain.Hours:00}:{remain.Minutes:00}:{remain.Seconds:00}";
                    break;
                case 3:
                    if (Process.GetProcessesByName(_targetProcessName).Length > 0) ShutdownNow();
                    break;
            }
        }

        private void ShutdownNow()
        {
            mainTimer.Stop();
            Process.Start(new ProcessStartInfo("shutdown", "/s /f /t 0") { CreateNoWindow = true, UseShellExecute = false });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isRunning && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(2000, "GOOT", "Ứng dụng đang chạy ngầm...", ToolTipIcon.Info);
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        private void ShowForm() { Show(); WindowState = FormWindowState.Normal; notifyIcon.Visible = false; }

        private void ExitApp()
        {
            if (_isRunning && chkPassword.Checked)
            {
                string pass = Prompt.ShowDialog("Nhập mật khẩu để thoát:", "Thoát ứng dụng");
                if (pass != txtPassword.Text) { MessageBox.Show("Sai mật khẩu!"); return; }
            }
            _isRunning = false;
            Application.Exit();
        }
    }

    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form() { Width = 350, Height = 160, FormBorderStyle = FormBorderStyle.FixedDialog, Text = caption, StartPosition = FormStartPosition.CenterScreen, MinimizeBox = false, MaximizeBox = false };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 300, PasswordChar = '•' };
            Button confirmation = new Button() { Text = "Xác nhận", Left = 220, Width = 100, Top = 80, DialogResult = DialogResult.OK };
            prompt.Controls.Add(textBox); prompt.Controls.Add(confirmation); prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}