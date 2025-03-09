using System.Net.NetworkInformation;
using XiaoZhiSharp;
using XiaoZhiSharp.Protocols;

class Program
{
    private static XiaoZhiAgent? _xiaoZhiAgent;
    private static bool _status = false;
    static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Title = "小智AI 调试助手";
        // 定义默认值
        string OTA_VERSION_URL = "https://api.tenclass.net/xiaozhi/ota/";
        string WEB_SOCKET_URL = "wss://api.tenclass.net/xiaozhi/v1/";
        string MAC_ADDR = "";
        string logoAndCopyright = @"
========================================================================
欢迎使用“小智AI 服务器调试控制台” ！版本 v1.0.1

当前功能：
1. 语音消息 输入回车：开始录音；再次输入回车：结束录音
2. 文字消息 可以随意输入文字对话
3. 全量往返协议输出，方便调试
        
要是你在使用中有啥想法或者遇到问题，别犹豫，找我们哟：
微信：Zhu-Lige       电子邮箱：ZhuLige@qq.com
有任何动态请大家关注 https://github.com/zhulige/xiaozhi-sharp
========================================================================";
        Console.WriteLine(logoAndCopyright);
        Console.WriteLine("启动：XinZhiSharp_Test.exe <OTA_VERSION_URL> <WEB_SOCKET_URL> <MAC_ADDR>");
        Console.WriteLine("默认OTA_VERSION_URL：" + OTA_VERSION_URL);
        Console.WriteLine("默认WEB_SOCKET_URL：" + WEB_SOCKET_URL);
        Console.WriteLine("========================================================================");

        // 检查是否有传入参数，如果有则覆盖默认值
        if (args.Length >= 1)
        {
            OTA_VERSION_URL = args[0];
        }
        if (args.Length >= 2)
        {
            WEB_SOCKET_URL = args[1];
        }
        if (args.Length >= 3)
        {
            MAC_ADDR = args[2];
        }
        _xiaoZhiAgent = new XiaoZhiAgent(OTA_VERSION_URL, WEB_SOCKET_URL, MAC_ADDR);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("当前 OTA_VERSION_URL：" + _xiaoZhiAgent.OTA_VERSION_URL);
        Console.WriteLine("当前 WEB_SOCKET_URL：" + _xiaoZhiAgent.WEB_SOCKET_URL);
        Console.WriteLine("当前 MAC_ADDR：" + _xiaoZhiAgent.MAC_ADDR);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("========================================================================");
        _xiaoZhiAgent.Start();
        await _xiaoZhiAgent.Send_Listen_Detect("你好");
        while (true)
        {
            //bool isCapsLockOn = Console.CapsLock;
            ////Console.WriteLine($"当前 Caps Lock 状态: {(isCapsLockOn ? "开启" : "关闭")}");
            //if (isCapsLockOn)
            //{
            //    if (_status == false)
            //    {
            //        _status = true;
            //        await _xiaoZhiAgent.Send_Listen_Start("auto");
            //        Console.WriteLine("开始录音...");
            //        continue;
            //    }
            //}
            //if (!isCapsLockOn)
            //{
            //    if (_status==true)
            //    {
            //        _status = false;
            //        await _xiaoZhiAgent.Send_Listen_Stop();
            //        Console.WriteLine("结束录音");
            //        continue;
            //    }
            //}
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                if (_status == false)
                {
                    _status = true;
                    await _xiaoZhiAgent.Send_Listen_Start("manual");
                    Console.Title = "小智AI 开始录音...";
                    Console.WriteLine("小智：开始录音... 再次回车结束录音");
                    continue;
                }
                else
                {
                    if (_status == true)
                    {
                        _status = false;
                        await _xiaoZhiAgent.Send_Listen_Stop();
                        Console.Title = "小智AI 调试助手";
                        Console.WriteLine("小智：结束录音");
                        continue;
                    }
                }
                //Console.WriteLine("空格");
                continue;
            }
            else
            {
                if (_status == false)
                {
                    if (input == "restart")
                    {
                        _xiaoZhiAgent.Restart();
                        continue;
                    }    
                    await _xiaoZhiAgent.Send_Listen_Detect(input);
                }
            }
        }
    }
}