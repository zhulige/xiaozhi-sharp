using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp_ConsoleApp.McpTools
{
    [McpServerToolType]
    public sealed class Chrome_Tool
    {
        [McpServerTool, Description("打开网站")]
        public static string OpenUrl(string url)
        {
            return OpenUrlInChrome(url);
        }

        public static string OpenUrlInChrome(string url)
        {
            try
            {
                // 如果URL为空，使用默认主页
                if (string.IsNullOrEmpty(url))
                    url = "https://www.google.com";

                // 在Windows上，使用Process.Start()直接打开URL
                // 系统会自动选择默认浏览器
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return "网站打开成功";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开浏览器时出错: {ex.Message}");

                // 如果上述方法失败，尝试直接启动Chrome
                //TryOpenChromeDirectly(url);
                return "网站打开失败";
            }
        }
    }
}
