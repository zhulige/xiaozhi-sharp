using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XiaoZhiSharp_BlazorApp
{
    public class AppHandle
    {
        public static Process WebProcess { get; set; }
        public static void OpenWebView()
        {
            var url = "http://localhost:5050";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                WebProcess = Process.Start("google-chrome", "--kiosk " + url + " --disable-background-networking");
                //Process.Start("xdg-open", url).Dispose();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url).Dispose();
            }
            else
            {
                string chrome = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
                if (System.IO.File.Exists(chrome))
                {
                    //WebProcess = Process.Start(chrome, "--kiosk " + url + " --disable-background-networking");
                    WebProcess = Process.Start(chrome, "--kiosk " + url);
                }
                else
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }).Dispose();
                }
            }
        }
    }
}
