using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp_MauiApp.McpTools
{
    [McpServerToolType]
    public sealed class Chrome_Tool
    {
        [McpServerTool, Description("打开网站")]
        public static string OpenUrl(string url)
        {
            return OpenUrlInBrowser(url);
        }

        public static string OpenUrlInBrowser(string url)
        {
            try
            {
                // 如果URL为空，使用默认主页
                if (string.IsNullOrEmpty(url))
                    url = "https://www.google.com";

                // 使用MAUI的浏览器启动器
                Microsoft.Maui.ApplicationModel.Browser.OpenAsync(new Uri(url), Microsoft.Maui.ApplicationModel.BrowserLaunchMode.SystemPreferred);
                return "网站打开成功";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开浏览器时出错: {ex.Message}");
                return "网站打开失败";
            }
        }
    }
} 