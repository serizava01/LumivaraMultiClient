using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        public WebView2 WebView { get; private set; }
        public AccountProfile Profile { get; private set; }

        private Panel topBar;
        private Button btnMute;
        private Button btnRefresh;
        private CheckBox chkAutoRefresh;

        private bool isInitializing;
        private bool isDisposed;
        private bool isRefreshing;
        private bool isCollectingDiagnostic;
        private bool isActive = true;

        // ตัวนับรอบที่ FPS ตกต่อเนื่อง
        private int consecutiveLowFpsCount = 0;
        private const int LowFpsThreshold = 10;
        private const int MaxConsecutiveLowFpsChecks = 3; // 3 รอบ x 30 วินาที = 1.5 นาที
        private const double MaxHeapUsageRatio = 0.85;    // รีเฟรชเมื่อ JS Heap เกิน 85%

        private readonly Stopwatch refreshTimer = new Stopwatch();
        private readonly Dictionary<ulong, Stopwatch> navigationTimers =
            new Dictionary<ulong, Stopwatch>();

        private System.Windows.Forms.Timer refreshProgressTimer;
        private System.Windows.Forms.Timer diagnosticTimer;
        private System.Windows.Forms.Timer refreshTimeoutTimer;

        private DateTime lastProcessSampleTime = DateTime.MinValue;
        private TimeSpan lastWebViewCpuTime = TimeSpan.Zero;

        private const int RefreshProgressIntervalMilliseconds = 15000;
        private const int DiagnosticIntervalMilliseconds = 30000;
        private const int RefreshTimeoutMilliseconds = 180000;

        public bool AutoRefreshEnabled
        {
            get { return chkAutoRefresh != null && chkAutoRefresh.Checked; }
            set { if (chkAutoRefresh != null) chkAutoRefresh.Checked = value; }
        }

        public GameTabControl(AccountProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException("profile");

            Profile = profile;
            InitializeComponents();
            _ = InitGameAsync();
        }

        /// <summary>
        /// เปลี่ยน URL ของจอนี้ (เรียกได้ทั้งตอนรันอยู่ และตอนยังไม่รัน)
        /// </summary>
        public void NavigateTo(string url)
        {
            if (isDisposed || string.IsNullOrWhiteSpace(url))
                return;

            Profile.TargetUrl = url;

            if (WebView != null && WebView.CoreWebView2 != null)
            {
                try
                {
                    Uri uri;
                    if (Uri.TryCreate(url, UriKind.Absolute, out uri))
                    {
                        WriteRefreshLog("NavigateTo: " + GetSafeUrl(url));
                        WebView.Source = uri;
                    }
                    else
                    {
                        WriteRefreshLog("NavigateTo rejected (invalid URL): " + url);
                    }
                }
                catch (Exception ex)
                {
                    WriteRefreshLog("NavigateTo exception: " + ex.Message);
                }
            }
            else
            {
                WriteRefreshLog("NavigateTo deferred (CoreWebView2 not ready): " + url);
            }
        }

        private void WriteRefreshLog(string message)
        {
            string profileId = Profile != null ? Profile.ProfileId : "Unknown";
            WebViewManager.WriteDiagnosticLog(profileId, message);
        }

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            topBar = new Panel { Dock = DockStyle.Top, Height = 32 };

            btnRefresh = new Button
            {
                Text = "รีเฟรช",
                Width = 75,
                Height = 30,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Standard,
                UseVisualStyleBackColor = true,
                BackColor = System.Drawing.SystemColors.Control,
                ForeColor = System.Drawing.SystemColors.ControlText
            };
            btnRefresh.Click += BtnRefresh_Click;

            btnMute = new Button
            {
                Text = "ปิดเสียง",
                Width = 90,
                Height = 30,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Standard,
                UseVisualStyleBackColor = true,
                BackColor = System.Drawing.SystemColors.Control,
                ForeColor = System.Drawing.SystemColors.ControlText
            };
            btnMute.Click += BtnMute_Click;

            chkAutoRefresh = new CheckBox
            {
                Text = "ออโต้รีเฟรชเมื่อค้าง/แลค",
                AutoSize = false,
                Width = 175,
                Height = 30,
                Dock = DockStyle.Left,
                Padding = new Padding(10, 0, 0, 0),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Checked = false,
                ForeColor = System.Drawing.Color.Green,
                Cursor = Cursors.Hand
            };
            chkAutoRefresh.CheckedChanged += (s, e) =>
            {
                consecutiveLowFpsCount = 0;
                WriteRefreshLog("Auto-refresh setting changed: " +
                    (chkAutoRefresh.Checked ? "ENABLED" : "DISABLED"));
            };

            topBar.Controls.Add(chkAutoRefresh);
            topBar.Controls.Add(btnMute);
            topBar.Controls.Add(btnRefresh);

            WebView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(WebView);
            Controls.Add(topBar);

            refreshTimeoutTimer = new System.Windows.Forms.Timer
            {
                Interval = RefreshTimeoutMilliseconds
            };
            refreshTimeoutTimer.Tick += RefreshTimeoutTimer_Tick;

            refreshProgressTimer = new System.Windows.Forms.Timer
            {
                Interval = RefreshProgressIntervalMilliseconds
            };
            refreshProgressTimer.Tick += RefreshProgressTimer_Tick;

            diagnosticTimer = new System.Windows.Forms.Timer
            {
                Interval = DiagnosticIntervalMilliseconds
            };
            diagnosticTimer.Tick += DiagnosticTimer_Tick;
        }

        private async Task InitGameAsync()
        {
            if (isDisposed || WebView == null || isInitializing)
                return;

            isInitializing = true;
            try
            {
                WriteRefreshLog("WebView initialization started");
                await WebViewManager.InitializeInstanceAsync(WebView, Profile.ProfileId);

                if (isDisposed || WebView == null || WebView.CoreWebView2 == null)
                {
                    WriteRefreshLog("Initialization failed: CoreWebView2 is null");
                    return;
                }

                CoreWebView2 core = WebView.CoreWebView2;
                core.NavigationStarting -= WebView_NavigationStarting;
                core.NavigationStarting += WebView_NavigationStarting;
                core.DOMContentLoaded -= WebView_DOMContentLoaded;
                core.DOMContentLoaded += WebView_DOMContentLoaded;
                core.NavigationCompleted -= WebView_NavigationCompleted;
                core.NavigationCompleted += WebView_NavigationCompleted;
                core.WebResourceResponseReceived -= WebView_WebResourceResponseReceived;
                core.WebResourceResponseReceived += WebView_WebResourceResponseReceived;
                core.ProcessFailed -= WebView_ProcessFailed;
                core.ProcessFailed += WebView_ProcessFailed;

                await core.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    (function () {
                        if (window.__lumiDiagInstalled) return;
                        window.__lumiDiagInstalled = true;

                        var frames = 0;
                        var lastMeasure = performance.now();
                        var currentFps = 0;
                        var enabled = true;
                        var rafRunning = false;

                        function countFrame(now) {
                            rafRunning = false;
                            if (!enabled) {
                                window.__lumiDiagFps = 0;
                                return;
                            }

                            frames++;
                            var elapsed = now - lastMeasure;
                            if (elapsed >= 1000) {
                                currentFps = Math.round(frames * 1000 / elapsed);
                                frames = 0;
                                lastMeasure = now;
                            }

                            window.__lumiDiagFps = currentFps;
                            rafRunning = true;
                            requestAnimationFrame(countFrame);
                        }

                        window.__lumiDiagSetEnabled = function (value) {
                            enabled = !!value;
                            if (!enabled) {
                                currentFps = 0;
                                window.__lumiDiagFps = 0;
                                return;
                            }

                            if (!rafRunning) {
                                frames = 0;
                                lastMeasure = performance.now();
                                rafRunning = true;
                                requestAnimationFrame(countFrame);
                            }
                        };

                        window.__lumiDiagRead = function () {
                            var heapUsed = null, heapLimit = null;
                            try {
                                if (performance.memory) {
                                    heapUsed = performance.memory.usedJSHeapSize;
                                    heapLimit = performance.memory.jsHeapSizeLimit;
                                }
                            } catch (e) {}

                            return {
                                fps: window.__lumiDiagFps || 0,
                                fpsSource: 'requestAnimationFrame estimate, not game-engine FPS',
                                visibilityState: document.visibilityState,
                                readyState: document.readyState,
                                domNodes: document.getElementsByTagName('*').length,
                                resourceCount: performance.getEntriesByType('resource').length,
                                jsHeapUsed: heapUsed,
                                jsHeapLimit: heapLimit
                            };
                        };

                        rafRunning = true;
                        requestAnimationFrame(countFrame);
                    })();
                ");

                if (diagnosticTimer != null && isActive)
                    diagnosticTimer.Start();

                if (!string.IsNullOrWhiteSpace(Profile.TargetUrl))
                {
                    WriteRefreshLog("Initial navigation: " + GetSafeUrl(Profile.TargetUrl));
                    WebView.Source = new Uri(Profile.TargetUrl);
                }

                if (isActive)
                    WebViewManager.SetMemoryNormal(WebView);
                else
                    WebViewManager.SetMemoryLow(WebView);

                UpdateMuteButton();
                WriteRefreshLog("WebView initialization completed");
            }
            catch (Exception ex)
            {
                WriteRefreshLog("Initialization exception: " + ex);
                if (!isDisposed)
                    MessageBox.Show("ไม่สามารถเปิด Client ได้\n\n" + ex.Message,
                        "Online Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isInitializing = false;
            }
        }

        private async void WebView_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            if (isDisposed) return;

            WriteRefreshLog("CRITICAL: WebView2 ProcessFailed | Kind: " + e.ProcessFailedKind + " | Reason: " + e.Reason);

            if (!AutoRefreshEnabled)
            {
                WriteRefreshLog("Auto-recovery skipped because AutoRefresh is disabled for this client.");
                if (!isDisposed && btnRefresh != null && !btnRefresh.IsDisposed)
                {
                    btnRefresh.Text = "ค้าง (กดรี)";
                }
                return;
            }

            await Task.Delay(2000);
            if (!isDisposed && WebView != null && WebView.CoreWebView2 != null)
            {
                try
                {
                    WriteRefreshLog("Auto-recovering crashed WebView2...");
                    TriggerRefresh("ProcessFailed auto-recovery");
                }
                catch (Exception ex)
                {
                    WriteRefreshLog("Auto-recovery failed: " + ex.Message);
                }
            }
        }

        public void SetActive(bool active)
        {
            if (isDisposed)
                return;

            isActive = active;
            consecutiveLowFpsCount = 0;

            try
            {
                if (WebView != null && WebView.CoreWebView2 != null)
                {
                    if (active)
                        WebViewManager.SetMemoryNormal(WebView);
                    else
                        WebViewManager.SetMemoryLow(WebView);
                }

                if (diagnosticTimer != null)
                {
                    if (active)
                        diagnosticTimer.Start();
                    else
                        diagnosticTimer.Stop();
                }

                SetPageDiagnosticsEnabled(active);
            }
            catch (Exception ex)
            {
                WriteRefreshLog("SetActive exception: " + ex.Message);
            }
        }

        private async void SetPageDiagnosticsEnabled(bool enabled)
        {
            if (isDisposed || WebView == null || WebView.IsDisposed ||
                WebView.CoreWebView2 == null)
                return;

            try
            {
                string value = enabled ? "true" : "false";
                await WebView.ExecuteScriptAsync(
                    "if (window.__lumiDiagSetEnabled) window.__lumiDiagSetEnabled(" + value + ");");
            }
            catch (Exception ex)
            {
                if (!isDisposed)
                    WriteRefreshLog("Set page diagnostic state failed: " + ex.Message);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            TriggerRefresh("Manual button click");
        }

        private void TriggerRefresh(string reason)
        {
            if (isDisposed || WebView == null)
                return;
            if (WebView.CoreWebView2 == null)
            {
                WriteRefreshLog("Refresh rejected (" + reason + "): CoreWebView2 is not ready");
                return;
            }
            if (isRefreshing)
            {
                WriteRefreshLog("Refresh ignored (" + reason + "): another refresh is in progress");
                return;
            }

            try
            {
                isRefreshing = true;
                consecutiveLowFpsCount = 0;
                refreshTimer.Reset();
                refreshTimer.Start();
                btnRefresh.Enabled = false;
                btnRefresh.Text = "กำลังโหลด...";
                refreshTimeoutTimer.Stop();
                refreshTimeoutTimer.Start();
                refreshProgressTimer.Stop();
                refreshProgressTimer.Start();

                WriteRefreshLog("Refresh started [" + reason + "] | URL: "
                    + GetSafeUrl(WebView.Source == null ? null : WebView.Source.ToString())
                    + " | Timeout: " + (RefreshTimeoutMilliseconds / 1000) + " seconds");
                WebView.CoreWebView2.Reload();
            }
            catch (Exception ex)
            {
                WriteRefreshLog("Refresh exception: " + ex);
                FinishRefresh("Refresh command failed");
            }
        }

        private static string GetSafeUrl(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url)) return "Unknown";
                Uri uri = new Uri(url);
                return uri.GetLeftPart(UriPartial.Path);
            }
            catch { return string.IsNullOrEmpty(url) ? "Unknown" : "URL unavailable"; }
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (isDisposed) return;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            navigationTimers[e.NavigationId] = timer;
            WriteRefreshLog("NavigationStarting | NavigationId: " + e.NavigationId
                + " | Redirect: " + e.IsRedirected + " | URL: " + GetSafeUrl(e.Uri)
                + " | RefreshInProgress: " + isRefreshing);
        }

        private void WebView_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (isDisposed) return;
            Stopwatch timer;
            string elapsed = navigationTimers.TryGetValue(e.NavigationId, out timer)
                ? timer.ElapsedMilliseconds + " ms" : "unknown";
            WriteRefreshLog("DOMContentLoaded | NavigationId: " + e.NavigationId
                + " | Since NavigationStarting: " + elapsed
                + " | URL: " + GetSafeUrl(WebView == null || WebView.Source == null
                    ? null : WebView.Source.ToString()));
        }

        private void WebView_WebResourceResponseReceived(object sender,
            CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            if (isDisposed || e == null || e.Response == null) return;
            try
            {
                int statusCode = e.Response.StatusCode;
                if (statusCode >= 400)
                {
                    string requestUrl = e.Request == null ? "Unknown" : GetSafeUrl(e.Request.Uri);
                    WriteRefreshLog("Resource HTTP ERROR | Status: " + statusCode
                        + " | Method: " + (e.Request == null ? "Unknown" : e.Request.Method)
                        + " | URL: " + requestUrl);
                }
            }
            catch (Exception ex) { WriteRefreshLog("Resource diagnostic exception: " + ex.Message); }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (isDisposed) return;
            Stopwatch navTimer;
            string duration = "unknown";
            if (navigationTimers.TryGetValue(e.NavigationId, out navTimer))
            {
                duration = navTimer.ElapsedMilliseconds + " ms";
                navigationTimers.Remove(e.NavigationId);
            }

            WriteRefreshLog("NavigationCompleted | NavigationId: " + e.NavigationId
                + " | Result: " + (e.IsSuccess ? "SUCCESS" : "FAILED")
                + " | WebErrorStatus: " + (e.IsSuccess ? "None" : e.WebErrorStatus.ToString())
                + " | NavigationDuration: " + duration
                + " | URL: " + GetSafeUrl(WebView == null || WebView.Source == null
                    ? null : WebView.Source.ToString()));

            SetPageDiagnosticsEnabled(isActive);

            if (!isRefreshing) return;
            long elapsed = refreshTimer.ElapsedMilliseconds;
            WriteRefreshLog("Refresh completed | Result: " + (e.IsSuccess ? "SUCCESS" : "FAILED")
                + " | Duration: " + elapsed + " ms (" + (elapsed / 1000.0).ToString("F2")
                + " sec) | WebErrorStatus: " + (e.IsSuccess ? "None" : e.WebErrorStatus.ToString())
                + " | URL: " + GetSafeUrl(WebView == null || WebView.Source == null
                    ? null : WebView.Source.ToString()));
            FinishRefresh("Navigation completed");
        }

        private void RefreshProgressTimer_Tick(object sender, EventArgs e)
        {
            if (isDisposed || !isRefreshing) return;
            WriteRefreshLog("Refresh still in progress | Elapsed: "
                + (refreshTimer.ElapsedMilliseconds / 1000.0).ToString("F1")
                + " sec | URL: " + GetSafeUrl(WebView == null || WebView.Source == null
                    ? null : WebView.Source.ToString()));
        }

        private async void DiagnosticTimer_Tick(object sender, EventArgs e)
        {
            if (isDisposed || isCollectingDiagnostic || WebView == null
                || WebView.IsDisposed || WebView.CoreWebView2 == null)
                return;

            isCollectingDiagnostic = true;
            try
            {
                string stats = await WebView.ExecuteScriptAsync(
                    "window.__lumiDiagRead ? window.__lumiDiagRead() : null;");
                if (isDisposed) return;

                WriteRefreshLog("Page diagnostic | Stats: "
                    + (string.IsNullOrEmpty(stats) ? "No data" : stats));
                WriteWebViewProcessStats();

                if (AutoRefreshEnabled && !isRefreshing && !string.IsNullOrEmpty(stats) && stats != "null")
                {
                    EvaluateAutoRefreshCondition(stats);
                }
            }
            catch (Exception ex) { WriteRefreshLog("Page diagnostic failed: " + ex.Message); }
            finally { isCollectingDiagnostic = false; }
        }

        private void EvaluateAutoRefreshCondition(string statsJson)
        {
            try
            {
                Match fpsMatch = Regex.Match(statsJson, "\"fps\":(\\d+)");
                Match heapUsedMatch = Regex.Match(statsJson, "\"jsHeapUsed\":(\\d+)");
                Match heapLimitMatch = Regex.Match(statsJson, "\"jsHeapLimit\":(\\d+)");

                if (fpsMatch.Success && int.TryParse(fpsMatch.Groups[1].Value, out int fps))
                {
                    if (fps > 0 && fps < LowFpsThreshold)
                    {
                        consecutiveLowFpsCount++;
                        WriteRefreshLog($"Low FPS detected ({fps} FPS) | Strike {consecutiveLowFpsCount}/{MaxConsecutiveLowFpsChecks}");

                        if (consecutiveLowFpsCount >= MaxConsecutiveLowFpsChecks)
                        {
                            TriggerRefresh($"Auto-refresh due to low FPS ({fps} FPS for {MaxConsecutiveLowFpsChecks} checks)");
                            return;
                        }
                    }
                    else if (fps >= LowFpsThreshold)
                    {
                        consecutiveLowFpsCount = 0;
                    }
                }

                if (heapUsedMatch.Success && heapLimitMatch.Success &&
                    long.TryParse(heapUsedMatch.Groups[1].Value, out long heapUsed) &&
                    long.TryParse(heapLimitMatch.Groups[1].Value, out long heapLimit) &&
                    heapLimit > 0)
                {
                    double ratio = (double)heapUsed / heapLimit;
                    if (ratio >= MaxHeapUsageRatio)
                    {
                        TriggerRefresh($"Auto-refresh due to high JS Heap ({ratio * 100:F1}%)");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteRefreshLog("EvaluateAutoRefreshCondition error: " + ex.Message);
            }
        }

        private void WriteWebViewProcessStats()
        {
            Process[] processes = null;
            long workingSet = 0;
            TimeSpan cpuTime = TimeSpan.Zero;
            int count = 0;
            try
            {
                processes = Process.GetProcessesByName("msedgewebview2");
                for (int i = 0; i < processes.Length; i++)
                {
                    try
                    {
                        workingSet += processes[i].WorkingSet64;
                        cpuTime += processes[i].TotalProcessorTime;
                        count++;
                    }
                    catch { }
                }

                DateTime now = DateTime.UtcNow;
                string cpu = "warming up";
                if (lastProcessSampleTime != DateTime.MinValue)
                {
                    double seconds = (now - lastProcessSampleTime).TotalSeconds;
                    double cpuSeconds = (cpuTime - lastWebViewCpuTime).TotalSeconds;
                    if (seconds > 0)
                    {
                        double percent = cpuSeconds / (seconds * Math.Max(1, Environment.ProcessorCount)) * 100.0;
                        if (percent < 0) percent = 0;
                        cpu = percent.ToString("F1") + "%";
                    }
                }
                lastProcessSampleTime = now;
                lastWebViewCpuTime = cpuTime;
                WriteRefreshLog("WebView2 process stats (all WebView2 processes on this PC)"
                    + " | Count: " + count + " | WorkingSet: " + (workingSet / (1024 * 1024))
                    + " MB | CPU: " + cpu);
            }
            catch (Exception ex) { WriteRefreshLog("WebView2 process stats failed: " + ex.Message); }
            finally
            {
                if (processes != null)
                    for (int i = 0; i < processes.Length; i++)
                        try { processes[i].Dispose(); } catch { }
            }
        }

        private void RefreshTimeoutTimer_Tick(object sender, EventArgs e)
        {
            if (isDisposed || !isRefreshing) return;
            WriteRefreshLog("Refresh TIMEOUT | Elapsed: " + refreshTimer.ElapsedMilliseconds
                + " ms | URL: " + GetSafeUrl(WebView == null || WebView.Source == null
                    ? null : WebView.Source.ToString()) + " | Navigation may still be running");
            FinishRefresh("Refresh timeout");
        }

        private void FinishRefresh(string reason)
        {
            if (refreshTimeoutTimer != null) refreshTimeoutTimer.Stop();
            if (refreshProgressTimer != null) refreshProgressTimer.Stop();
            if (refreshTimer.IsRunning) refreshTimer.Stop();
            isRefreshing = false;

            if (!isDisposed && btnRefresh != null && !btnRefresh.IsDisposed)
            {
                btnRefresh.Enabled = true;
                btnRefresh.Text = "รีเฟรช";
            }
            WriteRefreshLog("Refresh state reset | " + reason);
        }

        private void BtnMute_Click(object sender, EventArgs e)
        {
            if (isDisposed || WebView == null || WebView.CoreWebView2 == null) return;
            try
            {
                WebView.CoreWebView2.IsMuted = !WebView.CoreWebView2.IsMuted;
                UpdateMuteButton();
                WriteRefreshLog(WebView.CoreWebView2.IsMuted ? "Audio muted" : "Audio unmuted");
            }
            catch (Exception ex) { WriteRefreshLog("Mute exception: " + ex.Message); }
        }

        private void UpdateMuteButton()
        {
            if (btnMute == null || WebView == null || WebView.CoreWebView2 == null) return;
            try { btnMute.Text = WebView.CoreWebView2.IsMuted ? "เปิดเสียง" : "ปิดเสียง"; }
            catch { }
        }

        public CoreWebView2MemoryUsageTargetLevel GetMemoryUsageLevel()
        {
            if (WebView == null) return CoreWebView2MemoryUsageTargetLevel.Low;
            return WebViewManager.GetMemoryLevel(WebView);
        }

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
                        refreshTimeoutTimer.Tick -= RefreshTimeoutTimer_Tick;
                        refreshTimeoutTimer.Dispose();
                        refreshTimeoutTimer = null;
                    }
                }
                catch { }
                try { refreshTimer.Stop(); } catch { }
                try
                {
                    if (refreshProgressTimer != null)
                    {
                        refreshProgressTimer.Stop();
                        refreshProgressTimer.Tick -= RefreshProgressTimer_Tick;
                        refreshProgressTimer.Dispose();
                        refreshProgressTimer = null;
                    }
                    if (diagnosticTimer != null)
                    {
                        diagnosticTimer.Stop();
                        diagnosticTimer.Tick -= DiagnosticTimer_Tick;
                        diagnosticTimer.Dispose();
                        diagnosticTimer = null;
                    }
                }
                catch { }
                try { if (WebView != null) WebViewManager.SetMemoryLow(WebView); } catch { }
                try
                {
                    if (WebView != null)
                    {
                        if (WebView.CoreWebView2 != null)
                        {
                            WebView.CoreWebView2.NavigationStarting -= WebView_NavigationStarting;
                            WebView.CoreWebView2.DOMContentLoaded -= WebView_DOMContentLoaded;
                            WebView.CoreWebView2.NavigationCompleted -= WebView_NavigationCompleted;
                            WebView.CoreWebView2.WebResourceResponseReceived -= WebView_WebResourceResponseReceived;
                            WebView.CoreWebView2.ProcessFailed -= WebView_ProcessFailed;
                        }
                        WebView.Dispose();
                        WebView = null;
                    }
                }
                catch { }
                try
                {
                    if (btnMute != null) btnMute.Click -= BtnMute_Click;
                    if (btnRefresh != null) btnRefresh.Click -= BtnRefresh_Click;
                }
                catch { }
            }
            base.Dispose(disposing);
        }
    }
}