using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Services;
using XiaoZhiSharp.Utils;
using System.Threading;
using XiaoZhiSharp.Models;

namespace XiaoZhiSharp
{
    public class XiaoZhiAgent : IDisposable
    {
        private string _otaUrl { get; set; } = "https://api.tenclass.net/xiaozhi/ota/";
        private string _wsUrl { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";
        private string _token { get; set; } = "test-token";
        private string _deviceId { get; set; } = SystemInfo.GetMacAddress();
        private string _clientId { get; set; } = SystemInfo.GenerateClientId();
        private string _userAgent { get; set; } = SystemInfo.GetUserAgent();
        private string _currentVersion { get; set; } = SystemInfo.GetApplicationVersion();
        private Services.Chat.ChatService? _chatService = null;
        private Services.IAudioService? _audioService = null;
        private Services.AudioOpusService _audioOpusService = new Services.AudioOpusService();
        private Services.OtaService? _otaService = null;
        private OtaResponse? _latestOtaResponse = null;

        // 添加一个用于跟踪当前任务的变量
        private Task? _monitoringTask = null;
        private bool _disposed = false;
        private CancellationTokenSource? _recordingCts = null;

        #region 属性
        public string WsUrl
        {
            get { return _wsUrl; }
            set { _wsUrl = value; }
        }
        public string OtaUrl
        {
            get { return _otaUrl; }
            set { _otaUrl = value; }
        }
        public Services.IAudioService? AudioService
        {
            get { return _audioService; }
            set { _audioService = value; }
        }
        public string DeviceId
        {
            get { return _deviceId; }
            set { _deviceId = value; }
        }
        public string ClientId
        {
            get { return _clientId; }
            set { _clientId = value; }
        }
        public string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }
        public string CurrentVersion
        {
            get { return _currentVersion; }
            set { _currentVersion = value; }
        }
        public string Token
        {
            get { return _token; }
            set { _token = value; }
        }
        public OtaResponse? LatestOtaResponse
        {
            get { return _latestOtaResponse; }
        }
        public bool IsPlaying
        {
            get { return _audioService != null && _audioService.IsPlaying; }
        }
        public bool IsRecording
        {
            get { return _audioService != null && _audioService.IsRecording; }
        }
        public WebSocketState ConnectState
        {
            get { return _chatService != null ? _chatService.ConnectState : WebSocketState.None; }
        }
        #endregion

        #region 事件
        public delegate Task MessageEventHandler(string type, string message);
        public event MessageEventHandler? OnMessageEvent = null;

        public delegate Task AudioEventHandler(byte[] opus);
        public event AudioEventHandler? OnAudioEvent = null;

        public delegate Task AudioPcmEventHandler(byte[] pcm);
        public event AudioPcmEventHandler? OnAudioPcmEvent = null;

        public delegate Task OtaEventHandler(OtaResponse? otaResponse);
        public event OtaEventHandler? OnOtaEvent = null;
        #endregion

        #region 构造函数
        public XiaoZhiAgent() { }
        #endregion

        public async Task Start()
        {
            // 1. 首先进行OTA检查
            await CheckOtaUpdate();

            // 2. 根据OTA响应决定WebSocket连接参数
            string wsUrl = _latestOtaResponse?.WebSocket?.Url ?? _wsUrl;
            string token = _latestOtaResponse?.WebSocket?.Token ?? _token;

            LogConsole.InfoLine($"使用WebSocket URL: {wsUrl}");
            LogConsole.InfoLine($"使用Token: {token}");

            // 3. 启动WebSocket连接
            _chatService = new Services.Chat.ChatService(wsUrl, token, _deviceId);
            _chatService.OnMessageEvent += ChatService_OnMessageEvent;
            if (Global.IsAudio)
                _chatService.OnAudioEvent += ChatService_OnAudioEvent;
            _chatService.Start();

            // 4. 初始化音频服务
            if (Global.IsAudio)
            {
                if (_audioService == null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        LogConsole.InfoLine("当前操作系统是 Windows");
                        _audioService = new AudioWaveService();
                    }
                    else
                    {
                        _audioService = new AudioPortService();
                    }

                }
                if (_audioService != null)
                    _audioService.OnPcmAudioEvent += AudioService_OnPcmAudioEvent;
            }
        }
        public async Task Restart()
        {
            // 保存当前的音频服务引用，以便重新设置事件处理
            var currentAudioService = _audioService;
            
            // 如果存在音频服务，先移除事件处理器
            if (currentAudioService != null)
            {
                // 移除事件处理器，避免重复订阅
                currentAudioService.OnPcmAudioEvent -= AudioService_OnPcmAudioEvent;
            }
            
            // 释放现有资源
            _chatService?.Dispose();
            _otaService?.Dispose();
            
            // 重置录音取消令牌
            _recordingCts?.Cancel();
            _recordingCts?.Dispose();
            _recordingCts = null;
            
            // 重新启动服务
            await Start();
            
            // 如果音频服务实例没有变化，则确保录音状态正确
            if (currentAudioService != null && _audioService == currentAudioService)
            {
                // 确保录音状态正确
                if (_audioService.IsRecording)
                {
                    _audioService.StopRecording();
                }
            }
            
            LogConsole.InfoLine("重启完成");
        }

