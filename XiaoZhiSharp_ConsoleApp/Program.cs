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

class Program
{
    private static IMcpClient _mcpClient = null!;
    private static Pipe _clientToServerPipe = new Pipe();
    private static Pipe _serverToClientPipe = new Pipe();
    private static IHost? _host;
    private static XiaoZhiAgent _agent;
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


        builder.Services
            .AddMcpServer()
            .WithStreamServerTransport(_clientToServerPipe.Reader.AsStream(), _serverToClientPipe.Writer.AsStream())
            .WithTools<IotThings_Tool>()
            .WithTools<Chrome_Tool>();

        _host = builder.Build();
        await _host.StartAsync();

        _agent = new XiaoZhiAgent();
        _agent.OnMessageEvent += Agent_OnMessageEvent;
        await _agent.Start();

        while (true)
        {
            string? input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input))
            {
                await _agent.ChatMessage(input);
            }
            else
            {
            }
        }
    }

    private static async Task Agent_OnMessageEvent(string type, string message)
    {
        LogConsole.InfoLine($"[{type}] {message}");

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