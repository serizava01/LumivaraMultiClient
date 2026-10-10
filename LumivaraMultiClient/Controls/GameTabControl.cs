
using System;
using System.Diagnostics;
using System.IO;
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

        public WebView2 WebView { get; private set; }

        public AccountProfile Profile { get; private set; }

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
        private bool isRefreshing = false;

        private readonly Stopwatch refreshTimer = new Stopwatch();

        private readonly string DiagnosticLogPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "LumivaraMultiClient",
            "RefreshLog.txt");

        // ป้องกันปุ่มค้าง หากโหลดไม่เสร็จภายใน 180 วินาที
        private System.Windows.Forms.Timer refreshTimeoutTimer;

        private const int RefreshTimeoutMilliseconds = 180000;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public GameTabControl(AccountProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException("profile");

            Profile = profile;

            InitializeComponents();

            _ = InitGameAsync();
        }

        // =========================================================
        // LOG
        // =========================================================

        private void WriteRefreshLog(string message)
        {
            try
            {
                string folder = Path.GetDirectoryName(
                    DiagnosticLogPath);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string profileId = Profile != null
                    ? Profile.ProfileId
                    : "Unknown";

                string logLine =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                    + " | " + profileId
                    + " | " + message
                    + Environment.NewLine;

                File.AppendAllText(
                    DiagnosticLogPath,
                    logLine);
            }
            catch
            {
                // ไม่ให้ปัญหาการเขียน Log กระทบการทำงานของเกม
            }
        }

        // =========================================================
        // INITIALIZE COMPONENTS
        // =========================================================

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;

            // TOP BAR
            topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 32;

            // REFRESH BUTTON
            btnRefresh = new Button();
            btnRefresh.Text = "รีเฟรช";
            btnRefresh.Width = 75;
            btnRefresh.Height = 30;
            btnRefresh.Dock = DockStyle.Left;
            btnRefresh.FlatStyle = FlatStyle.Standard;
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.BackColor =
                System.Drawing.SystemColors.Control;
            btnRefresh.ForeColor =
                System.Drawing.SystemColors.ControlText;

            btnRefresh.Click += BtnRefresh_Click;

            // MUTE BUTTON
            btnMute = new Button();
            btnMute.Text = "ปิดเสียง";
            btnMute.Width = 90;
            btnMute.Height = 30;
            btnMute.Dock = DockStyle.Left;
            btnMute.FlatStyle = FlatStyle.Standard;
            btnMute.UseVisualStyleBackColor = true;
            btnMute.BackColor =
                System.Drawing.SystemColors.Control;
            btnMute.ForeColor =
                System.Drawing.SystemColors.ControlText;

            btnMute.Click += BtnMute_Click;

            // ADD BUTTONS
            topBar.Controls.Add(btnMute);
            topBar.Controls.Add(btnRefresh);

            // WEBVIEW
            WebView = new WebView2();
            WebView.Dock = DockStyle.Fill;

            Controls.Add(WebView);
            Controls.Add(topBar);

            // REFRESH TIMEOUT
            refreshTimeoutTimer =
                new System.Windows.Forms.Timer();

            refreshTimeoutTimer.Interval =
                RefreshTimeoutMilliseconds;

            refreshTimeoutTimer.Tick +=
                RefreshTimeoutTimer_Tick;
        }

        // =========================================================
        // INITIALIZE WEBVIEW
        // =========================================================

        private async Task InitGameAsync()
        {
            if (isDisposed || WebView == null || isInitializing)
                return;

            isInitializing = true;

            try
            {
                WriteRefreshLog("WebView initialization started");

                await WebViewManager.InitializeInstanceAsync(
                    WebView,
                    Profile.ProfileId);

                if (isDisposed || WebView == null)
                    return;

                if (WebView.CoreWebView2 == null)
                {
                    WriteRefreshLog(
                        "Initialization failed: CoreWebView2 is null");
                    return;
                }

                WebView.CoreWebView2.NavigationCompleted -=
                    WebView_NavigationCompleted;

                WebView.CoreWebView2.NavigationCompleted +=
                    WebView_NavigationCompleted;

                if (!string.IsNullOrWhiteSpace(Profile.TargetUrl))
                {
                    WriteRefreshLog(
                        "Initial navigation: " + Profile.TargetUrl);

                    WebView.Source = new Uri(Profile.TargetUrl);
                }

                WebViewManager.SetMemoryNormal(WebView);

                UpdateMuteButton();

                WriteRefreshLog("WebView initialization completed");
            }
            catch (Exception ex)
            {
                WriteRefreshLog(
                    "Initialization exception: " + ex);

                if (!isDisposed)
                {
                    MessageBox.Show(
                        "ไม่สามารถเปิด Client ได้\n\n" + ex.Message,
                        "Lumivara Multi Client",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                isInitializing = false;
            }
        }

        // =========================================================
        // SET ACTIVE
        // =========================================================

        public void SetActive(bool active)
        {
            if (isDisposed || WebView == null)
                return;

            if (WebView.CoreWebView2 == null)
                return;

            try
            {
                if (active)
                    WebViewManager.SetMemoryNormal(WebView);
                else
                    WebViewManager.SetMemoryLow(WebView);
            }
            catch (Exception ex)
            {
                WriteRefreshLog(
                    "SetActive exception: " + ex.Message);
            }
        }

        // =========================================================
        // REFRESH
        // =========================================================

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            if (isDisposed || WebView == null)
                return;

            if (WebView.CoreWebView2 == null)
            {
                WriteRefreshLog(
                    "Refresh rejected: CoreWebView2 is not ready");
                return;
            }

            // ป้องกันการสั่งรีเฟรชซ้ำระหว่างที่กำลังโหลด
            if (isRefreshing)
            {
                WriteRefreshLog(
                    "Refresh ignored: another refresh is in progress");
                return;
            }

            try
            {
                isRefreshing = true;
                refreshTimer.Reset();
                refreshTimer.Start();

                btnRefresh.Enabled = false;
                btnRefresh.Text = "กำลังโหลด...";

                refreshTimeoutTimer.Stop();
                refreshTimeoutTimer.Start();

                WriteRefreshLog(
                    "Refresh started"
                    + " | URL: " + WebView.Source
                    + " | Timeout: "
                    + (RefreshTimeoutMilliseconds / 1000)
                    + " seconds");

                WebView.CoreWebView2.Reload();
            }
            catch (Exception ex)
            {
                WriteRefreshLog(
                    "Refresh exception: " + ex);

                FinishRefresh("Refresh command failed");
            }
        }

        // =========================================================
        // NAVIGATION COMPLETED
        // =========================================================

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (isDisposed)
                return;

            if (!isRefreshing)
                return;

            long elapsedMilliseconds =
                refreshTimer.ElapsedMilliseconds;

            string status = e.IsSuccess
                ? "SUCCESS"
                : "FAILED";

            string errorStatus = e.IsSuccess
                ? "None"
                : e.WebErrorStatus.ToString();

            WriteRefreshLog(
                "Refresh completed"
                + " | Result: " + status
                + " | Duration: " + elapsedMilliseconds + " ms"
                + " (" + (elapsedMilliseconds / 1000.0).ToString("F2")
                + " sec)"
                + " | WebErrorStatus: " + errorStatus
                + " | URL: " + WebView.Source);

            FinishRefresh("Navigation completed");
        }

        // =========================================================
        // REFRESH TIMEOUT
        // =========================================================

        private void RefreshTimeoutTimer_Tick(
            object sender,
            EventArgs e)
        {
            if (isDisposed || !isRefreshing)
                return;

            WriteRefreshLog(
                "Refresh TIMEOUT"
                + " | Elapsed: "
                + refreshTimer.ElapsedMilliseconds + " ms"
                + " | URL: "
                + (WebView != null ? WebView.Source.ToString() : "Unknown")
                + " | Navigation may still be running");

            FinishRefresh("Refresh timeout");
        }

        // =========================================================
        // FINISH REFRESH
        // =========================================================

        private void FinishRefresh(string reason)
        {
            if (refreshTimeoutTimer != null)
                refreshTimeoutTimer.Stop();

            if (refreshTimer.IsRunning)
                refreshTimer.Stop();

            isRefreshing = false;

            if (isDisposed)
                return;

            if (btnRefresh != null && !btnRefresh.IsDisposed)
            {
                btnRefresh.Enabled = true;
                btnRefresh.Text = "รีเฟรช";
            }

            WriteRefreshLog("Refresh state reset | " + reason);
        }

        // =========================================================
        // MUTE / UNMUTE
        // =========================================================

        private void BtnMute_Click(object sender, EventArgs e)
        {
            if (isDisposed || WebView == null)
                return;

            if (WebView.CoreWebView2 == null)
                return;

            try
            {
                WebView.CoreWebView2.IsMuted =
                    !WebView.CoreWebView2.IsMuted;

                UpdateMuteButton();

                WriteRefreshLog(
                    WebView.CoreWebView2.IsMuted
                        ? "Audio muted"
                        : "Audio unmuted");
            }
            catch (Exception ex)
            {
                WriteRefreshLog(
                    "Mute exception: " + ex.Message);
            }
        }

        // =========================================================
        // UPDATE MUTE BUTTON
        // =========================================================

        private void UpdateMuteButton()
        {
            if (btnMute == null || WebView == null)
                return;

            if (WebView.CoreWebView2 == null)
                return;

            try
            {
                btnMute.Text =
                    WebView.CoreWebView2.IsMuted
                        ? "เปิดเสียง"
                        : "ปิดเสียง";
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

        protected override void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                base.Dispose(disposing);
                return;
            }

            isDisposed = true;

            if (disposing)
            {
                try
                {
                    if (refreshTimeoutTimer != null)
                    {
                        refreshTimeoutTimer.Stop();

                        refreshTimeoutTimer.Tick -=
                            RefreshTimeoutTimer_Tick;

                        refreshTimeoutTimer.Dispose();
                        refreshTimeoutTimer = null;
                    }
                }
                catch
                {
                }

                try
                {
                    refreshTimer.Stop();
                }
                catch
                {
                }

                try
                {
                    if (WebView != null)
                    {
                        WebViewManager.SetMemoryLow(WebView);
                    }
                }
                catch
                {
                }

                try
                {
                    if (WebView != null)
                    {
                        if (WebView.CoreWebView2 != null)
                        {
                            WebView.CoreWebView2.NavigationCompleted -=
                                WebView_NavigationCompleted;
                        }

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
                        btnMute.Click -= BtnMute_Click;

                    if (btnRefresh != null)
                        btnRefresh.Click -= BtnRefresh_Click;
                }
                catch
                {
                }
            }

            base.Dispose(disposing);
        }
    }
}
