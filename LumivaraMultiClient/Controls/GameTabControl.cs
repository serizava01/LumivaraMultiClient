using System;
using System.Threading.Tasks;
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

        private bool isInitializing = false;

        public GameTabControl(AccountProfile profile)
        {
            Profile = profile;

            InitializeComponents();

            _ = InitGameAsync();
        }

        private void InitializeComponents()
        {
            this.Dock = DockStyle.Fill;

            // =========================
            // TOP BAR
            // =========================
            topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 32;

            // =========================
            // REFRESH BUTTON
            // =========================
            btnRefresh = new Button();
            btnRefresh.Text = "รีเฟรช";
            btnRefresh.Width = 75;
            btnRefresh.Height = 30;
            btnRefresh.Dock = DockStyle.Left;
            btnRefresh.FlatStyle = FlatStyle.Standard;
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.BackColor = System.Drawing.SystemColors.Control;
            btnRefresh.ForeColor = System.Drawing.SystemColors.ControlText;
            btnRefresh.Click += BtnRefresh_Click;

            // =========================
            // MUTE BUTTON
            // =========================
            btnMute = new Button();
            btnMute.Text = "ปิดเสียง";
            btnMute.Width = 90;
            btnMute.Height = 30;
            btnMute.Dock = DockStyle.Left;
            btnMute.FlatStyle = FlatStyle.Standard;
            btnMute.UseVisualStyleBackColor = true;
            btnMute.BackColor = System.Drawing.SystemColors.Control;
            btnMute.ForeColor = System.Drawing.SystemColors.ControlText;
            btnMute.Click += BtnMute_Click;

            topBar.Controls.Add(btnMute);
            topBar.Controls.Add(btnRefresh);

            // =========================
            // WEBVIEW
            // =========================
            WebView = new WebView2();
            WebView.Dock = DockStyle.Fill;

            this.Controls.Add(WebView);
            this.Controls.Add(topBar);
        }

        // =========================================================
        // INITIALIZE WEBVIEW
        // =========================================================
        private async Task InitGameAsync()
        {
            if (WebView == null)
                return;

            if (isInitializing)
                return;

            isInitializing = true;

            try
            {
                await WebViewManager.InitializeInstanceAsync(
                    WebView,
                    Profile.ProfileId
                );

                if (WebView.CoreWebView2 != null)
                {
                    WebView.Source = new Uri(Profile.TargetUrl);

                    UpdateMuteButton();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "ไม่สามารถเปิด Client ได้\n\n" + ex.Message,
                    "Lumivara Multi Client",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                isInitializing = false;
            }
        }

        // =========================================================
        // REFRESH
        // =========================================================
        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            if (WebView == null)
                return;

            if (WebView.CoreWebView2 == null)
                return;

            try
            {
                WebView.CoreWebView2.Reload();
            }
            catch
            {

            }
        }

        // =========================================================
        // MUTE / UNMUTE
        // =========================================================
        private void BtnMute_Click(object sender, EventArgs e)
        {
            if (WebView == null)
                return;

            if (WebView.CoreWebView2 == null)
                return;

            try
            {
                WebView.CoreWebView2.IsMuted =
                    !WebView.CoreWebView2.IsMuted;

                UpdateMuteButton();
            }
            catch
            {

            }
        }

        // =========================================================
        // UPDATE MUTE BUTTON TEXT
        // =========================================================
        private void UpdateMuteButton()
        {
            if (btnMute == null)
                return;

            if (WebView == null)
                return;

            if (WebView.CoreWebView2 == null)
                return;

            btnMute.Text =
                WebView.CoreWebView2.IsMuted
                    ? "เปิดเสียง"
                    : "ปิดเสียง";
        }

        // =========================================================
        // CLEANUP
        // =========================================================
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (WebView != null)
                    {
                        WebView.Dispose();
                        WebView = null;
                    }
                }
                catch
                {
                }
            }

            base.Dispose(disposing);
        }
    }
}