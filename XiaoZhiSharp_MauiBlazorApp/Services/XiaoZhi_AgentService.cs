using XiaoZhiSharp;
using XiaoZhiSharp.Protocols;
using System.Collections.ObjectModel;
using System.IO.Pipelines;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XiaoZhiSharp_MauiBlazorApp.McpTools;
using ModelContextProtocol.Protocol;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XiaoZhiSharp_MauiBlazorApp.McpTools;
using System.Net.WebSockets;

namespace XiaoZhiSharp_MauiBlazorApp.Services
{
    // 聊天消息类
    public class ChatMessage
    {
        public string Content { get; set; } = "";
        public bool IsUser { get; set; } = false;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public ChatMessage(string content, bool isUser)
        {
            Content = content;
            IsUser = isUser;
            Timestamp = DateTime.Now;
        }
    }
    
    public class XiaoZhi_AgentService : IDisposable
    {
        private readonly XiaoZhiAgent _agent;
        private bool _disposed = false;

        // MCP相关字段
        private IMcpClient _mcpClient = null!;
        private Pipe _clientToServerPipe = new Pipe();
        private Pipe _serverToClientPipe = new Pipe();
        private IHost? _host;

        public string QuestionMessae = "";
        public string AnswerMessae = "";
        public string Emotion = "normal";
        public float AudioLevel = 0.0f; // 音频强度 0-1
        public bool IsConnected = false; // 连接状态
        
