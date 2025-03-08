using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XiaoZhiSharp.Services;

namespace XiaoZhiSharp
{
    public class XiaoZhiAgent
    {
        public string OTA_VERSION_URL { get; set; } = "https://api.tenclass.net/xiaozhi/ota/";
        public string WEB_SOCKET_URL { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";
        public string? MAC_ADDR { get; set; } = Utils.SystemInfo.GetMacAddress();

        public delegate void MessageEventHandler(string message);
        public delegate void AudioEventHandler(byte[] opus);
        public event MessageEventHandler? OnMessageEvent = null;
        public event AudioEventHandler? OnAudioEvent = null;

        private OtaService? _xiaoZhiOtaClient = null;
        private WebSocketService? _webSocketService = null;
        private AudioService? _audioService = null;

        public XiaoZhiAgent(string otaUrl, string wsUrl, string mac = "")
        {
            OTA_VERSION_URL = otaUrl;
            WEB_SOCKET_URL = wsUrl;
            if (!string.IsNullOrEmpty(mac))
                MAC_ADDR = mac;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //Console.WriteLine("当前操作系统是 Windows，执行 Windows 相关代码。");
                _audioService = new AudioService();
            }

            _xiaoZhiOtaClient = new OtaService(OTA_VERSION_URL, MAC_ADDR);
            // 小智 WebSocket 客户端
            _webSocketService = new WebSocketService(WEB_SOCKET_URL);
            _webSocketService.OnMessageEvent += XiaoZhiWSClient_OnMessageEvent;
            _webSocketService.OnAudioEvent += XiaoZhiWSClient_OnAudioEvent;

            _ = Send_Hello();
            _ = Send_Listen_Detect("你好");



            Task.Run(async () =>
            {
                while (true)
                {
                    if (_audioService == null)
                        return;

                    byte[] opusData;
                    if (_audioService._opusRecordPackets.TryDequeue(out opusData))
                        await _webSocketService.SendOpusAsync(opusData);

                    await Task.Delay(60);
                }
            });

        }

        private void XiaoZhiWSClient_OnAudioEvent(byte[] opus)
        {
            if (_audioService != null)
                _audioService.OpusPlayEnqueue(opus);

            if (OnAudioEvent != null)
                OnAudioEvent(opus);
        }

        private void XiaoZhiWSClient_OnMessageEvent(string message)
        {
            if (OnMessageEvent != null)
                OnMessageEvent(message);
        }

        #region 协议
        /// <summary>
        /// 
        /// </summary>
        public async Task Send_Hello()
        {
            if(_webSocketService!=null)
                await _webSocketService.SendMessageAsync(Protocols.WebSocketProtocol.Hello());
        }
        public async Task Send_Listen_Detect(string text)
        {
            if (_webSocketService != null)
                await _webSocketService.SendMessageAsync(Protocols.WebSocketProtocol.Listen_Detect(text));
        }
        public async Task Send_Listen_Start(string mode)
        {
            if (_webSocketService != null && _audioService!=null)
            {
                await _webSocketService.SendMessageAsync(Protocols.WebSocketProtocol.Listen_Start(_webSocketService.SessionId, mode));
                _audioService.StartRecording();
            }
        }
        public async Task Send_Listen_Stop()
        {

            if (_webSocketService != null && _audioService != null)
            {
                await _webSocketService.SendMessageAsync(Protocols.WebSocketProtocol.Listen_Stop(_webSocketService.SessionId));
                _audioService.StopRecording();
            }
        }

        #endregion
    }
}
