using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Text;
using XiaoZhiSharp.Services;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp
{
    public class WebSocketService
    {
        // 事件
        public delegate void MessageEventHandler(string message);
        public delegate void AudioEventHandler(byte[] opus);
        public event MessageEventHandler? OnMessageEvent = null;
        public event AudioEventHandler? OnAudioEvent = null;

        // 属性
        public string WEB_SOCKET_URL { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";
        public string TOKEN { get; set; } = "test-token";
        public string? SessionId { get; set; }
        public string DeviceId { get; set; }
        public List<string>? MessageList { get; set; } = new List<string>();
        
        // 私有资源
        private ClientWebSocket _webSocket;
        private Uri _serverUri;

        // 构造函数
        public WebSocketService(string url,string token)
        {
            WEB_SOCKET_URL = url;
            TOKEN = token;

            // 获取 MAC 地址
            DeviceId = Utils.SystemInfo.GetMacAddress();

            // 初始化 WebSocket
            _serverUri = new Uri(WEB_SOCKET_URL);
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Authorization", "Bearer " + TOKEN);
            _webSocket.Options.SetRequestHeader("Protocol-Version", "1");
            _webSocket.Options.SetRequestHeader("Device-Id", DeviceId);
            _webSocket.Options.SetRequestHeader("Client-Id", Guid.NewGuid().ToString());
            _webSocket.ConnectAsync(_serverUri, CancellationToken.None);

            LogConsole.WriteLine($"WebSocketUrl：{WEB_SOCKET_URL}");
            LogConsole.WriteLine("WebSocket 初始化完成");
            LogConsole.Write("WebSocket 连接中...");
            while (_webSocket.State != WebSocketState.Open){
                Console.Write(".");
                Thread.Sleep(100);
            }
            Console.WriteLine("");
            LogConsole.WriteLine("WebSocket 连接成功 WebSocket.State:" + _webSocket.State.ToString());

            // WebSocket 接收消息
            Task.Run(async () =>
            {
                await ReceiveMessagesAsync();
            });

            // WebSocket 重连
            Task.Run(async () =>
            {
                if (_webSocket.State != WebSocketState.Open) {
                    await _webSocket.ConnectAsync(_serverUri, CancellationToken.None);
                }
                await Task.Delay(1000);
            });
        }

        /// <summary>
        /// 接收WebSocket消息
        /// </summary>
        /// <returns></returns>
        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024];
            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (!string.IsNullOrEmpty(message))
                        {
                            LogConsole.ReceiveLine($"WebSocket 接收到消息: {message}");

                            // 触发事件
                            if (OnMessageEvent != null)
                            {
                                OnMessageEvent(message);
                            }

                            // 解析消息
                            if (message.Contains("session_id"))
                            {
                                dynamic? json = JsonConvert.DeserializeObject<dynamic>(message);
                                SessionId = (string)json.session_id;
                            }
                        }
                    }
                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        //await _audioService.OpusPlayEnqueue(buffer);
                        //if (IsDebug)
                        //    Console.WriteLine($"WebSocket 接收到语音: {buffer.Length}");

                        // 触发事件
                        if (OnAudioEvent != null)
                        {
                            OnAudioEvent(buffer);
                        }
                    }
                    await Task.Delay(60);
                }
            }
            catch (Exception ex)
            {
                LogConsole.ErrorLine($"WebSocket 接收消息时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送WebSocket消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessageAsync(string message)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    
                LogConsole.SendLine($"WebSocket 发送的消息: {message}");
                
            }
        }

        /// <summary>
        /// 发送WebSocket语音包Opus
        /// </summary>
        /// <param name="opus"></param>
        /// <returns></returns>
        public async Task SendOpusAsync(byte[] opus)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(opus), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }

        /// <summary>
        /// WebSocket 连接打开
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync()
        {
            if(_webSocket.State!=WebSocketState.Open)
                await _webSocket.ConnectAsync(_serverUri, CancellationToken.None);
        }

        /// <summary>
        /// WebSocket 连接关闭
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            if (_webSocket.State == WebSocketState.Open)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
        }
    }
}