        // 设置相关属性
        public string ServerUrl { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";
        public string OtaUrl { get; set; } = "https://api.tenclass.net/xiaozhi/ota/";
        public string DeviceId { get; set; } = Global.DeivceId;
        public int VadThreshold { get; set; } = 40;
        public bool IsDebugMode { get; set; } = false;
        
        // VAD配置（从Unity版本移植）
        public bool UseVAD { get; set; } = true;
        public float VadEnergyThreshold { get; set; } = 0.015f;
        public int VadSilenceFrames { get; set; } = 20;
        public float TtsCooldownTime { get; set; } = 0.5f;

        // OTA 相关信息
        public OtaResponse? LatestOtaResponse { get; private set; }
        public string OtaStatus { get; private set; } = "未检查";
        public DateTime? LastOtaCheckTime { get; private set; }
        public string? ActivationCode { get; private set; }
        public string? ActivationMessage { get; private set; }
        public string? FirmwareVersion { get; private set; }
        public string? FirmwareUrl { get; private set; }
        public DateTime? ServerTime { get; private set; }
        public string? MqttEndpoint { get; private set; }
        public ObservableCollection<string> DebugLogs { get; private set; } = new();
        
        // 版本信息
        public string CurrentVersion { get; private set; } = "1.0.0"; // 当前版本
        public string? LatestVersion { get; private set; } // 最新版本
        public bool NeedUpdate { get; private set; } = false; // 是否需要更新
        public string UpdateMessage { get; private set; } = ""; // 更新提示信息
        
        // 聊天历史记录
        public ObservableCollection<ChatMessage> ChatHistory { get; private set; } = new();
        
        public XiaoZhiAgent Agent
        {
            get { return _agent; }
        }
        public int VadCounter
        {
            get { return _agent.AudioService?.VadCounter ?? 0; }
        }
        public bool IsRecording
        {
            get { return _agent.AudioService?.IsRecording ?? false; }
        }

        private System.Timers.Timer? _connectionMonitorTimer;
        private DateTime _lastReconnectAttempt = DateTime.MinValue;
        private int _reconnectAttempts = 0;
        private const int MaxReconnectAttempts = 5; // 最大重连尝试次数
        private const int ReconnectInterval = 5000; // 重连间隔(毫秒)

        public XiaoZhi_AgentService()
        {
            // 从配置初始化设置
            LoadSettings();
            
            // 获取当前版本（可以从应用程序信息中获取）
            CurrentVersion = AppInfo.VersionString ?? "1.0.0";
            
            XiaoZhiSharp.Global.VadThreshold = VadThreshold;
            XiaoZhiSharp.Global.IsDebug = IsDebugMode;
            //XiaoZhiSharp.Global.IsAudio = false;
            _agent = new XiaoZhiAgent();
            _agent.DeviceId = DeviceId;
            _agent.WsUrl = ServerUrl;
            _agent.OtaUrl = OtaUrl;
            _agent.OnMessageEvent += Agent_OnMessageEvent;
            _agent.OnAudioPcmEvent += Agent_OnAudioPcmEvent;
            _agent.OnOtaEvent += Agent_OnOtaEvent;
            
            // 根据平台注册相应的音频服务
            if (DeviceInfo.Platform == DevicePlatform.Android) 
            { 
                try
                {
                    var audioService = new Services.AudioService();
                    // 配置VAD参数
                    audioService.ConfigureVAD(UseVAD, VadEnergyThreshold, VadSilenceFrames, TtsCooldownTime);
                    _agent.AudioService = audioService;
                    AddDebugLog($"已初始化Android音频服务并配置VAD: 启用={UseVAD}, 阈值={VadEnergyThreshold}, 静音帧数={VadSilenceFrames}, 冷却时间={TtsCooldownTime}秒");
                }
                catch (Exception ex)
                {
                    AddDebugLog($"音频服务初始化失败: {ex.Message}");
                }
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
               //_agent.AudioService = new Services.AudioService();
               AddDebugLog("Windows平台暂未实现音频服务");
            }
            
            // 初始化MCP服务
            InitializeMcpService();
            
            _ = Task.Run(async () => await _agent.Start());
            IsConnected = true; // 假设启动后就连接成功
            
            // 启动连接状态监测定时器
            StartConnectionMonitor();
        }

        // 初始化MCP服务
        private void InitializeMcpService()
        {
            try
            {
                var builder = Host.CreateApplicationBuilder();

                builder.Services
                    .AddMcpServer()
                    .WithStreamServerTransport(_clientToServerPipe.Reader.AsStream(), _serverToClientPipe.Writer.AsStream())
                    .WithTools<IotThings_Tool>()
                    .WithTools<Chrome_Tool>()
                    .WithTools<WindowsApp_Tool>();

                _host = builder.Build();
                _host.StartAsync();
                
                AddDebugLog("MCP服务初始化成功");
            }
            catch (Exception ex)
            {
                AddDebugLog($"MCP服务初始化失败: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            // 从Preferences加载设置
            ServerUrl = Preferences.Get("ServerUrl", "wss://api.tenclass.net/xiaozhi/v1/");
            OtaUrl = Preferences.Get("OtaUrl", "https://api.tenclass.net/xiaozhi/ota/");
            DeviceId = Preferences.Get("DeviceId", Global.DeivceId);
            VadThreshold = Preferences.Get("VadThreshold", 40);
            IsDebugMode = Preferences.Get("IsDebugMode", false);
            
            // 加载VAD配置
            UseVAD = Preferences.Get("UseVAD", true);
            VadEnergyThreshold = Preferences.Get("VadEnergyThreshold", 0.015f);
            VadSilenceFrames = Preferences.Get("VadSilenceFrames", 20);
            TtsCooldownTime = Preferences.Get("TtsCooldownTime", 0.5f);
        }

        public void SaveSettings()
        {
            // 保存设置到Preferences
            Preferences.Set("ServerUrl", ServerUrl);
            Preferences.Set("OtaUrl", OtaUrl);
            Preferences.Set("DeviceId", DeviceId);
            Preferences.Set("VadThreshold", VadThreshold);
            Preferences.Set("IsDebugMode", IsDebugMode);
            
            // 保存VAD配置
            Preferences.Set("UseVAD", UseVAD);
            Preferences.Set("VadEnergyThreshold", VadEnergyThreshold);
            Preferences.Set("VadSilenceFrames", VadSilenceFrames);
            Preferences.Set("TtsCooldownTime", TtsCooldownTime);
        }

        public async Task ApplySettings()
        {
            // 应用设置并重启连接
            SaveSettings();
            
            // 更新全局配置
            XiaoZhiSharp.Global.VadThreshold = VadThreshold;
            XiaoZhiSharp.Global.IsDebug = IsDebugMode;
            
            // 更新Agent配置
            _agent.DeviceId = DeviceId;
            _agent.WsUrl = ServerUrl;
            _agent.OtaUrl = OtaUrl;
            
            // 更新音频服务的VAD配置
            if (_agent.AudioService != null && DeviceInfo.Platform == DevicePlatform.Android)
            {
                var audioService = _agent.AudioService as Services.AudioService;
                //audioService?.ConfigureVAD(UseVAD, VadEnergyThreshold, VadSilenceFrames, TtsCooldownTime);
            }
            
            // 重启连接
            await _agent.Restart();
            
            // 重置VAD配置
            if (_agent.AudioService != null && DeviceInfo.Platform == DevicePlatform.Android)
            {
                var audioService = _agent.AudioService as Services.AudioService;
                audioService?.ConfigureVAD(UseVAD, VadEnergyThreshold, VadSilenceFrames, TtsCooldownTime);
                AddDebugLog("连接重启后，已重置VAD参数");
            }
        }

        public void ResetSettings()
        {
            // 重置为默认值
            ServerUrl = "wss://api.tenclass.net/xiaozhi/v1/";
            OtaUrl = "https://api.tenclass.net/xiaozhi/ota/";
            DeviceId = Global.DeivceId;
            VadThreshold = 40;
            IsDebugMode = false;
            
            // 重置VAD配置为默认值
            UseVAD = true;
            VadEnergyThreshold = 0.015f;
            VadSilenceFrames = 20;
            TtsCooldownTime = 0.5f;
        }

        private async Task Agent_OnAudioPcmEvent(byte[] pcm)
        {
            // 计算音频强度
            AudioLevel = CalculateAudioLevel(pcm);
        }

        private float CalculateAudioLevel(byte[] pcmData)
        {
            if (pcmData == null || pcmData.Length == 0)
                return 0.0f;

            double rms = 0;
            int sampleCount = pcmData.Length / 2; // 16位音频

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(pcmData, i * 2);
                rms += sample * sample;
            }

            rms = Math.Sqrt(rms / sampleCount);
            float level = (float)(rms / short.MaxValue);
            
            // 限制在0-1范围
            return Math.Max(0.0f, Math.Min(1.0f, level * 10)); // 放大10倍以便显示
        }

        private async Task Agent_OnMessageEvent(string type, string message)
        {
            if (type == "question")
            {
                QuestionMessae = message;
                // 添加用户问题到聊天历史，但首先检查是否已经存在相同内容的用户消息
                if (ChatHistory.Count == 0 || ChatHistory.Last().Content != message || !ChatHistory.Last().IsUser)
                {
                    ChatHistory.Add(new ChatMessage(message, true));
                }
            }
            else if (type == "answer")
            {
                AnswerMessae = message;
                // 添加AI回答到聊天历史
                if (ChatHistory.Count > 0 && !ChatHistory.Last().IsUser)
                {
                    // 如果最后一条消息是AI的，更新它
                    ChatHistory.Last().Content = message;
                }
                else
                {
                    // 否则添加新的AI消息
                    ChatHistory.Add(new ChatMessage(message, false));
                }
            }
            else if (type == "emotion")
            {
                Emotion = message;
            }
            else if (type == "answer_stop")
            {
                // TTS播放结束，触发音频服务的冷却期
                if (_agent.AudioService != null && DeviceInfo.Platform == DevicePlatform.Android)
                {
                    var audioService = _agent.AudioService as Services.AudioService;
                    audioService?.StopPlaying(); // 这会触发冷却期
                    
                    // 添加新逻辑：在冷却期结束后，自动启动新一轮录音
                    _ = Task.Run(async () =>
                    {
                        // 等待冷却期结束(ttsCooldownTime+0.1秒额外缓冲)
                        await Task.Delay(TimeSpan.FromSeconds(TtsCooldownTime + 0.1));
                        
                        // 确保不在录音中
                        if (_agent != null && !_agent.IsRecording)
                        {
                            AddDebugLog("冷却期结束，自动开始新一轮录音");
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                try
                                {
                                    await _agent.StartRecording("auto");
                                }
                                catch (Exception ex)
                                {
                                    AddDebugLog($"自动开始录音失败: {ex.Message}");
                                }
                            });
                        }
                    });
                }
            }
            // 处理MCP消息
            else if (type == "mcp")
            {
                await HandleMcpMessage(message);
            }

            // 添加调试日志
            if (IsDebugMode)
            {
                AddDebugLog($"[{type}] {message}");
            }
        }

