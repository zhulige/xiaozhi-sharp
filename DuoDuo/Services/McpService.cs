using DuoDuo.McpTools;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DuoDuo.Services
{
    public class McpService
    {
        private static IHost? _host;
        private static IMcpClient _mcpClient;
        public McpService() {
            var builder = Host.CreateApplicationBuilder();

            var mcpBuilder = builder.Services
                .AddMcpServer()
                .WithStreamServerTransport(Global.McpClientToServerPipe.Reader.AsStream(), Global.McpServerToClientPipe.Writer.AsStream())
                .WithTools<IotThings_Tool>();

            _host = builder.Build();
            _host.StartAsync();
        }

        public async Task<string> McpMessageHandle(string message)
        {
            try
            {
                if (_mcpClient == null)
                {
                    var clientTransport = new StreamClientTransport(
                        serverInput: Global.McpClientToServerPipe.Writer.AsStream(),
                        serverOutput: Global.McpServerToClientPipe.Reader.AsStream());

                    _mcpClient = await McpClientFactory.CreateAsync(clientTransport);
                }

                dynamic? mcp = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(message);
                if (mcp == null)
                    return string.Empty;

                JsonNode? root = JsonNode.Parse(message);

                if (mcp.method == "initialize")
                {
                    Global.McpVisionUrl = root?["params"]?["capabilities"]?["vision"]?["url"]?.GetValue<string>();
                    Global.McpVisionToken = root?["params"]?["capabilities"]?["vision"]?["token"]?.GetValue<string>();


                    // 处理初始化请求
                    var resultData = new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = _mcpClient.ServerCapabilities,
                        serverInfo = new
                        {
                            name = "XiaoZhiSharp",
                            version = Global.CurrentVersion,
                        }
                    };

                    JsonNode resultNode = JsonSerializer.SerializeToNode(resultData);
                    JsonRpcResponse? response = new JsonRpcResponse()
                    {
                        Id = new RequestId((long)mcp.id),
                        JsonRpc = "2.0",
                        Result = resultNode
                    };

                    return JsonSerializer.Serialize(response);

                }

                if (mcp.method == "tools/list")
                {
                    // 处理工具列表请求
                    var tools = await _mcpClient.ListToolsAsync();
                    List<Tool> toolsList = new List<Tool>();
                    foreach (var item in tools)
                    {
                        toolsList.Add(item.ProtocolTool);
                    }

                    var resultData = new
                    {
                        tools = toolsList
                    };

                    JsonNode resultNode = JsonSerializer.SerializeToNode(resultData);
                    JsonRpcResponse? response = new JsonRpcResponse()
                    {
                        Id = new RequestId((long)mcp.id),
                        JsonRpc = "2.0",
                        Result = resultNode
                    };

                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

                    return JsonSerializer.Serialize(response, options);
                }

                if (mcp.method == "tools/call")
                {
                    // 处理工具调用请求
                    //JsonNode? root = JsonNode.Parse(message);

                    string? name = root?["params"]?["name"]?.GetValue<string>();
                    JsonNode? argumentsNode = root?["params"]?["arguments"];

                    Dictionary<string, object>? arguments = null;
                    if (argumentsNode != null)
                    {
                        arguments = argumentsNode.Deserialize<Dictionary<string, object>>();
                    }

                    ModelContextProtocol.Protocol.CallToolResult? callToolResponse = await _mcpClient.CallToolAsync(name, arguments);
                    JsonNode jsonNode = JsonSerializer.SerializeToNode(callToolResponse);
                    JsonRpcResponse? response = new JsonRpcResponse()
                    {
                        Id = new RequestId((long)mcp.id),
                        JsonRpc = "2.0",
                        Result = jsonNode
                    };

                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    return JsonSerializer.Serialize(response, options);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"MCP处理异常: {ex.Message}";
            }

            return string.Empty;
        }
    }
}
