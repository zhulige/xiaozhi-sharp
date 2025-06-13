using XiaoZhiSharp;
using XiaoZhiSharp.Protocols;
using System.Collections.ObjectModel;

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
    
    public class XiaoZhi_AgentService
    {
        private readonly XiaoZhiAgent _agent;

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
                var audioService = new Services.AudioService();
                // 配置VAD参数
                audioService.ConfigureVAD(UseVAD, VadEnergyThreshold, VadSilenceFrames, TtsCooldownTime);
                _agent.AudioService = audioService;
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
               //_agent.AudioService = new Services.AudioService();
            }
            
            _ = Task.Run(async () => await _agent.Start());
            IsConnected = true; // 假设启动后就连接成功
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
                audioService?.ConfigureVAD(UseVAD, VadEnergyThreshold, VadSilenceFrames, TtsCooldownTime);
            }
            
            // 重启连接
            await _agent.Restart();
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
                // 添加用户问题到聊天历史
                ChatHistory.Add(new ChatMessage(message, true));
            }
            if (type == "answer")
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
            if (type == "emotion")
                Emotion = message;
            if (type == "answer_stop")
            {
                // TTS播放结束，触发音频服务的冷却期
                if (_agent.AudioService != null && DeviceInfo.Platform == DevicePlatform.Android)
                {
                    var audioService = _agent.AudioService as Services.AudioService;
                    audioService?.StopPlaying(); // 这会触发冷却期
                }
                
                // 延迟后再开始录音，等待冷却期结束
                _ = Task.Run(async () =>
                {
                    await Task.Delay((int)(TtsCooldownTime * 1000));
                    await _agent.StartRecording("auto");
                });
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
    }
}
