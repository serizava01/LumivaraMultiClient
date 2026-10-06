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
                "LumivaraMultiClient"
            );

        private static readonly string WebViewDataPath =
            Path.Combine(
                BaseDataPath,
                "WebViewData"
            );

        // =========================================================
        // SHARED ENVIRONMENT
        // =========================================================

        private static CoreWebView2Environment sharedEnvironment;

        private static readonly object environmentLock =
            new object();

        private static Task<CoreWebView2Environment> environmentTask;

        // =========================================================
        // INITIALIZE SHARED ENVIRONMENT
        // =========================================================

        private static Task<CoreWebView2Environment>
            GetEnvironmentAsync()
        {
            lock (environmentLock)
            {
                if (sharedEnvironment != null)
                {
                    return Task.FromResult(
                        sharedEnvironment
                    );
                }

                if (environmentTask != null)
                {
                    return environmentTask;
                }

                environmentTask =
                    CreateEnvironmentAsync();

                return environmentTask;
            }
        }

        // =========================================================
        // CREATE ENVIRONMENT
        // =========================================================

        private static async Task<CoreWebView2Environment>
            CreateEnvironmentAsync()
        {
            if (!Directory.Exists(
                WebViewDataPath))
            {
                Directory.CreateDirectory(
                    WebViewDataPath
                );
            }

            CoreWebView2EnvironmentOptions options =
                new CoreWebView2EnvironmentOptions();

            options.AdditionalBrowserArguments =
                @"--disable-features=CalculateNativeWinOcclusion,IntensiveWakeUpThrottling " +
                @"--js-flags=""--max-old-space-size=512""";

            CoreWebView2Environment env =
                await CoreWebView2Environment.CreateAsync(
                    null,
                    WebViewDataPath,
                    options
                );

            lock (environmentLock)
            {
                sharedEnvironment =
                    env;
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
                throw new ArgumentNullException(
                    "webView"
                );

            if (string.IsNullOrWhiteSpace(
                profileId))
            {
                throw new ArgumentException(
                    "Profile ID is empty.",
                    "profileId"
                );
            }

            // =====================================================
            // GET SHARED ENVIRONMENT
            // =====================================================

            CoreWebView2Environment env =
                await GetEnvironmentAsync();

            // =====================================================
            // CREATE PROFILE OPTIONS
            // =====================================================

            CoreWebView2ControllerOptions controllerOptions =
                env.CreateCoreWebView2ControllerOptions();

            controllerOptions.ProfileName =
                profileId;

            controllerOptions.IsInPrivateModeEnabled =
                false;

            // =====================================================
            // INITIALIZE WEBVIEW
            // =====================================================

            await webView.EnsureCoreWebView2Async(
                env,
                controllerOptions
            );

            if (webView.CoreWebView2 == null)
            {
                throw new Exception(
                    "CoreWebView2 initialization failed."
                );
            }

            // =====================================================
            // SETTINGS
            // =====================================================

            webView.CoreWebView2.Settings.IsStatusBarEnabled =
                false;

            webView.CoreWebView2.Settings.AreDevToolsEnabled =
                false;

            // =====================================================
            // VISIBILITY SCRIPT
            // =====================================================

            await webView.CoreWebView2
                .AddScriptToExecuteOnDocumentCreatedAsync(
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
                            get: function()
                            {
                                return 'visible';
                            }
                        }
                    );
                    "
                );

            // =====================================================
            // POINTER LOCK
            // =====================================================

            await webView.CoreWebView2
                .AddScriptToExecuteOnDocumentCreatedAsync(
                    @"
                    Element.prototype.requestPointerLock =
                        function()
                        {
                            console.log(
                                'Pointer lock disabled'
                            );
                        };

                    document.exitPointerLock =
                        function()
                        {
                            console.log(
                                'Pointer lock disabled'
                            );
                        };
                    "
                );

            // =====================================================
            // DEFAULT MEMORY LEVEL
            // =====================================================

            webView.CoreWebView2.MemoryUsageTargetLevel =
                CoreWebView2MemoryUsageTargetLevel.Low;
        }

        // =========================================================
        // SET MEMORY NORMAL
        // =========================================================

        public static void SetMemoryNormal(
            WebView2 webView)
        {
            if (webView == null)
                return;

            try
            {
                if (webView.CoreWebView2 == null)
                    return;

                webView.CoreWebView2
                    .MemoryUsageTargetLevel =
                    CoreWebView2MemoryUsageTargetLevel.Normal;
            }
            catch
            {
            }
        }

        // =========================================================
        // SET MEMORY LOW
        // =========================================================

        public static void SetMemoryLow(
            WebView2 webView)
        {
            if (webView == null)
                return;

            try
            {
                if (webView.CoreWebView2 == null)
                    return;

                webView.CoreWebView2
                    .MemoryUsageTargetLevel =
                    CoreWebView2MemoryUsageTargetLevel.Low;
            }
            catch
            {
            }
        }

        // =========================================================
        // GET CURRENT MEMORY LEVEL
        // =========================================================

        public static CoreWebView2MemoryUsageTargetLevel
            GetMemoryLevel(
                WebView2 webView)
        {
            if (webView == null)
            {
                return CoreWebView2MemoryUsageTargetLevel.Low;
            }

            try
            {
                if (webView.CoreWebView2 == null)
                {
                    return CoreWebView2MemoryUsageTargetLevel.Low;
                }

                return webView.CoreWebView2
                    .MemoryUsageTargetLevel;
            }
            catch
            {
                return CoreWebView2MemoryUsageTargetLevel.Low;
            }
        }

        // =========================================================
        // SHARED ENVIRONMENT
        // =========================================================

        public static CoreWebView2Environment
            SharedEnvironment
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
            get
            {
                return WebViewDataPath;
            }
        }

        // =========================================================
        // PROFILE PATH
        // =========================================================

        public static string GetProfilePath(
            string profileId)
        {
            if (string.IsNullOrWhiteSpace(
                profileId))
            {
                return null;
            }

            return Path.Combine(
                WebViewDataPath,
                profileId
            );
        }
    }
}