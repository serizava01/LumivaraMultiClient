
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace LumivaraMultiClient.Services
{
    public static class WebViewManager
    {
        // =========================================================
        // PATH
        // =========================================================

        private static readonly string BaseDataPath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "LumivaraMultiClient");

        private static readonly string WebViewDataPath =
            Path.Combine(BaseDataPath, "WebViewData");

        private static readonly string DiagnosticLogPath =
            Path.Combine(BaseDataPath, "RefreshLog.txt");

        // =========================================================
        // SHARED ENVIRONMENT
        // =========================================================

        private static CoreWebView2Environment sharedEnvironment;

        private static readonly object environmentLock =
            new object();

        private static Task<CoreWebView2Environment> environmentTask;

        // =========================================================
        // LOG
        // =========================================================

        private static void WriteLog(string profileId, string message)
        {
            try
            {
                if (!Directory.Exists(BaseDataPath))
                    Directory.CreateDirectory(BaseDataPath);

                File.AppendAllText(
                    DiagnosticLogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                    + " | "
                    + (string.IsNullOrWhiteSpace(profileId)
                        ? "WebViewManager"
                        : profileId)
                    + " | "
                    + message
                    + Environment.NewLine);
            }
            catch
            {
                // Logging must not interrupt the game.
            }
        }

        // =========================================================
        // GET SHARED ENVIRONMENT
        // =========================================================

        private static async Task<CoreWebView2Environment>
            GetEnvironmentAsync()
        {
            Task<CoreWebView2Environment> task;

            lock (environmentLock)
            {
                if (sharedEnvironment != null)
                    return sharedEnvironment;

                if (environmentTask == null)
                {
                    environmentTask = CreateEnvironmentAsync();
                }

                task = environmentTask;
            }

            try
            {
                return await task;
            }
            catch
            {
                // Allow another initialization attempt after failure.
                lock (environmentLock)
                {
                    if (object.ReferenceEquals(environmentTask, task))
                    {
                        environmentTask = null;
                    }
                }

                throw;
            }
        }

        // =========================================================
        // CREATE ENVIRONMENT
        // =========================================================

        private static async Task<CoreWebView2Environment>
            CreateEnvironmentAsync()
        {
            if (!Directory.Exists(WebViewDataPath))
            {
                Directory.CreateDirectory(WebViewDataPath);
            }

            CoreWebView2EnvironmentOptions options =
                new CoreWebView2EnvironmentOptions();

            options.AdditionalBrowserArguments =
                "--disable-features=CalculateNativeWinOcclusion,"
                + "IntensiveWakeUpThrottling";

            CoreWebView2Environment env =
                await CoreWebView2Environment.CreateAsync(
                    null,
                    WebViewDataPath,
                    options);

            lock (environmentLock)
            {
                sharedEnvironment = env;
            }

            return env;
        }

        // =========================================================
        // INITIALIZE INSTANCE
        // =========================================================

        public static async Task InitializeInstanceAsync(
            WebView2 webView,
            string profileId)
        {
            if (webView == null)
                throw new ArgumentNullException("webView");

            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new ArgumentException(
                    "Profile ID is empty.",
                    "profileId");
            }

            WriteLog(profileId, "Environment initialization requested");

            try
            {
                CoreWebView2Environment env =
                    await GetEnvironmentAsync();

                WriteLog(profileId, "Shared environment ready");

                CoreWebView2ControllerOptions controllerOptions =
                    env.CreateCoreWebView2ControllerOptions();

                controllerOptions.ProfileName = profileId;
                controllerOptions.IsInPrivateModeEnabled = false;

                await webView.EnsureCoreWebView2Async(
                    env,
                    controllerOptions);

                if (webView.CoreWebView2 == null)
                {
                    throw new InvalidOperationException(
                        "CoreWebView2 initialization failed.");
                }

                CoreWebView2 core = webView.CoreWebView2;

                // SETTINGS
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.AreDevToolsEnabled = false;

                // VISIBILITY SCRIPT
                // Keep existing game behavior unchanged.
                await core.AddScriptToExecuteOnDocumentCreatedAsync(
                    @"
                    document.addEventListener(
                        'visibilitychange',
                        function(e)
                        {
                            e.stopImmediatePropagation();
                        },
                        true
                    );

                    Object.defineProperty(
                        document,
                        'hidden',
                        {
                            configurable: true,
                            get: function()
                            {
                                return false;
                            }
                        }
                    );

                    Object.defineProperty(
                        document,
                        'visibilityState',
                        {
                            configurable: true,
                            get: function()
                            {
                                return 'visible';
                            }
                        }
                    );
                    ");

                // POINTER LOCK
                // Keep existing behavior unchanged.
                await core.AddScriptToExecuteOnDocumentCreatedAsync(
                    @"
                    Element.prototype.requestPointerLock =
                        function()
                        {
                            console.log('Pointer lock disabled');
                        };

                    document.exitPointerLock =
                        function()
                        {
                            console.log('Pointer lock disabled');
                        };
                    ");

                // DEFAULT MEMORY TARGET
                core.MemoryUsageTargetLevel =
                    CoreWebView2MemoryUsageTargetLevel.Low;

                WriteLog(profileId, "WebView initialization completed");
            }
            catch (Exception ex)
            {
                WriteLog(
                    profileId,
                    "Initialization failed: " + ex);

                throw;
            }
        }

        // =========================================================
        // SET MEMORY NORMAL
        // =========================================================

        public static void SetMemoryNormal(WebView2 webView)
        {
            SetMemoryLevel(
                webView,
                CoreWebView2MemoryUsageTargetLevel.Normal);
        }

        // =========================================================
        // SET MEMORY LOW
        // =========================================================

        public static void SetMemoryLow(WebView2 webView)
        {
            SetMemoryLevel(
                webView,
                CoreWebView2MemoryUsageTargetLevel.Low);
        }

        // =========================================================
        // SET MEMORY LEVEL
        // =========================================================

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
                // WebView2 may already be shutting down.
            }
        }

        // =========================================================
        // GET MEMORY LEVEL
        // =========================================================

        public static CoreWebView2MemoryUsageTargetLevel GetMemoryLevel(
            WebView2 webView)
        {
            if (webView == null || webView.IsDisposed)
            {
                return CoreWebView2MemoryUsageTargetLevel.Low;
            }

            try
            {
                if (webView.CoreWebView2 == null)
                {
                    return CoreWebView2MemoryUsageTargetLevel.Low;
                }

                return webView.CoreWebView2.MemoryUsageTargetLevel;
            }
            catch
            {
                return CoreWebView2MemoryUsageTargetLevel.Low;
            }
        }

        // =========================================================
        // SHARED ENVIRONMENT
        // =========================================================

        public static CoreWebView2Environment SharedEnvironment
        {
            get
            {
                lock (environmentLock)
                {
                    return sharedEnvironment;
                }
            }
        }

        // =========================================================
        // DATA PATH
        // =========================================================

        public static string DataPath
        {
            get { return WebViewDataPath; }
        }

        // =========================================================
        // PROFILE PATH
        // =========================================================

        public static string GetProfilePath(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                return null;

            return Path.Combine(WebViewDataPath, profileId);
        }
    }
}
