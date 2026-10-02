using System;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using LumivaraMultiClient.Models;
using LumivaraMultiClient.Services;

namespace LumivaraMultiClient.Controls
{
    public class GameTabControl : UserControl
    {
        public WebView2 WebView { get; private set; }
        public AccountProfile Profile { get; private set; }

        private Panel topBar;
        private Button btnMute;
        private Button btnRefresh;

        public GameTabControl(AccountProfile profile)
        {
            Profile = profile;
            InitializeComponents();
            _ = InitGameAsync();
        }
        /// <summary>
        /// ตัวจัดการส่วนประกอบของ GameTabControl โดยสร้างแถบด้านบน (topBar) และปุ่มต่าง ๆ (btnMute, btnRefresh) และเพิ่ม WebView2 ลงใน UserControl
        /// </summary>
        private void InitializeComponents()
        {
            this.Dock = DockStyle.Fill;
            topBar = new Panel { Dock = DockStyle.Top, Height = 30 };

            btnRefresh = new Button { Text = "รีเฟรช", Width = 75, Dock = DockStyle.Left };
            btnRefresh.Click += (s, e) => WebView?.Reload();

            btnMute = new Button { Text = "ปิด/เปิดเสียง", Width = 110, Dock = DockStyle.Left };
            btnMute.Click += ToggleMute;
            
            topBar.Controls.Add(btnMute);
            topBar.Controls.Add(btnRefresh);
            WebView = new WebView2 { Dock = DockStyle.Fill };

            this.Controls.Add(WebView);
            this.Controls.Add(topBar);
        }

        private async System.Threading.Tasks.Task InitGameAsync()
        {
            await WebViewManager.InitializeInstanceAsync(WebView, Profile.ProfileId);
            WebView.Source = new Uri(Profile.TargetUrl);
        }

        private void ToggleMute(object sender, EventArgs e)
        {
            if (WebView?.CoreWebView2 != null)
            {
                WebView.CoreWebView2.IsMuted = !WebView.CoreWebView2.IsMuted;
                btnMute.Text = WebView.CoreWebView2.IsMuted ? "เปิดเสียง" : "ปิดเสียง";
            }
        }
    }
}