        /// <summary>
        /// 执行OTA检查
        /// </summary>
        /// <returns></returns>
        public async Task<OtaResponse?> CheckOtaUpdate()
        {
            try
            {
                LogConsole.InfoLine("开始OTA检查...");

                // 初始化OTA服务
                _otaService ??= new Services.OtaService(_userAgent, _deviceId, _clientId);

                // 创建OTA请求
                var otaRequest = _otaService.CreateDefaultOtaRequest(_currentVersion, "", 
                    "xiaozhi-sharp", "xiaozhi-sharp-client");

                // 发送OTA请求
                _latestOtaResponse = await _otaService.CheckOtaAsync(_otaUrl, otaRequest);

                // 触发OTA事件
                if (OnOtaEvent != null)
                    await OnOtaEvent(_latestOtaResponse);

                if (_latestOtaResponse != null)
                {
                    LogConsole.InfoLine("OTA检查完成，获取到服务器配置信息");
                    
                    // 显示激活信息
                    if (_latestOtaResponse.Activation != null)
                    {
                        LogConsole.InfoLine($"激活码: {_latestOtaResponse.Activation.Code}");
                        LogConsole.InfoLine($"激活消息: {_latestOtaResponse.Activation.Message}");
                    }

                    // 显示固件信息
                    if (_latestOtaResponse.Firmware != null)
                    {
                        LogConsole.InfoLine($"固件版本: {_latestOtaResponse.Firmware.Version}");
                        if (!string.IsNullOrEmpty(_latestOtaResponse.Firmware.Url))
                        {
                            LogConsole.InfoLine($"固件下载地址: {_latestOtaResponse.Firmware.Url}");
                        }
                    }

                    // 显示服务器时间信息
                    if (_latestOtaResponse.ServerTime != null)
                    {
                        LogConsole.InfoLine($"服务器时间: {DateTimeOffset.FromUnixTimeMilliseconds(_latestOtaResponse.ServerTime.Timestamp)}");
                        LogConsole.InfoLine($"时区: {_latestOtaResponse.ServerTime.Timezone}");
                    }

                    // 显示MQTT配置信息
                    if (_latestOtaResponse.Mqtt != null)
                    {
                        LogConsole.InfoLine($"MQTT服务器: {_latestOtaResponse.Mqtt.Endpoint}");
                        LogConsole.InfoLine($"MQTT客户端ID: {_latestOtaResponse.Mqtt.ClientId}");
                    }

                    // 显示WebSocket配置信息
                    if (_latestOtaResponse.WebSocket != null)
                    {
                        LogConsole.InfoLine($"WebSocket服务器: {_latestOtaResponse.WebSocket.Url}");
                    }
                }
                else
                {
                    LogConsole.InfoLine("OTA检查完成，使用默认配置");
                }

                return _latestOtaResponse;
            }
            catch (Exception ex)
            {
                LogConsole.ErrorLine($"OTA检查异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建包含WiFi信息的OTA请求
        /// </summary>
        /// <param name="ssid">WiFi网络名称</param>
        /// <param name="rssi">WiFi信号强度</param>
        /// <param name="channel">WiFi频道</param>
        /// <param name="ip">设备IP地址</param>
        /// <returns></returns>
        public async Task<OtaResponse?> CheckOtaUpdateWithWifi(string ssid, int rssi = -50, int channel = 1, string ip = "")
        {
            try
            {
                LogConsole.InfoLine($"开始OTA检查（WiFi: {ssid}）...");

                // 初始化OTA服务
                _otaService ??= new Services.OtaService(_userAgent, _deviceId, _clientId);

                // 创建包含WiFi信息的OTA请求
                var otaRequest = _otaService.CreateWifiOtaRequest(_currentVersion, "", 
                    "xiaozhi-sharp-wifi", "xiaozhi-sharp-wifi-client", ssid, rssi, channel, ip);

                // 发送OTA请求
                _latestOtaResponse = await _otaService.CheckOtaAsync(_otaUrl, otaRequest);

                // 触发OTA事件
                if (OnOtaEvent != null)
                    await OnOtaEvent(_latestOtaResponse);

                return _latestOtaResponse;
            }
            catch (Exception ex)
            {
                LogConsole.ErrorLine($"OTA检查异常: {ex.Message}");
                return null;
            }
        }
        private async Task AudioService_OnPcmAudioEvent(byte[] pcm)
        {
            byte[] opus = _audioOpusService.Encode(pcm);
            await _chatService.SendAudio(opus);
        }
        private async Task ChatService_OnAudioEvent(byte[] opus)
        {
            if (_audioService != null)
            {
                byte[] pcmData = _audioOpusService.Decode(opus);
                _audioService.AddOutSamples(pcmData);

                if(OnAudioPcmEvent!=null)
                    await OnAudioPcmEvent(pcmData);
            }

            if (OnAudioEvent != null)
                await OnAudioEvent(opus);
        }
        private async Task ChatService_OnMessageEvent(string type, string message)
        {
            //if (type == "answer_stop") {
            //    await StopRecording();
            //}
            if (OnMessageEvent != null)
                await OnMessageEvent(type, message);
        }
        public async Task ChatMessage(string message)
        {
            if (_chatService != null)
                await _chatService.ChatMessage(message);
        }
        public async Task ChatAbort()
        {
            if (_chatService != null)
                await _chatService.ChatAbort();
        }
        public async Task McpMessage(string message)
        {
            if (_chatService != null)
                await _chatService.McpMessage(message);
        }
        /// <summary>
        /// 开始录音
        /// </summary>
        /// <param name="type">auto\manual</param>
        /// <returns></returns>
        public async Task StartRecording(string type= "manual")
        {
            if (_audioService != null)
            {
               if (type == "auto")
                {
                    await _chatService.StartRecordingAuto();
                    
                    // 创建新的监听任务
                    _recordingCts?.Cancel(); // 如果已有任务，先取消
                    _recordingCts = new CancellationTokenSource();
                    var token = _recordingCts.Token;
                    
                    _ = Task.Run(async () =>
                    {
                        try 
                        {
                            while (!token.IsCancellationRequested) 
                            {
                                if (_audioService.VadCounter >= Global.VadThreshold)
                                {
                                    LogConsole.InfoLine($"VAD检测到静音，自动结束录音 (计数: {_audioService.VadCounter})");
                                    _audioService.StopRecording();
                                    await _chatService.StopRecording();
                                    break;
                                }
                                await Task.Delay(100, token); // 每0.1秒检查一次
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            // 任务被取消，正常退出
                        }
                        catch (Exception ex)
                        {
                            LogConsole.ErrorLine($"VAD监听任务异常: {ex.Message}");
                        }
                    }, token); 
                }
                else
                {
                    await _chatService.StartRecording();
                }
                _audioService.StartRecording();
            }
        }
        public async Task StopRecording()
        {
            if (_audioService != null)
            {
                // 取消VAD监听任务
                _recordingCts?.Cancel();
                _recordingCts?.Dispose();
                _recordingCts = null;
                
                _audioService.StopRecording();
                await _chatService.StopRecording();
            }
        }
        
        /// <summary>
        /// 释放资源，在应用程序关闭时调用
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                // 停止VAD监听任务
                _recordingCts?.Cancel();
                _recordingCts?.Dispose();
                _recordingCts = null;
                
                // 停止录音
                if (_audioService != null && _audioService.IsRecording)
                {
                    _audioService.StopRecording();
                }
                
                // 释放音频服务
                if (_audioService is IDisposable disposableAudioService)
                {
                    disposableAudioService.Dispose();
                }
                
                // 释放WebSocket连接
                _chatService?.Dispose();
                
                // 释放OTA服务
                _otaService?.Dispose();
                
                // 释放音频编码服务
                if (_audioOpusService is IDisposable disposableOpusService)
                {
                    disposableOpusService.Dispose();
                }
                
                // GC回收
                GC.SuppressFinalize(this);
            }
        }
    }
}
