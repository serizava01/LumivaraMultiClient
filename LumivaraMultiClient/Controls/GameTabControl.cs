using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
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
        public event Action<string> ToggleMaximizeRequested;
        private Panel topBar;
        private Button btnMute;
        private Button btnRefresh;
        private CheckBox chkAutoRefresh;
        private Label lblTitle;

        private bool isInitializing;
        private bool isDisposed;
        private bool isRefreshing;
        private bool isCollectingDiagnostic;
        private bool isActive = true;

        private int consecutiveLowFpsCount = 0;
        private const int LowFpsThreshold = 10;
        private const int MaxConsecutiveLowFpsChecks = 3;
        private const double MaxHeapUsageRatio = 0.85;

        private readonly Stopwatch refreshTimer = new Stopwatch();
        private readonly Dictionary<ulong, Stopwatch> navigationTimers = new Dictionary<ulong, Stopwatch>();

        private Timer refreshProgressTimer;
        private Timer diagnosticTimer;
        private Timer refreshTimeoutTimer;

        private DateTime lastProcessSampleTime = DateTime.MinValue;
        private TimeSpan lastWebViewCpuTime = TimeSpan.Zero;

        private const int RefreshProgressIntervalMilliseconds = 15000;
        private const int DiagnosticIntervalMilliseconds = 30000;
        private const int RefreshTimeoutMilliseconds = 180000;

        public event Action<string, string> UrlChanged;

        public bool AutoRefreshEnabled
        {
            get { return chkAutoRefresh != null && chkAutoRefresh.Checked; }
            set { if (chkAutoRefresh != null) chkAutoRefresh.Checked = value; }
        }

        public GameTabControl(AccountProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            Profile = profile;
            InitializeComponents();
            _ = InitGameAsync();
        }

        public void SetActive(bool active)
        {
            if (isDisposed) return;
            isActive = active;
            consecutiveLowFpsCount = 0;
            try
            {
                if (WebView != null && WebView.CoreWebView2 != null)
                {
                    if (active) WebViewManager.SetMemoryNormal(WebView);
                    else WebViewManager.SetMemoryLow(WebView);
                }
                if (diagnosticTimer != null) { if (active) diagnosticTimer.Start(); else diagnosticTimer.Stop(); }
            }
            catch { }
        }
        public void NavigateTo(string url)
        {
            if (isDisposed || string.IsNullOrWhiteSpace(url)) return;

            // อัปเดต URL ใน Profile เผื่อไว้ใช้งานต่อ
            Profile.TargetUrl = url;

            // ถ้า WebView พร้อมใช้งานแล้ว ให้สั่งเปลี่ยนหน้าเว็บทันที
            if (WebView != null && WebView.CoreWebView2 != null)
            {
                try
                {
                    Uri uri;
                    if (Uri.TryCreate(url, UriKind.Absolute, out uri))
                    {
                        WebView.Source = uri;
                    }
                }
                catch { }
            }
        }
        public bool IsMuted
        {
            get
            {
                if (WebView != null && WebView.CoreWebView2 != null)
                    return WebView.CoreWebView2.IsMuted;
                return false;
            }
            set
            {
                if (WebView != null && WebView.CoreWebView2 != null)
                {
                    try
                    {
                        WebView.CoreWebView2.IsMuted = value;
                        UpdateMuteButton();
                    }
                    catch { }
                }
            }
        }
        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(30, 30, 30);

            topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(50, 50, 50),
                Padding = new Padding(5, 0, 5, 0)
            };

            lblTitle = new Label
            {
                Text = Profile.Title,
                Dock = DockStyle.Left,
                Width = 150,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true
            };

    
            btnRefresh = new Button { Text = "⟳", Width = 30, Height = 24, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += BtnRefresh_Click;

            btnMute = new Button { Text = "🔊", Width = 30, Height = 24, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnMute.FlatAppearance.BorderSize = 0;
            btnMute.Click += BtnMute_Click;

            chkAutoRefresh = new CheckBox
            {
                Text = "Auto",
                Width = 50,
                Height = 24,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.LightGreen,
                Cursor = Cursors.Hand
            };
            chkAutoRefresh.CheckedChanged += (s, e) => { consecutiveLowFpsCount = 0; };


            // เพิ่มปุ่มขยาย (ก่อนปุ่ม Close)
            Button btnMaximize = new Button { Text = "⛶", Width = 30, Height = 24, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.Click += (s, e) =>
            {
                if (ToggleMaximizeRequested != null) ToggleMaximizeRequested(Profile.ProfileId);
            };

            Button btnClose = new Button { Text = "✕", Width = 30, Height = 24, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) =>
            {
                if (CloseRequested != null) CloseRequested(Profile.ProfileId);
            };

            // ปรับลำดับการ Add (ขวาสุดคือ Close, ถัดมาคือ Maximize)
            topBar.Controls.Add(lblTitle);
            topBar.Controls.Add(btnClose);
            topBar.Controls.Add(btnMaximize);
            topBar.Controls.Add(btnRefresh);
            topBar.Controls.Add(btnMute);
            topBar.Controls.Add(chkAutoRefresh);

            WebView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(WebView);
            Controls.Add(topBar);

            refreshTimeoutTimer = new Timer { Interval = RefreshTimeoutMilliseconds };
            refreshTimeoutTimer.Tick += RefreshTimeoutTimer_Tick;
            refreshProgressTimer = new Timer { Interval = RefreshProgressIntervalMilliseconds };
            refreshProgressTimer.Tick += RefreshProgressTimer_Tick;
            diagnosticTimer = new Timer { Interval = DiagnosticIntervalMilliseconds };
            diagnosticTimer.Tick += DiagnosticTimer_Tick;
        }

        public event Action<string> CloseRequested;
        private void OnCloseRequested()
        {
            if (CloseRequested != null) CloseRequested(Profile.ProfileId);
        }

        private async Task InitGameAsync()
        {
            if (isDisposed || WebView == null || isInitializing) return;
            isInitializing = true;
            try
            {
                await WebViewManager.InitializeInstanceAsync(WebView, Profile);

                if (isDisposed || WebView == null || WebView.CoreWebView2 == null) return;

                CoreWebView2 core = WebView.CoreWebView2;
                core.NavigationStarting -= WebView_NavigationStarting; core.NavigationStarting += WebView_NavigationStarting;
                core.NavigationCompleted -= WebView_NavigationCompleted; core.NavigationCompleted += WebView_NavigationCompleted;
                core.ProcessFailed -= WebView_ProcessFailed; core.ProcessFailed += WebView_ProcessFailed;
                core.SourceChanged -= Core_SourceChanged; core.SourceChanged += Core_SourceChanged;

                await core.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    (function () {
                        if (window.__lumiDiagInstalled) return;
                        window.__lumiDiagInstalled = true;
                        var frames = 0; var lastMeasure = performance.now(); var currentFps = 0; var enabled = true; var rafRunning = false;
                        function countFrame(now) {
                            rafRunning = false;
                            if (!enabled) { window.__lumiDiagFps = 0; return; }
                            frames++;
                            var elapsed = now - lastMeasure;
                            if (elapsed >= 1000) { currentFps = Math.round(frames * 1000 / elapsed); frames = 0; lastMeasure = now; }
                            window.__lumiDiagFps = currentFps; rafRunning = true; requestAnimationFrame(countFrame);
                        }
                        window.__lumiDiagSetEnabled = function (value) {
                            enabled = !!value;
                            if (!enabled) { currentFps = 0; window.__lumiDiagFps = 0; return; }
                            if (!rafRunning) { frames = 0; lastMeasure = performance.now(); rafRunning = true; requestAnimationFrame(countFrame); }
                        };
                        window.__lumiDiagRead = function () {
                            var heapUsed = null, heapLimit = null;
                            try { if (performance.memory) { heapUsed = performance.memory.usedJSHeapSize; heapLimit = performance.memory.jsHeapSizeLimit; }} catch (e) {}
                            return { fps: window.__lumiDiagFps || 0, fpsSource: 'requestAnimationFrame estimate', visibilityState: document.visibilityState, readyState: document.readyState, domNodes: document.getElementsByTagName('*').length, resourceCount: performance.getEntriesByType('resource').length, jsHeapUsed: heapUsed, jsHeapLimit: heapLimit };
                        };
                        rafRunning = true; requestAnimationFrame(countFrame);
                    })();");

                if (diagnosticTimer != null && isActive) diagnosticTimer.Start();

                if (!string.IsNullOrWhiteSpace(Profile.TargetUrl))
                {
                    WebView.Source = new Uri(Profile.TargetUrl);
                }

                UpdateMuteButton();
            }
            catch (Exception ex)
            {
                if (!isDisposed) MessageBox.Show("ไม่สามารถเปิด Client ได้\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { isInitializing = false; }
        }

        private void Core_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (isDisposed) return;
            try
            {
                string newUrl = WebView.Source != null ? WebView.Source.ToString() : "";
                if (UrlChanged != null) UrlChanged(Profile.ProfileId, newUrl);
            }
            catch { }
        }

        private async void WebView_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            if (isDisposed) return;
            if (!AutoRefreshEnabled) { if (!isDisposed && btnRefresh != null && !btnRefresh.IsDisposed) btnRefresh.Text = "!"; return; }
            await Task.Delay(2000);
            if (!isDisposed && WebView != null && WebView.CoreWebView2 != null)
                try { TriggerRefresh("ProcessFailed auto-recovery"); } catch { }
        }

        private void BtnRefresh_Click(object sender, EventArgs e) { TriggerRefresh("Manual button click"); }

        private void TriggerRefresh(string reason)
        {
            if (isDisposed || WebView == null || WebView.CoreWebView2 == null || isRefreshing) return;
            try
            {
                isRefreshing = true; consecutiveLowFpsCount = 0; refreshTimer.Reset(); refreshTimer.Start();
                btnRefresh.Enabled = false; btnRefresh.Text = "...";
                refreshTimeoutTimer.Stop(); refreshTimeoutTimer.Start(); refreshProgressTimer.Stop(); refreshProgressTimer.Start();
                WebView.CoreWebView2.Reload();
            }
            catch { FinishRefresh(); }
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e) { if (!isDisposed) navigationTimers[e.NavigationId] = Stopwatch.StartNew(); }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (isDisposed) return;
            Stopwatch navTimer; if (navigationTimers.TryGetValue(e.NavigationId, out navTimer)) navigationTimers.Remove(e.NavigationId);
            if (!isRefreshing) return;
            FinishRefresh();
        }

        private void RefreshProgressTimer_Tick(object sender, EventArgs e) { }
        private async void DiagnosticTimer_Tick(object sender, EventArgs e)
        {
            if (isDisposed || isCollectingDiagnostic || WebView == null || WebView.IsDisposed || WebView.CoreWebView2 == null) return;
            isCollectingDiagnostic = true;
            try
            {
                string stats = await WebView.ExecuteScriptAsync("window.__lumiDiagRead ? window.__lumiDiagRead() : null;");
                if (isDisposed) return;
                if (AutoRefreshEnabled && !isRefreshing && !string.IsNullOrEmpty(stats) && stats != "null") EvaluateAutoRefreshCondition(stats);
            }
            catch { }
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
                        if (consecutiveLowFpsCount >= MaxConsecutiveLowFpsChecks) TriggerRefresh("Auto-refresh (low FPS)");
                    }
                    else if (fps >= LowFpsThreshold) consecutiveLowFpsCount = 0;
                }
                if (heapUsedMatch.Success && heapLimitMatch.Success && long.TryParse(heapUsedMatch.Groups[1].Value, out long heapUsed) && long.TryParse(heapLimitMatch.Groups[1].Value, out long heapLimit) && heapLimit > 0)
                {
                    if ((double)heapUsed / heapLimit >= MaxHeapUsageRatio) TriggerRefresh("Auto-refresh (high heap)");
                }
            }
            catch { }
        }

        private void RefreshTimeoutTimer_Tick(object sender, EventArgs e) { if (isDisposed || !isRefreshing) return; FinishRefresh(); }

        private void FinishRefresh()
        {
            if (refreshTimeoutTimer != null) refreshTimeoutTimer.Stop();
            if (refreshProgressTimer != null) refreshProgressTimer.Stop();
            if (refreshTimer.IsRunning) refreshTimer.Stop();
            isRefreshing = false;
            if (!isDisposed && btnRefresh != null && !btnRefresh.IsDisposed) { btnRefresh.Enabled = true; btnRefresh.Text = "⟳"; }
        }

        private void BtnMute_Click(object sender, EventArgs e)
        {
            if (isDisposed || WebView == null || WebView.CoreWebView2 == null) return;
            try { WebView.CoreWebView2.IsMuted = !WebView.CoreWebView2.IsMuted; UpdateMuteButton(); } catch { }
        }

        private void UpdateMuteButton()
        {
            if (btnMute == null || WebView == null || WebView.CoreWebView2 == null) return;
            try { btnMute.Text = WebView.CoreWebView2.IsMuted ? "🔇" : "🔊"; } catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed) { base.Dispose(disposing); return; }
            isDisposed = true;

            if (disposing)
            {
                try { if (refreshTimeoutTimer != null) { refreshTimeoutTimer.Stop(); refreshTimeoutTimer.Tick -= RefreshTimeoutTimer_Tick; refreshTimeoutTimer.Dispose(); refreshTimeoutTimer = null; } } catch { }
                try { refreshTimer.Stop(); } catch { }
                try { if (refreshProgressTimer != null) { refreshProgressTimer.Stop(); refreshProgressTimer.Tick -= RefreshProgressTimer_Tick; refreshProgressTimer.Dispose(); refreshProgressTimer = null; } } catch { }
                try { if (diagnosticTimer != null) { diagnosticTimer.Stop(); diagnosticTimer.Tick -= DiagnosticTimer_Tick; diagnosticTimer.Dispose(); diagnosticTimer = null; } } catch { }
                try { if (WebView != null) WebViewManager.SetMemoryLow(WebView); } catch { }
                try
                {
                    if (WebView != null)
                    {
                        if (WebView.CoreWebView2 != null)
                        {
                            WebView.CoreWebView2.NavigationStarting -= WebView_NavigationStarting;
                            WebView.CoreWebView2.NavigationCompleted -= WebView_NavigationCompleted;
                            WebView.CoreWebView2.ProcessFailed -= WebView_ProcessFailed;
                            WebView.CoreWebView2.SourceChanged -= Core_SourceChanged;
                        }
                        WebView.Dispose(); WebView = null;
                    }
                }
                catch { }
                try { if (btnMute != null) btnMute.Click -= BtnMute_Click; if (btnRefresh != null) btnRefresh.Click -= BtnRefresh_Click; } catch { }
            }
            base.Dispose(disposing);
        }
    }
}