using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Pipelines;
using System.Net.NetworkInformation;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using XiaoZhiSharp;
using XiaoZhiSharp_ConsoleApp.McpTools;
using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Services;
using XiaoZhiSharp.Utils;
using XiaoZhiSharp.Models;

class Program
{
    private static IMcpClient _mcpClient = null!;
    private static Pipe _clientToServerPipe = new Pipe();
    private static Pipe _serverToClientPipe = new Pipe();
    private static IHost? _host;
    private static XiaoZhiAgent _agent;
    //private static bool _recordStatus = false;
    private static string _audioMode = ""; 
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Title = "小智XiaoZhiSharp客户端";
        string logoAndCopyright = @"
========================================================================
欢迎使用“小智XiaoZhiSharp客户端” ！

当前功能：
1. 语音消息 输入回车：开始录音；再次输入回车：结束录音
   注意：CapsLock键也可以控制录音状态，按下CapsLock开始录音，再次按下结束录音
   录音结束后会自动发送语音消息到服务器，服务器会返回语音识别结果
2. 文字消息 可以随意输入文字对话
3. 全量往返协议输出，方便调试

要是你在使用中有啥想法或者遇到问题，别犹豫，找我们哟：
微信：Zhu-Lige       电子邮箱：zhuLige@qq.com
有任何动态请大家关注 https://github.com/zhulige/xiaozhi-sharp
========================================================================";
        Console.WriteLine(logoAndCopyright);
        Console.ForegroundColor = ConsoleColor.White;

        builder.Services
            .AddMcpServer()
            .WithStreamServerTransport(_clientToServerPipe.Reader.AsStream(), _serverToClientPipe.Writer.AsStream())
            .WithTools<IotThings_Tool>()
            .WithTools<Chrome_Tool>()
            .WithTools<WindowsApp_Tool>();

        _host = builder.Build();
        await _host.StartAsync();

        XiaoZhiSharp.Global.IsDebug = true;
        XiaoZhiSharp.Global.IsMcp = true;
        _agent = new XiaoZhiAgent();
        //XiaoZhiSharp.Global.SampleRate_WaveOut = 24000;
        //_agent.WsUrl = "wss://coze.nbee.net/xiaozhi/v1/"; 
        //_agent.AudioService = new 
        //_agent.OnAudioPcmEvent =
        _agent.OnMessageEvent += Agent_OnMessageEvent;
        _agent.OnOtaEvent += Agent_OnOtaEvent;
        LogConsole.InfoLine($"初始OTA URL: {_agent.OtaUrl}");
        LogConsole.InfoLine($"初始WebSocket URL: {_agent.WsUrl}");
        LogConsole.InfoLine($"设备ID: {_agent.DeviceId}");
        LogConsole.InfoLine($"客户端ID: {_agent.ClientId}");
        LogConsole.InfoLine($"User-Agent: {_agent.UserAgent}");
        await _agent.Start();

        _ = Task.Run(async () =>
        {
            while (true)
            {
                if (_agent.ConnectState != System.Net.WebSockets.WebSocketState.Open) { 
                    await _agent.Restart();
                    LogConsole.InfoLine("服务器重连...");
                    await Task.Delay(10000);
                }

                bool isCapsLockOn = Console.CapsLock;
                //Console.WriteLine($"当前 Caps Lock 状态: {(isCapsLockOn ? "开启" : "关闭")}");
                if (isCapsLockOn)
                {
                    if (_agent.IsRecording == false)
                    {
                        _audioMode = "manual";
                        LogConsole.InfoLine("开始录音... 再次按Caps键结束录音");
                        await _agent.StartRecording("manual");
                        continue;
                    }
                }
                if (!isCapsLockOn)
                {
                    if (_agent.IsRecording == true)
                    {
                        if (_audioMode == "manual")
                        {
                            await _agent.StopRecording();
                            LogConsole.InfoLine("结束录音");
                            continue;
                        }
                    }
                }
                await Task.Delay(100); // 避免过于频繁的检查
            }
        });

