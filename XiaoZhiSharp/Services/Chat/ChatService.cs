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
    public class ChatService: IDisposable
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
        public WebSocketState ConnectState { get { return _webSocket?.State ?? WebSocketState.None; } }
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

            //Task.Run(async () =>
            //{
            //    while (_webSocket.State == WebSocketState.Open)
            //    {
            //        //await SendMessageAsync(XiaoZhi_Protocol.Heartbeat());
            //        await SendMessageAsync(XiaoZhi_Protocol.Listen_Detect(""));
            //        await Task.Delay(10000);
            //    }
            //});
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
                                    if (OnMessageEvent != null)
                                        await OnMessageEvent("mcp", Newtonsoft.Json.JsonConvert.SerializeObject(msg.payload));
                                }
                                // 问
                                if (msg.type == "stt")
                                {
                                    if (OnMessageEvent != null)
                                        await OnMessageEvent("question", System.Convert.ToString(msg.text));
                                }
                                // 答
                                if (msg.type == "tts")
                                {
                                    if (msg.state == "sentence_start")
                                    {
                                        if (OnMessageEvent != null)
                                            await OnMessageEvent("answer", System.Convert.ToString(msg.text));
                                    }

                                    if (msg.state == "stop")
                                    {
                                        if (OnMessageEvent != null)
                                            await OnMessageEvent("answer_stop", "");
                                    }
                                }
                                // 情感
                                if (msg.type == "llm")
                                {
                                    if (OnMessageEvent != null)
                                        await OnMessageEvent("emotion", System.Convert.ToString(msg.emotion));
                                    if (OnMessageEvent != null)
                                        await OnMessageEvent("emotion_text", System.Convert.ToString(msg.text));
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
                LogConsole.SendLine($"{TAG} {message}");
            }
        }
        private async Task SendAudioAsync(byte[] opus)
        {
            if (_webSocket == null)
                return;

            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(opus), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
        public async Task SendAudio(byte[] audio)
        {
            await SendAudioAsync(audio);
        }
        public async Task ChatAbort()
        {
            await SendMessageAsync(XiaoZhi_Protocol.Abort());
        }
        public async Task ChatMessage(string message)
        {
            await ChatAbort();
            await SendMessageAsync(XiaoZhi_Protocol.Listen_Detect(message));
        }
        public async Task McpMessage(string message)
        {
            await SendMessageAsync(XiaoZhi_Protocol.Mcp(message, _sessionId));
        }
        public async Task StartRecording()
        {
            await ChatAbort();
            await SendMessageAsync(XiaoZhi_Protocol.Listen_Start("", "manual"));
        }
        public async Task StartRecordingAuto()
        {
            await ChatAbort();
            await SendMessageAsync(XiaoZhi_Protocol.Listen_Start("", "auto"));
        }
        public async Task StopRecording()
        {
            await SendMessageAsync(XiaoZhi_Protocol.Listen_Stop(_sessionId));
        }
        public void Dispose()
        {
            _webSocket.Dispose();
        }
    }
}
