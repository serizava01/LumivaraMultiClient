using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace LumivaraMultiClient.Services
{
    public class WebViewManager
    {
        private static readonly string BaseDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LumivaraMultiClient",
            "Profiles"
        );
        /// <summary>
        /// ตัวจัดการการเริ่มต้น Instance ของ WebView2
        /// </summary>
        /// <param name="webView"></param>
        /// <param name="profileId"></param>
        /// <returns></returns>
        public static async Task InitializeInstanceAsync(WebView2 webView, string profileId)
        {
            string profilePath = Path.Combine(BaseDataPath, profileId);
            var options = new CoreWebView2EnvironmentOptions();
            options.AdditionalBrowserArguments =
                @"--disable-background-timer-throttling " +
                @"--disable-backgrounding-occluded-windows " +
                @"--disable-renderer-backgrounding " +
                @"--disable-features=CalculateNativeWinOcclusion,IntensiveWakeUpThrottling " +
                @"--js-flags=""--max-old-space-size=1024""";

            var env = await CoreWebView2Environment.CreateAsync(null, profilePath, options);
            await webView.EnsureCoreWebView2Async(env);
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                document.addEventListener('visibilitychange', function(e) {
                    e.stopImmediatePropagation();
                }, true);
    
                Object.defineProperty(document, 'hidden', {get: function() { return false; }});
                Object.defineProperty(document, 'visibilityState', {get: function() { return 'visible'; }});"
            );
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"Element.prototype.requestPointerLock = function()
                {console.log('Pointer lock disabled');};document.exitPointerLock = function(){console.log('Pointer lock disabled');};");
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        }

    }
}