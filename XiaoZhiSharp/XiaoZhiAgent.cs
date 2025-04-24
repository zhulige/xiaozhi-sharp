using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XiaoZhiSharp.Services;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp
{
    public class XiaoZhiAgent
    {
        public string OTA_VERSION_URL { get; set; } = "https://api.tenclass.net/xiaozhi/ota/";
        public string WEB_SOCKET_URL { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";
        public string TOKEN { get; set; } = "test-token";
        public string? MAC_ADDR { get; set; } = Utils.SystemInfo.GetMacAddress();
        public bool IsLogWrite { get { return LogConsole.IsWrite; } set { LogConsole.IsWrite = value; } }
        public bool IsAudio { get; set; } = true;
        public bool IsOTA { get; set; } = true;
        public Protocols.IotThingsProtocol? IotThings { get; set; } = null;

        public delegate void MessageEventHandler(string message);
        public delegate void AudioEventHandler(byte[] opus);
        public delegate void IotEventHandler(string message);
        public event MessageEventHandler? OnMessageEvent = null;
        public event AudioEventHandler? OnAudioEvent = null;
        public event IotEventHandler? OnIotEvent = null;


        private OtaService? _otaService = null;
        private OtaServiceXZ? _otaServiceXZ = null;
        private WebSocketService? _webSocketService = null;
        private AudioService? _audioService = null;
        private Thread? _sendOpusthread = null;

        public XiaoZhiAgent(string otaUrl, string wsUrl, string mac = "")
        {
            OTA_VERSION_URL = otaUrl;
            WEB_SOCKET_URL = wsUrl;
            if (!string.IsNullOrEmpty(mac))
                MAC_ADDR = mac;
        }

        public void Start()
        {
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //{
            if(IsAudio)
                _audioService = new AudioService();
            //}
            if (IsOTA)
            {
                LogConsole.WriteLine($"IsOTA{IsOTA}");
                if (OTA_VERSION_URL.Contains("tenclass"))
                {
                    _otaServiceXZ = new OtaServiceXZ(OTA_VERSION_URL, MAC_ADDR);
                }
                else
                {
                    _otaService = new OtaService(OTA_VERSION_URL, MAC_ADDR, "web_test_client");
                }

            }
            // 小智 WebSocket 客户端
            _webSocketService = new WebSocketService(WEB_SOCKET_URL, TOKEN, MAC_ADDR);
            _webSocketService.OnMessageEvent += WebSocketService_OnMessageEvent;
            _webSocketService.OnAudioEvent += WebSocketService_OnAudioEvent;

            //_ = Send_Hello();
            //_ = Send_Listen_Detect("你好");

            _sendOpusthread = new Thread(async () =>
            {
                while (true)
                {
                    if (_audioService == null)
                        return;

                    byte[]? opusData;
                    if (_audioService.OpusRecordEnqueue(out opusData))
                    {
                        if (opusData == null)
                            continue;
                        await _webSocketService.SendOpusAsync(opusData);
                    }

                    await Task.Delay(60);
                }
            });
            _sendOpusthread.Start();
        }

        public void Stop() {
            _audioService = null;
            _otaService = null;
            _otaServiceXZ = null;
            _webSocketService = null;
        }
        public void Restart() {
            Stop();
            Start();
        }
        private void WebSocketService_OnAudioEvent(byte[] opus)
        {
            if (_audioService != null)
                _audioService.OpusPlayEnqueue(opus);

            if (OnAudioEvent != null)
                OnAudioEvent(opus);
        }

        private void WebSocketService_OnMessageEvent(string message)
        {
            if (OnMessageEvent != null)
                OnMessageEvent(message);
        }

        public async Task SendMessage(string message) {
            await Send_Listen_Detect(message);
        }

        public async Task StartRecording(string mode)
        {
            await Send_Listen_Start(mode);
        }

        public async Task StopRecording()
        {
            await Send_Listen_Stop();
        }

        #region 协议
        public async Task Send_Hello()
        {
            if(_webSocketService!=null)
                await _webSocketService.SendMessageAsync(Protocols.WebSocketProtocol.Hello());
        }

        public async Task IotInit(string iotjson) 
        {
            if (_webSocketService != null && _audioService != null) 
            {
                //Console.WriteLine("生成的设备描述JSON：\n" + iotjson);
                await _webSocketService.SendMessageAsync(iotjson);
            }
        }

        public async Task IotState(string statejson)
        {
            if (_webSocketService != null && _audioService != null)
            {
                await _webSocketService.SendMessageAsync(statejson);
            }
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
