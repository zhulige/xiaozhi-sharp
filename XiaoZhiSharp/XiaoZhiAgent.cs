using System.Collections.Generic;
using System.Threading.Tasks;
using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Services;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp
{
    public class XiaoZhiAgent
    {
        private string _otaUrl { get; set; } = "https://api.tenclass.net/xiaozhi/ota/";
        private string _wsUrl { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";
        private string _token { get; set; } = "test-token";
        private string _deviceId { get; set; } = SystemInfo.GetMacAddress();
        private Services.Chat.ChatService? _chatService = null;
        private Services.AudioWaveService? _audioService = null;
        private Services.AudioOpusService _audioOpusService = new Services.AudioOpusService();

        #region 属性
        #endregion

        #region 事件
        public delegate Task MessageEventHandler(string type, string message);
        public event MessageEventHandler? OnMessageEvent = null;

        public delegate Task AudioEventHandler(byte[] opus);
        public event AudioEventHandler? OnAudioEvent = null;
        #endregion

        #region 构造函数
        public XiaoZhiAgent() { }
        #endregion

        public async Task Start()
        {
            _chatService = new Services.Chat.ChatService(_wsUrl, _token, _deviceId);
            _chatService.OnMessageEvent += ChatService_OnMessageEvent;
            _chatService.OnAudioEvent += ChatService_OnAudioEvent;
            _chatService.Start();

            if (Global.IsAudio)
            {
                _audioService = new AudioWaveService();
                _audioService.OnPcmAudioEvent += AudioService_OnPcmAudioEvent;
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
            }

            if (OnAudioEvent != null)
                await OnAudioEvent(opus);
        }
        private async Task ChatService_OnMessageEvent(string type, string message)
        {
            if (OnMessageEvent != null)
                await OnMessageEvent(type, message);
        }
        public async Task ChatMessage(string message)
        {
            if (_chatService != null)
                await _chatService.ChatMessage(message);
        }
        public async Task McpMessage(string message)
        {
            if (_chatService != null)
                await _chatService.McpMessage(message);
        }

        /// <summary>
        /// 开始录音
        /// </summary>
        public async Task StartRecording()
        {
            if (_audioService != null)
            {
                await _chatService.StartRecording();
                _audioService.StartRecording();
            }
        }
        /// <summary>
        /// 结束录音
        /// </summary>
        public async Task StopRecording()
        {
            if (_audioService != null)
            {
                _audioService.StopRecording();
                await _chatService.StopRecording();
            }
        }
    }
}
