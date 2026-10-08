using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using LumivaraMultiClient.Models;
using LumivaraMultiClient.Services;

namespace LumivaraMultiClient.Controls
{
    public class GameTabControl : UserControl
    {
        // =========================================================
        // PUBLIC
        // =========================================================

        public WebView2 WebView
        {
            get;
            private set;
        }

        public AccountProfile Profile
        {
            get;
            private set;
        }

        // =========================================================
        // UI
        // =========================================================

        private Panel topBar;
        private Button btnMute;
        private Button btnRefresh;

        // =========================================================
        // STATE
        // =========================================================

        private bool isInitializing = false;
        private bool isDisposed = false;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public GameTabControl(
            AccountProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(
                    "profile"
                );
            }

            Profile = profile;

            InitializeComponents();

            _ = InitGameAsync();
        }

        // =========================================================
        // INITIALIZE COMPONENTS
        // =========================================================

        private void InitializeComponents()
        {
            this.Dock =
                DockStyle.Fill;

            // =====================================================
            // TOP BAR
            // =====================================================

            topBar =
                new Panel();

            topBar.Dock =
                DockStyle.Top;

            topBar.Height =
                32;

            // =====================================================
            // REFRESH BUTTON
            // =====================================================

            btnRefresh =
                new Button();

            btnRefresh.Text =
                "รีเฟรช";

            btnRefresh.Width =
                75;

            btnRefresh.Height =
                30;

            btnRefresh.Dock =
                DockStyle.Left;

            btnRefresh.FlatStyle =
                FlatStyle.Standard;

            btnRefresh.UseVisualStyleBackColor =
                true;

            btnRefresh.BackColor =
                System.Drawing.SystemColors.Control;

            btnRefresh.ForeColor =
                System.Drawing.SystemColors.ControlText;

            btnRefresh.Click +=
                BtnRefresh_Click;

            // =====================================================
            // MUTE BUTTON
            // =====================================================

            btnMute =
                new Button();

            btnMute.Text =
                "ปิดเสียง";

            btnMute.Width =
                90;

            btnMute.Height =
                30;

            btnMute.Dock =
                DockStyle.Left;

            btnMute.FlatStyle =
                FlatStyle.Standard;

            btnMute.UseVisualStyleBackColor =
                true;

            btnMute.BackColor =
                System.Drawing.SystemColors.Control;

            btnMute.ForeColor =
                System.Drawing.SystemColors.ControlText;

            btnMute.Click +=
                BtnMute_Click;

            // =====================================================
            // ADD BUTTONS
            // =====================================================

            topBar.Controls.Add(
                btnMute
            );

            topBar.Controls.Add(
                btnRefresh
            );

            // =====================================================
            // WEBVIEW
            // =====================================================

            WebView =
                new WebView2();

            WebView.Dock =
                DockStyle.Fill;

            this.Controls.Add(
                WebView
            );

            this.Controls.Add(
                topBar
            );
        }

        // =========================================================
        // INITIALIZE WEBVIEW
        // =========================================================

        private async Task InitGameAsync()
        {
            if (isDisposed)
                return;

            if (WebView == null)
                return;

            if (isInitializing)
                return;

            isInitializing =
                true;

            try
            {
                // =================================================
                // INITIALIZE
                // =================================================

                await WebViewManager
                    .InitializeInstanceAsync(
                        WebView,
                        Profile.ProfileId
                    );

                if (isDisposed)
                    return;

                if (WebView == null)
                    return;

                if (WebView.CoreWebView2 == null)
                    return;

                // =================================================
                // NAVIGATE
                // =================================================

                if (!string.IsNullOrWhiteSpace(
                    Profile.TargetUrl))
                {
                    WebView.Source =
                        new Uri(
                            Profile.TargetUrl
                        );
                }

                // =================================================
                // ACTIVE CLIENT
                // =================================================

                WebViewManager.SetMemoryNormal(
                    WebView
                );

                // =================================================
                // MUTE BUTTON
                // =================================================

                UpdateMuteButton();
            }
            catch (Exception ex)
            {
                if (isDisposed)
                    return;

                MessageBox.Show(
                    "ไม่สามารถเปิด Client ได้\n\n" +
                    ex.Message,

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
        // SET ACTIVE
        // =========================================================
        // Active = Normal
        // Inactive =  Low
        // =========================================================

        public void SetActive(
            bool active)
        {
            if (isDisposed)
                return;

            if (WebView == null)
                return;

            if (WebView.CoreWebView2 == null)
                return;

            if (active)
            {
                WebViewManager.SetMemoryNormal(
                    WebView
                );
            }
            else
            {
                WebViewManager.SetMemoryLow(
                    WebView
                );
            }
        }

        // =========================================================
        // REFRESH
        // =========================================================

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            if (isDisposed)
                return;

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

        private void BtnMute_Click(
            object sender,
            EventArgs e)
        {
            if (isDisposed)
                return;

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
        // UPDATE MUTE BUTTON
        // =========================================================

        private void UpdateMuteButton()
        {
            if (btnMute == null)
                return;

            if (WebView == null)
                return;

            if (WebView.CoreWebView2 == null)
                return;

            try
            {
                btnMute.Text = WebView.CoreWebView2.IsMuted ? "เปิดเสียง" : "ปิดเสียง";
            }
            catch
            {


            }
        }

        // =========================================================
        // GET MEMORY LEVEL
        // =========================================================

        public CoreWebView2MemoryUsageTargetLevel
            GetMemoryUsageLevel()
        {
            if (WebView == null)
            {
                return CoreWebView2MemoryUsageTargetLevel.Low;
            }
            return WebViewManager.GetMemoryLevel(WebView);
        }

        // =========================================================
        // CLEANUP
        // =========================================================

        protected override void Dispose(
            bool disposing)
        {
            if (isDisposed)
            {
                base.Dispose(disposing);
                return;
            }

            isDisposed =
                true;

            if (disposing)
            {
                try
                {
                    if (WebView != null)
                    {
                        WebViewManager.SetMemoryLow(
                            WebView
                        );
                    }
                }
                catch
                {
                }

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

                try
                {
                    if (btnMute != null)
                    {
                        btnMute.Click -=
                            BtnMute_Click;
                    }
                }
                catch
                {
                }

                try
                {
                    if (btnRefresh != null)
                    {
                        btnRefresh.Click -=
                            BtnRefresh_Click;
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