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
        string WEB_SOCKET_URL = "ws://192.168.31.113:8888/xiaozhi/v1/";
        string MAC_ADDR = "";
        string logoAndCopyright = @"
========================================================================
欢迎使用“小智AI 服务器测试控制台” ！版本 v1.0.0
        
要是你在使用中有啥想法或者遇到问题，别犹豫，找我们哟：
微信：Zhu-Lige       电子邮箱：ZhuLige@qq.com
有任何动态请大家关注 https://github.com/zhulige/xiaozhi-sharp
========================================================================";
        Console.WriteLine(logoAndCopyright);
        Console.WriteLine("启动：XiaoZhiSharp_PerformanceTesting.exe <OTA_VERSION_URL> <WEB_SOCKET_URL>");
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

        int number = 30000;
        int i = 0;
        while (i < number)
        {
            await Task.Run(() =>
            {
                MAC_ADDR = Guid.NewGuid().ToString();
                XiaoZhiSharp.XiaoZhiAgent _agent = new XiaoZhiSharp.XiaoZhiAgent(OTA_VERSION_URL, WEB_SOCKET_URL, MAC_ADDR);
                _agent.IsAudio = false;
                _agent.IsOTA = false;
                _agent.Start();
            });
            ;
            i++;
            Thread.Sleep(10);
        }

        Console.ReadLine();
    }
}