        private async Task Agent_OnOtaEvent(OtaResponse? otaResponse)
        {
            LastOtaCheckTime = DateTime.Now;
            LatestOtaResponse = otaResponse;

            if (otaResponse != null)
            {
                OtaStatus = "检查成功";
                
                // 提取激活信息
                if (otaResponse.Activation != null)
                {
                    ActivationCode = otaResponse.Activation.Code;
                    ActivationMessage = otaResponse.Activation.Message;
                }

                // 提取固件信息
                if (otaResponse.Firmware != null)
                {
                    FirmwareVersion = otaResponse.Firmware.Version;
                    FirmwareUrl = otaResponse.Firmware.Url;
                    LatestVersion = FirmwareVersion;
                    
                    // 比较版本
                    CompareVersions();
                }

                // 提取服务器时间
                if (otaResponse.ServerTime != null)
                {
                    ServerTime = DateTimeOffset.FromUnixTimeMilliseconds(otaResponse.ServerTime.Timestamp).DateTime;
                }

                // 提取MQTT信息
                if (otaResponse.Mqtt != null)
                {
                    MqttEndpoint = otaResponse.Mqtt.Endpoint;
                }

                // 如果WebSocket配置更新了，更新当前URL
                if (otaResponse.WebSocket != null && !string.IsNullOrEmpty(otaResponse.WebSocket.Url))
                {
                    ServerUrl = otaResponse.WebSocket.Url;
                }

                AddDebugLog($"OTA检查成功，激活码: {ActivationCode}");
                
                if (!string.IsNullOrEmpty(FirmwareUrl))
                {
                    AddDebugLog($"发现固件更新: {FirmwareVersion}");
                }
            }
            else
            {
                OtaStatus = "检查失败";
                AddDebugLog("OTA检查失败，使用默认配置");
            }
        }

