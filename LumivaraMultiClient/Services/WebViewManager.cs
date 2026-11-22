using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace LumivaraMultiClient.Services
{
    public static class WebViewManager
    {
        // =========================================================
        // PATH (เก็บไว้ในโฟลเดอร์ Data ข้างไฟล์โปรแกรม)
        // =========================================================

        public static readonly string BaseDataPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        private static readonly string WebViewDataPath =
            Path.Combine(BaseDataPath, "WebViewData");

        private static readonly string DiagnosticLogPath =
            Path.Combine(BaseDataPath, "GameengineLog.txt");

        private static readonly string DiagnosticLogBackupPath =
            DiagnosticLogPath + ".old";

        // เก็บไฟล์ปัจจุบันประมาณ 2 MB และสำรองไว้เพียง 1 ไฟล์
        private const long MaxDiagnosticLogBytes = 2 * 1024 * 1024;

        private static readonly object logLock = new object();

        public static bool DiagnosticLoggingEnabled { get; set; } = true;

        // =========================================================
        // SHARED DIAGNOSTIC LOG
        // =========================================================

        public static void WriteDiagnosticLog(string profileId, string message)
        {
            if (!DiagnosticLoggingEnabled)
                return;

            try
            {
                if (!Directory.Exists(BaseDataPath))
                    Directory.CreateDirectory(BaseDataPath);

                string safeProfileId = string.IsNullOrWhiteSpace(profileId)
                    ? "WebViewManager"
                    : profileId;

                string line =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                    + " | " + safeProfileId
                    + " | " + (message ?? string.Empty)
                    + Environment.NewLine;

                byte[] bytes = Encoding.UTF8.GetBytes(line);

                lock (logLock)
                {
                    RotateLogIfNeeded(bytes.Length);

                    using (FileStream stream = new FileStream(
                        DiagnosticLogPath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.Read))
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            catch
            {

            }
        }

        private static void RotateLogIfNeeded(int incomingBytes)
        {
            try
            {
                if (!File.Exists(DiagnosticLogPath))
                    return;

                FileInfo info = new FileInfo(DiagnosticLogPath);

                if (info.Length + incomingBytes <= MaxDiagnosticLogBytes)
                    return;

                if (File.Exists(DiagnosticLogBackupPath))
                    File.Delete(DiagnosticLogBackupPath);

                File.Move(DiagnosticLogPath, DiagnosticLogBackupPath);
            }
            catch
            {
            }
        }

        private static void WriteLog(string profileId, string message)
        {
            WriteDiagnosticLog(profileId, message);
        }

        // =========================================================
        // INITIALIZE INSTANCE (แยก Environment อิสระต่อ 1 จอ)
        // =========================================================

        public static async Task InitializeInstanceAsync(
            WebView2 webView,
            string profileId)
        {
            if (webView == null)
                throw new ArgumentNullException("webView");

            if (string.IsNullOrWhiteSpace(profileId))
                throw new ArgumentException("Profile ID is empty.", "profileId");

            WriteLog(profileId, "Isolated environment initialization requested");

            try
            {
                string userFolder = GetProfilePath(profileId);
                if (!Directory.Exists(userFolder))
                    Directory.CreateDirectory(userFolder);

                CoreWebView2EnvironmentOptions options =
                    new CoreWebView2EnvironmentOptions();

                options.AdditionalBrowserArguments =
                    "--disable-features=CalculateNativeWinOcclusion,IntensiveWakeUpThrottling "
                    + "--disable-background-timer-throttling "
                    + "--disable-renderer-backgrounding "
                    + "--js-flags=\"--max-old-space-size=4096\"";

                CoreWebView2Environment env =
                    await CoreWebView2Environment.CreateAsync(
                        null,
                        userFolder,
                        options);

                await webView.EnsureCoreWebView2Async(env);

                if (webView.CoreWebView2 == null)
                {
                    throw new InvalidOperationException(
                        "CoreWebView2 initialization failed.");
                }

                CoreWebView2 core = webView.CoreWebView2;

                core.Settings.IsStatusBarEnabled = false;
                core.Settings.AreDevToolsEnabled = false;

                await core.AddScriptToExecuteOnDocumentCreatedAsync(
                    @"
                    document.addEventListener(
                        'visibilitychange',
                        function(e) { e.stopImmediatePropagation(); },
                        true
                    );

                    Object.defineProperty(document, 'hidden', {
                        configurable: true,
                        get: function() { return false; }
                    });

                    Object.defineProperty(document, 'visibilityState', {
                        configurable: true,
                        get: function() { return 'visible'; }
                    });
                    ");

                await core.AddScriptToExecuteOnDocumentCreatedAsync(
                    @"
                    Element.prototype.requestPointerLock = function() {
                        console.log('Pointer lock disabled');
                    };

                    document.exitPointerLock = function() {
                        console.log('Pointer lock disabled');
                    };
                    ");

                core.MemoryUsageTargetLevel =
                    CoreWebView2MemoryUsageTargetLevel.Normal;

                WriteLog(profileId, "WebView initialization completed");
            }
            catch (Exception ex)
            {
                WriteLog(profileId, "Initialization failed: " + ex);
                throw;
            }
        }

        // =========================================================
        // SET MEMORY LEVEL
        // =========================================================

        public static void SetMemoryNormal(WebView2 webView)
        {
            SetMemoryLevel(webView, CoreWebView2MemoryUsageTargetLevel.Normal);
        }

        public static void SetMemoryLow(WebView2 webView)
        {
            SetMemoryLevel(webView, CoreWebView2MemoryUsageTargetLevel.Low);
        }

        private static void SetMemoryLevel(
            WebView2 webView,
            CoreWebView2MemoryUsageTargetLevel level)
        {
            if (webView == null || webView.IsDisposed)
                return;

            try
            {
                if (webView.CoreWebView2 == null)
                    return;

                webView.CoreWebView2.MemoryUsageTargetLevel = level;
            }
            catch
            {
            }
        }

        public static CoreWebView2MemoryUsageTargetLevel GetMemoryLevel(
            WebView2 webView)
        {
            if (webView == null || webView.IsDisposed)
                return CoreWebView2MemoryUsageTargetLevel.Low;

            try
            {
                if (webView.CoreWebView2 == null)
                    return CoreWebView2MemoryUsageTargetLevel.Low;

                return webView.CoreWebView2.MemoryUsageTargetLevel;
            }
            catch
            {
                return CoreWebView2MemoryUsageTargetLevel.Low;
            }
        }

        // =========================================================
        // PATHS
        // =========================================================

        public static string DataPath
        {
            get { return WebViewDataPath; }
        }

        public static string GetProfilePath(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                return null;

            return Path.Combine(WebViewDataPath, profileId);
        }
    }
}