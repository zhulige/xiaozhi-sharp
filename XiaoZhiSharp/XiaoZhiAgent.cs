using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
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
            }
        }

        private async Task ChatService_OnAudioEvent(byte[] opus)
        {
            if (_audioService != null)
            {
                byte[] pcmData = _audioOpusService.ConvertOpusToPcm(opus);
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

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ChatMessage(string message)
        {
            if (_chatService != null)
                await _chatService.ChatMessage(message);
        }
    }
}
