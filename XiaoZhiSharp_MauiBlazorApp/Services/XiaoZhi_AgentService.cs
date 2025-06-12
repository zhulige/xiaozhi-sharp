using XiaoZhiSharp;
using XiaoZhiSharp.Protocols;
using System.Collections.ObjectModel;

namespace XiaoZhiSharp_MauiBlazorApp.Services
{
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
                _agent.AudioService = new Services.AudioService();
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
        }

        public void SaveSettings()
        {
            // 保存设置到Preferences
            Preferences.Set("ServerUrl", ServerUrl);
            Preferences.Set("OtaUrl", OtaUrl);
            Preferences.Set("DeviceId", DeviceId);
            Preferences.Set("VadThreshold", VadThreshold);
            Preferences.Set("IsDebugMode", IsDebugMode);
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
                QuestionMessae = message;
            if (type == "answer")
                AnswerMessae = message;
            if (type == "emotion")
                Emotion = message;
            if (type == "answer_stop")
                await _agent.StartRecording("auto");

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

        public async Task ManualOtaCheck()
        {
            try
            {
                OtaStatus = "检查中...";
                AddDebugLog("开始手动OTA检查");
                
                var result = await _agent.CheckOtaUpdate();
                
                if (result != null)
                {
                    AddDebugLog("手动OTA检查成功");
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
