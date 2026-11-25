using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LumivaraMultiClient.Controls;

namespace LumivaraMultiClient.Forms
{
    public class MiniViewForm : Form
    {
        public event Action<string> ClientClicked;
        public string ProfileId { get; private set; }

        private readonly GameTabControl gameControl;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public MiniViewForm(string profileId, string title, GameTabControl sourceControl)
        {
            ProfileId = profileId;
            gameControl = sourceControl;

            // ตั้งค่าหน้าต่างลอย
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(320, 240);
            BackColor = Color.Black;

            // คลิกที่หน้าต่างเพื่อสลับจอ
            this.Click += (s, e) =>
            {
                if (ClientClicked != null) ClientClicked(ProfileId);
            };

            // ลากย้ายหน้าต่าง
            this.MouseDown += MiniViewForm_MouseDown;

            // แถบชื่อด้านบน
            Label lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Padding = new Padding(5, 0, 0, 0),
                Cursor = Cursors.SizeAll
            };
            lblTitle.MouseDown += MiniViewForm_MouseDown;
            lblTitle.Click += (s, e) => { if (ClientClicked != null) ClientClicked(ProfileId); };
            Controls.Add(lblTitle);

            // ย้าย WebView เข้ามาใน Form นี้
            this.HandleCreated += (s, e) => AttachWebView();
            this.FormClosed += (s, e) => DetachWebView();
        }

        private void MiniViewForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void AttachWebView()
        {
            if (gameControl == null || gameControl.WebView == null || gameControl.WebView.IsDisposed)
                return;

            // นำ UserControl ทั้งก้อนมาใส่ใน Form
            gameControl.Dock = DockStyle.Fill;
            this.Controls.Add(gameControl);
        }

        private void DetachWebView()
        {
            // คืนค่าเมื่อปิดหน้าต่าง
            if (gameControl != null && !gameControl.IsDisposed)
            {
                this.Controls.Remove(gameControl);
            }
        }
    }
}