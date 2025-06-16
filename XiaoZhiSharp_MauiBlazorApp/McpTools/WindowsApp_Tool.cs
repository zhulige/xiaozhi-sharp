using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp_MauiBlazorApp.McpTools
{
    [McpServerToolType]
    public sealed class WindowsApp_Tool
    {
        [McpServerTool, Description("打开记事本")]
        public static string OpenNotepad()
        {
            return OpenWindowsApp("记事本");
        }

        public static string OpenWindowsApp(string name)
        {
            try
            {
                // 只在Windows平台运行
                if (DeviceInfo.Platform != DevicePlatform.WinUI)
                {
                    return "非Windows平台，无法运行应用";
                }

                switch(name.ToLower())
                {
                    case "资源管理器":
                        name = "explorer.exe"; // 打开文件资源管理器
                        break;
                    case "记事本":
                        name = "notepad.exe"; // 打开记事本
                        break;
                    case "计算器":
                        name = "calc.exe"; // 打开计算器
                        break;
                    case "命令提示符":
                        name = "cmd.exe"; // 打开命令提示符
                        break;
                    case "powershell":
                        name = "powershell.exe"; // 打开 PowerShell
                        break;
                    default:
                        // 如果是其他应用程序，直接使用名称
                        break;
                }
                Process.Start(name);
                return "应用打开成功";
            }
            catch (Exception ex)
            {
                return "应用打开失败: " + ex.Message;
            }
        }
    }
} 