        private void AddDebugLog(string message)
        {
            var logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            
            if (DebugLogs.Count >= 100) // 保持最新100条日志
            {
                DebugLogs.RemoveAt(0);
            }
            
            DebugLogs.Add(logMessage);
        }

        public void ClearDebugLogs()
        {
            DebugLogs.Clear();
        }
        
        /// <summary>
        /// 清除聊天记录
        /// </summary>
        public void ClearChatHistory()
        {
            ChatHistory.Clear();
            QuestionMessae = "";
            AnswerMessae = "";
        }
        
        /// <summary>
        /// 获取OTA更新状态信息
        /// </summary>
        /// <returns>包含当前版本、最新版本和更新状态的字符串</returns>
        public string GetUpdateStatusInfo()
        {
            if (string.IsNullOrEmpty(LatestVersion))
            {
                return $"当前版本: {CurrentVersion}\n尚未检查更新";
            }
            
            if (NeedUpdate)
            {
                return $"当前版本: {CurrentVersion}\n最新版本: {LatestVersion}\n状态: 需要更新";
            }
            else
            {
                return $"当前版本: {CurrentVersion}\n最新版本: {LatestVersion}\n状态: 已是最新";
            }
        }

        // 版本比较方法
        private void CompareVersions()
        {
            try
            {
                if (string.IsNullOrEmpty(LatestVersion))
                {
                    NeedUpdate = false;
                    UpdateMessage = "无法获取最新版本信息";
                    return;
                }

                // 解析版本号
                var currentParts = CurrentVersion.Split('.');
                var latestParts = LatestVersion.Split('.');

                // 比较主版本号、次版本号、修订号
                for (int i = 0; i < Math.Min(currentParts.Length, latestParts.Length); i++)
                {
                    if (int.TryParse(currentParts[i], out int current) && 
                        int.TryParse(latestParts[i], out int latest))
                    {
                        if (latest > current)
                        {
                            NeedUpdate = true;
                            UpdateMessage = $"发现新版本！当前版本: {CurrentVersion}，最新版本: {LatestVersion}";
                            AddDebugLog(UpdateMessage);
                            return;
                        }
                        else if (latest < current)
                        {
                            NeedUpdate = false;
                            UpdateMessage = $"当前版本已是最新（当前: {CurrentVersion}，服务器: {LatestVersion}）";
                            return;
                        }
                    }
                }

                // 如果版本号位数不同
                if (latestParts.Length > currentParts.Length)
                {
                    NeedUpdate = true;
                    UpdateMessage = $"发现新版本！当前版本: {CurrentVersion}，最新版本: {LatestVersion}";
                }
                else
                {
                    NeedUpdate = false;
                    UpdateMessage = $"当前版本已是最新（{CurrentVersion}）";
                }

                AddDebugLog(UpdateMessage);
            }
            catch (Exception ex)
            {
                AddDebugLog($"版本比较失败: {ex.Message}");
                NeedUpdate = false;
                UpdateMessage = "版本比较失败";
            }
        }

