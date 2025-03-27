using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using XiaozhiAI.Models.IoT;
using XiaozhiAI.Models.IoT.Things;
using XiaoZhiSharp;
using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Services;

class Program
{
    private static XiaoZhiAgent? _xiaoZhiAgent;
    private static bool _status = false;
    static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Title = "小智AI 控制台客户端";
        // 定义默认值
        string OTA_VERSION_URL = "https://api.tenclass.net/xiaozhi/ota/";
        string WEB_SOCKET_URL = "wss://api.tenclass.net/xiaozhi/v1/";
        //string WEB_SOCKET_URL = "ws://192.168.10.29:8000";
        string MAC_ADDR = "";
        string logoAndCopyright = @"
========================================================================
欢迎使用“小智AI 控制台客户端” ！版本 v1.0.1

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
        _xiaoZhiAgent.IsLogWrite = true;
        _xiaoZhiAgent.Start();
        _xiaoZhiAgent.OnMessageEvent += _xiaoZhiAgent_OnMessageEvent;
        await Task.Delay(1000);



        // 1. 注册生成设备描述
        ThingManager.GetInstance().AddThing(new Lamp());
        ThingManager.GetInstance().AddThing(new Camera());
        ThingManager.GetInstance().AddThing(new Speaker());
        string descriptorJson = ThingManager.GetInstance().GetDescriptorsJson();
        await Task.Delay(1000);
        await _xiaoZhiAgent.IotInit(descriptorJson);
        await _xiaoZhiAgent.Send_Listen_Detect("你好啊,当前虚拟设备有啥");

         _xiaoZhiAgent.StartMqtt();

        while (true)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                if (_status == false)
                {
                    _status = true;
                    await _xiaoZhiAgent.Send_Listen_Start("manual");
                    Console.Title = "小智AI 开始录音...";
                    Console.WriteLine("开始录音... 再次回车结束录音");
                    continue;
                }
                else
                {
                    if (_status == true)
                    {
                        _status = false;
                        await _xiaoZhiAgent.Send_Listen_Stop();
                        Console.Title = "小智AI 控制台客户端";
                        Console.WriteLine("结束录音");
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

    private static void _xiaoZhiAgent_OnMessageEvent(string message)
    {
        dynamic? msg = JsonConvert.DeserializeObject<dynamic>(message);
        if (msg != null)
        {
            if (msg.type == "tts") {
                if (msg.state == "sentence_start") {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"小智：{msg.text}");
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
            }

            if (msg.type == "stt") {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"{msg.text}");
            }

            if (msg.type=="iot")
            {
                var msgss = JObject.Parse(message);
                HandleIotMessage(msgss);
            }
            
        }
    }
    /// <summary>
    /// 处理物联Iot网消息
    /// </summary>
    /// <param name="data">带有iot的json数据</param>
    private static void HandleIotMessage(JObject data)
    {
        try
        {
            // 检查消息类型
            var type = data["type"]?.ToString();
            if (type != "iot")
            {
                Console.WriteLine($"非物联网消息类型: {type}", true);
                return;
            }

            // 获取命令数组
            var commands = data["commands"] as JArray;
            if (commands == null || commands.Count == 0)
            {
                Console.WriteLine("物联网命令为空或格式不正确", true);
                return;
            }

            foreach (JObject command in commands)
            {
                try
                {
                    // 记录接收到的命令
                    var mes = command.ToString(Newtonsoft.Json.Formatting.None);
                    //mqttService.PublishAsync(mes);

                    Console.WriteLine($"收到物联网命令: {mes}");

                    // 执行命令
                    var result = ThingManager.GetInstance().Invoke(command);
                    Console.WriteLine($"执行物联网命令结果: {result}");

                    // 命令执行后更新设备状态
                   _xiaoZhiAgent.IotState();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"执行物联网命令失败: {ex.Message}", true);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理物联网消息失败: {ex.Message}", true);
        }
    }

}