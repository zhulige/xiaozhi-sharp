using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        private Services.IAudioService? _audioService = null;
        private Services.AudioOpusService _audioOpusService = new Services.AudioOpusService();

        #region 属性
        public string WsUrl
        {
            get { return _wsUrl; }
            set { _wsUrl = value; }
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

        #endregion

        #region 事件
        public delegate Task MessageEventHandler(string type, string message);
        public event MessageEventHandler? OnMessageEvent = null;

        public delegate Task AudioEventHandler(byte[] opus);
        public event AudioEventHandler? OnAudioEvent = null;

        public delegate Task AudioPcmEventHandler(byte[] pcm);
        public event AudioPcmEventHandler? OnAudioPcmEvent = null;
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
                _audioService.OnPcmAudioEvent += AudioService_OnPcmAudioEvent;
            }
        }
        public async Task Restart()
        {
            _chatService.Dispose();
            //_audioService
            await Start();
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
            if (type == "answer_stop") {
                await StopRecording();
            }
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
        public async Task StartRecording(string type= "auto")
        {
            if (_audioService != null)
            {
               if (type == "auto")
                {
                    await _chatService.StartRecordingAuto();
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
                _audioService.StopRecording();
                await _chatService.StopRecording();
            }
        }
    }
}
