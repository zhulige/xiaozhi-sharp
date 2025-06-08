using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Services.Chat
{
    public class ChatService
    {
        private string TAG = "小智";
        private string _wsUrl { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";
        private string? _token { get; set; } = "test-token";
        private string? _deviceId { get; set; }
        private string? _sessionId = "";
        // 首次连接
        private bool _isFirst = true;

        private ClientWebSocket? _webSocket = null;

        #region 属性

        #endregion

        #region 事件
        public delegate Task MessageEventHandler(string type, string message);
        public event MessageEventHandler? OnMessageEvent = null;

        public delegate Task AudioEventHandler(byte[] opus);
        public event AudioEventHandler? OnAudioEvent = null;
        #endregion

        #region 构造函数
        public ChatService(string wsUrl, string token,string deviceId)
        {
            _wsUrl = wsUrl;
            _token = token;
            _deviceId = deviceId;
        }
        #endregion

        public void Start()
        {
            Uri uri = new Uri(_wsUrl);
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Authorization", "Bearer " + _token);
            _webSocket.Options.SetRequestHeader("Protocol-Version", "1");
            _webSocket.Options.SetRequestHeader("Device-Id", _deviceId);
            _webSocket.Options.SetRequestHeader("Client-Id", Guid.NewGuid().ToString());
            _webSocket.ConnectAsync(uri, CancellationToken.None);
            LogConsole.InfoLine($"{TAG} 连接中...");

            Task.Run(async () =>
            {
                await ReceiveMessagesAsync();
            });
        }

        private async Task ReceiveMessagesAsync()
        {
            if (_webSocket == null)
                return;

            var buffer = new byte[1024 * 10];
            while (true)
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        // 首次
                        if (_isFirst)
                        {
                            _isFirst = false;
                            LogConsole.InfoLine($"{TAG} 连接成功");
                            await SendMessageAsync(XiaoZhi_Protocol.Hello());
                        }

                        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        byte[] messageBytes = new byte[result.Count];
                        Array.Copy(buffer, messageBytes, result.Count);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(messageBytes);
                            LogConsole.ReceiveLine($"{TAG} {message}");

                            if (!string.IsNullOrEmpty(message))
                            {
                                dynamic? msg = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(message);
                                if (msg == null)
                                {
                                    LogConsole.ErrorLine($"{TAG} 接收到的消息格式错误: {message}");
                                    continue;
                                }
                                _sessionId = msg.session_id;
                                if (msg.type == "mcp") {
                                    if (msg.payload.method == "initialize") {
                                        //await SendMessageAsync(XiaoZhi_Protocol.Mcp_Initialize_Receive(_sessionId));
                                    }
                                    if (OnMessageEvent != null)
                                        await OnMessageEvent("mcp", Newtonsoft.Json.JsonConvert.SerializeObject(msg.payload));
                                }
                            }

                        }

                        if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            // 触发事件
                            if (OnAudioEvent != null)
                               await OnAudioEvent(messageBytes);
                        }
                    }
                    catch (Exception ex)
                    {

                        LogConsole.ErrorLine($"{TAG} {ex.Message}");
                        break;
                    }
                }
            }
        }

        private async Task SendMessageAsync(string message)
        {
            if (_webSocket == null)
                return;

            if (_webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                if (Global.IsDebug)
                    LogConsole.SendLine($"{TAG} {message}");
            }
        }


        /// <summary>
        /// 打断消息
        /// </summary>
        /// <returns></returns>
        public async Task ChatAbort()
        {
            //await SendMessageAsync();
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ChatMessage(string message)
        {
            await SendMessageAsync(XiaoZhi_Protocol.Listen_Detect(message));
        }

        public async Task McpMessage(string message)
        {
            await SendMessageAsync(XiaoZhi_Protocol.Mcp(message, _sessionId));
        }

    }
}