        public async Task ManualOtaCheck()
        {
            try
            {
                OtaStatus = "检查中...";
                AddDebugLog("开始手动OTA检查");
                
                // 确保设置当前版本
                _agent.CurrentVersion = CurrentVersion;
                
                var result = await _agent.CheckOtaUpdate();
                
                if (result != null)
                {
                    AddDebugLog("手动OTA检查成功");
                    
                    // 显示版本比较结果
                    if (NeedUpdate)
                    {
                        AddDebugLog($"【版本信息】{UpdateMessage}");
                        if (!string.IsNullOrEmpty(FirmwareUrl))
                        {
                            AddDebugLog($"【更新地址】{FirmwareUrl}");
                        }
                    }
                    else
                    {
                        AddDebugLog($"【版本信息】{UpdateMessage}");
                    }
                }
                else
                {
                    AddDebugLog("手动OTA检查失败");
                }
            }
            catch (Exception ex)
            {
                OtaStatus = "检查异常";
                AddDebugLog($"OTA检查异常: {ex.Message}");
            }
        }

        // 处理MCP消息
        private async Task HandleMcpMessage(string message)
        {
            try
            {
                AddDebugLog($"收到MCP消息: {message}");

                if (_mcpClient == null)
                {
                    var clientTransport = new StreamClientTransport(
                        serverInput: _clientToServerPipe.Writer.AsStream(),
                        serverOutput: _serverToClientPipe.Reader.AsStream());

                    _mcpClient = await McpClientFactory.CreateAsync(clientTransport);
                    AddDebugLog("MCP客户端初始化成功");
                }

                dynamic? mcp = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(message);
                if (mcp == null)
                {
                    AddDebugLog("MCP消息解析失败");
                    return;
                }

                if (mcp.method == "initialize")
                {
                    // 处理初始化请求
                    var resultData = new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = _mcpClient.ServerCapabilities,
                        serverInfo = new
                        {
                            name = "XiaoZhiSharp MAUI",
                            version = CurrentVersion
                        }
                    };

                    JsonNode resultNode = JsonSerializer.SerializeToNode(resultData);
                    JsonRpcResponse? response = new JsonRpcResponse()
                    {
                        Id = new RequestId((long)mcp.id),
                        JsonRpc = "2.0",
                        Result = resultNode
                    };

                    await _agent.McpMessage(JsonSerializer.Serialize(response));
                    AddDebugLog("已响应MCP初始化请求");
                }
                else if (mcp.method == "tools/list")
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

                    await _agent.McpMessage(JsonSerializer.Serialize(response, options));
                    AddDebugLog("已响应MCP工具列表请求");
                }
                else if (mcp.method == "tools/call")
                {
                    // 处理工具调用请求
                    JsonNode? root = JsonNode.Parse(message);

                    string? name = root?["params"]?["name"]?.GetValue<string>();
                    JsonNode? argumentsNode = root?["params"]?["arguments"];

                    Dictionary<string, object>? arguments = null;
                    if (argumentsNode != null)
                    {
                        arguments = argumentsNode.Deserialize<Dictionary<string, object>>();
                    }

                    AddDebugLog($"调用工具: {name}, 参数: {argumentsNode}");

                    CallToolResponse? callToolResponse = await _mcpClient.CallToolAsync(name, arguments);
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
                    await _agent.McpMessage(JsonSerializer.Serialize(response, options));
                    AddDebugLog("已响应MCP工具调用请求");
                }
            }
            catch (Exception ex)
            {
                AddDebugLog($"处理MCP消息出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动连接状态监测定时器
        /// </summary>
        private void StartConnectionMonitor()
        {
            if (_connectionMonitorTimer != null)
            {
                _connectionMonitorTimer.Stop();
                _connectionMonitorTimer.Dispose();
            }
            
            _connectionMonitorTimer = new System.Timers.Timer(2000); // 每2秒检查一次
            _connectionMonitorTimer.Elapsed += async (sender, e) => await CheckConnectionStatus();
            _connectionMonitorTimer.AutoReset = true;
            _connectionMonitorTimer.Start();
            
            AddDebugLog("启动连接状态监测");
        }
        
        /// <summary>
        /// 检查连接状态并在必要时重连
        /// </summary>
        private async Task CheckConnectionStatus()
        {
            try
            {
                bool wasConnected = IsConnected;
                IsConnected = _agent.ConnectState == WebSocketState.Open;
                
                // 如果连接状态发生变化
                if (IsConnected != wasConnected)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        if (IsConnected)
                        {
                            AddDebugLog("连接状态: 已连接");
                            _reconnectAttempts = 0; // 连接成功，重置重连尝试计数
                            
                            // 重连成功后，彻底重置音频服务和VAD状态
                            if (_agent.AudioService != null && DeviceInfo.Platform == DevicePlatform.Android)
                            {
                                var audioService = _agent.AudioService as Services.AudioService;
                                if (audioService != null)
                                {
                                    // 停止任何可能在进行的录音
                                    if (audioService.IsRecording)
                                    {
                                        audioService.StopRecording();
                                    }
                                    
                                    // 确保不在播放状态
                                    if (audioService.IsPlaying)
                                    {
                                        audioService.StopPlaying();
                                    }
                                    
                                    // 重置VAD参数和状态
                                    audioService.ConfigureVAD(UseVAD, VadEnergyThreshold, VadSilenceFrames, TtsCooldownTime);
                                    audioService.ResetTtsState(); // 特别重置TTS状态
                                    AddDebugLog("重连成功，已重置音频服务和VAD参数");
                                    
                                    // 延迟一秒后尝试开始新的录音会话
                                    _ = Task.Run(async () =>
                                    {
                                        await Task.Delay(1000);
                                        await MainThread.InvokeOnMainThreadAsync(async () =>
                                        {
                                            try
                                            {
                                                if (!_agent.IsRecording)
                                                {
                                                    AddDebugLog("重连后开始新一轮录音");
                                                    await _agent.StartRecording("auto");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                AddDebugLog($"重连后开始录音失败: {ex.Message}");
                                            }
                                        });
                                    });
                                }
                            }
                        }
                        else
                        {
                            AddDebugLog("连接状态: 已断开");
                            
                            // 断线时停止任何录音
                            if (_agent.IsRecording)
                            {
                                _ = _agent.StopRecording();
                                AddDebugLog("断线检测，已停止录音");
                            }
                        }
                    });
                }
                
                // 如果断开连接，尝试重连
                if (!IsConnected && 
                    (DateTime.Now - _lastReconnectAttempt).TotalMilliseconds > ReconnectInterval &&
                    _reconnectAttempts < MaxReconnectAttempts)
                {
                    _lastReconnectAttempt = DateTime.Now;
                    _reconnectAttempts++;
                    
                    await MainThread.InvokeOnMainThreadAsync(async () => {
                        AddDebugLog($"尝试重新连接... (第{_reconnectAttempts}次)");
                        try 
                        {
                            await _agent.Restart();
                        }
                        catch (Exception ex)
                        {
                            AddDebugLog($"重新连接失败: {ex.Message}");
                        }
                    });
                }
                
                // 如果多次尝试重连都失败，暂停一段时间后重试
                if (!IsConnected && _reconnectAttempts >= MaxReconnectAttempts && 
                    (DateTime.Now - _lastReconnectAttempt).TotalSeconds > 30)
                {
                    _reconnectAttempts = 0; // 重置尝试次数，进入新一轮尝试
                }
            }
            catch (Exception ex)
            {
                AddDebugLog($"连接监测异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源，确保应用程序关闭时能够正确释放所有资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                // 释放连接监测定时器
                if (_connectionMonitorTimer != null)
                {
                    _connectionMonitorTimer.Stop();
                    _connectionMonitorTimer.Dispose();
                    _connectionMonitorTimer = null;
                }
                
                // 停止MCP服务
                _host?.StopAsync().Wait();
                _host?.Dispose();
                
                // 释放其他资源
                _agent?.Dispose();
                
                // 标记为已释放
                GC.SuppressFinalize(this);
            }
        }
    }
}