        while (true)
        {
            string? input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input))
            {
                if (input.ToLower() == "restart")
                {
                    await _agent.Restart();
                }
                else
                {
                    await _agent.ChatMessage(input);
                }
            }
            else
            {
                if (!_agent.IsRecording)
                {
                    //Console.Title = "开始录音...";
                    //LogConsole.InfoLine("开始录音... 再次回车结束录音");
                    _audioMode = "auto";
                    LogConsole.InfoLine("开始录音... Auto");
                    await _agent.StartRecording("auto");
                }
                else
                {
                    //await _agent.StopRecording();
                    //Console.Title = "小智XiaoZhiSharp客户端";
                    //LogConsole.InfoLine("结束录音");
                }
            }
        }
    }

    private static async Task Agent_OnOtaEvent(OtaResponse? otaResponse)
    {
        if (otaResponse != null)
        {
            LogConsole.InfoLine("=== OTA检查结果 ===");
            
            if (otaResponse.Activation != null)
            {
                LogConsole.InfoLine($"设备激活码: {otaResponse.Activation.Code}");
                LogConsole.InfoLine($"激活消息: {otaResponse.Activation.Message}");
            }

            if (otaResponse.Firmware != null && !string.IsNullOrEmpty(otaResponse.Firmware.Url))
            {
                LogConsole.InfoLine($"发现固件更新: {otaResponse.Firmware.Version}");
                LogConsole.InfoLine($"下载地址: {otaResponse.Firmware.Url}");
            }

            if (otaResponse.WebSocket != null)
            {
                LogConsole.InfoLine($"WebSocket服务器: {otaResponse.WebSocket.Url}");
            }

            if (otaResponse.Mqtt != null)
            {
                LogConsole.InfoLine($"MQTT服务器: {otaResponse.Mqtt.Endpoint}");
            }

            LogConsole.InfoLine("=== OTA检查完成 ===");
        }
        else
        {
            LogConsole.InfoLine("OTA检查失败，将使用默认配置");
        }
    }

    private static async Task Agent_OnMessageEvent(string type, string message)
    {
        switch(type.ToLower())
        {
            case "question":
                LogConsole.WriteLine(MessageType.Send, $"[{type}] {message}");
                break;
            case "answer":
                LogConsole.WriteLine(MessageType.Recv, $"[{type}] {message}");
                break;
            default:
                LogConsole.InfoLine($"[{type}] {message}");
                break;
        }
        //LogConsole.InfoLine($"[{type}] {message}");

        if (_mcpClient == null)
        {
            var clientTransport = new StreamClientTransport(
                serverInput: _clientToServerPipe.Writer.AsStream(),
                serverOutput: _serverToClientPipe.Reader.AsStream());

            _mcpClient = await McpClientFactory.CreateAsync(clientTransport);
        }
        
        if (type == "mcp")
        {
            dynamic? mcp = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(message);
            if (mcp.method == "initialize")
            {
                // 构造 Result 数据（非匿名类型，需确保属性名匹配）
                var resultData = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = _mcpClient.ServerCapabilities, // ServerCapabilities 对象
                    serverInfo = new
                    {
                        name = "RestSharp", // 设备名称 (BOARD_NAME)
                        version = "112.1.0.0" // 设备固件版本
                    }
                };

                // 直接序列化为 JsonNode（关键步骤）
                JsonNode resultNode = System.Text.Json.JsonSerializer.SerializeToNode(resultData);
                ModelContextProtocol.Protocol.JsonRpcResponse? response = new ModelContextProtocol.Protocol.JsonRpcResponse()
                {
                    Id = new RequestId((long)mcp.id),
                    JsonRpc = "2.0",
                    Result = resultNode
                };

                await _agent.McpMessage(System.Text.Json.JsonSerializer.Serialize(response));
            }

            if (mcp.method == "tools/list")
            {
                var tools = await _mcpClient.ListToolsAsync();
                List<Tool> toolss = new List<Tool>();
                foreach (var item in tools)
                {
                    toolss.Add(item.ProtocolTool);
                }
                var resultData = new
                {
                    tools = toolss
                };

                // 直接序列化为 JsonNode（关键步骤）
                JsonNode resultNode = System.Text.Json.JsonSerializer.SerializeToNode(resultData);
                ModelContextProtocol.Protocol.JsonRpcResponse? response = new ModelContextProtocol.Protocol.JsonRpcResponse()
                {
                    Id = new RequestId((long)mcp.id),
                    JsonRpc = "2.0",
                    Result = resultNode
                };
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 关键配置
                };

                await _agent.McpMessage(System.Text.Json.JsonSerializer.Serialize(response));
            }

            if (mcp.method == "tools/call") {
                // 解析整个 JSON
                JsonNode? root = JsonNode.Parse(message);

                // 安全提取 name 和 arguments
                string? name = root?["params"]?["name"]?.GetValue<string>();
                JsonNode? argumentsNode = root?["params"]?["arguments"];

                // 将 arguments 转换为 Dictionary<string, object>
                Dictionary<string, object>? arguments = null;
                if (argumentsNode != null)
                {
                    arguments = argumentsNode.Deserialize<Dictionary<string, object>>();
                }

                CallToolResponse? callToolResponse = await _mcpClient.CallToolAsync(name, arguments);
                JsonNode jsonNode = System.Text.Json.JsonSerializer.SerializeToNode(callToolResponse);
                ModelContextProtocol.Protocol.JsonRpcResponse? response = new ModelContextProtocol.Protocol.JsonRpcResponse()
                {
                    Id = new RequestId((long)mcp.id),
                    JsonRpc = "2.0",
                    Result = jsonNode
                };

                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 关键配置
                };
                await _agent.McpMessage(System.Text.Json.JsonSerializer.Serialize(response));
            }
        }
    }
}