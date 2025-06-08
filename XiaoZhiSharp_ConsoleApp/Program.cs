using Newtonsoft.Json;
using System.Net.NetworkInformation;
using XiaoZhiSharp;
using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Services;
using XiaoZhiSharp.Utils;

class Program
{
    static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Title = "小智XiaoZhiSharp客户端";
        string logoAndCopyright = @"
========================================================================
欢迎使用“小智XiaoZhiSharp客户端” ！

当前功能：
1. 语音消息 输入回车：开始录音；再次输入回车：结束录音
2. 文字消息 可以随意输入文字对话
3. 全量往返协议输出，方便调试
        
要是你在使用中有啥想法或者遇到问题，别犹豫，找我们哟：
微信：Zhu-Lige       电子邮箱：zhuLige@qq.com
有任何动态请大家关注 https://github.com/zhulige/xiaozhi-sharp
========================================================================";
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(logoAndCopyright);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("使用前请配置 appsettings.json ！");
        Console.WriteLine("========================================================================");
        Console.ForegroundColor = ConsoleColor.White;

        XiaoZhiAgent agent = new XiaoZhiAgent();
        agent.OnMessageEvent += Agent_OnMessageEvent;
        await agent.Start();

        while (true)
        {
            string? input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input))
            {
                await agent.ChatMessage(input);
            }
            else
            {
            }
        }

    }

    private static Task Agent_OnMessageEvent(string type, string message)
    {
        LogConsole.InfoLine($"[{type}] {message}");
        return Task.CompletedTask;
    }
}