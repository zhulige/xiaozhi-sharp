using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_
{
    /// <summary>
    /// 
    /// </summary>
    public class WebSocketProtocol
    {
        // 1. 客户端连接Websocket服务器时需要携带以下 headers:
        // Authorization: Bearer<access_token>
        // Protocol-Version: 1
        // Device-Id: <设备MAC地址>
        // Client-Id: <设备UUID>

        // 2. 连接成功后,客户端发送hello消息:
        public static string Hello(string sessionId="")
        {
            string message = @"{
                ""type"": ""hello"",
                ""version"": 1,
                ""transport"": ""websocket"",
                ""audio_params"": {
                    ""format"": ""opus"",
                    ""sample_rate"": 24000,
                    ""channels"": 1,
                    ""frame_duration"": 60
                    },
                ""session_id"":""<会话ID>""
            }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ","");
            if(string.IsNullOrEmpty(sessionId))
                message = message.Replace(",\"session_id\":\"<会话ID>\"", "");
            else
                message = message.Replace("<会话ID>", sessionId);
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        // 3. 服务端响应hello消息:
        public static string Hello_Receive()
        {
            string message = @"{
                ""type"": ""hello"",
                ""transport"": ""websocket"",
                ""audio_params"": {
                    ""sample_rate"": <服务器采样率>
                }
            }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        // 消息类型

        // 1. 语音识别相关消息

        // 开始监听 监听模式: "auto": 自动停止 "manual": 手动停止 "realtime": 持续监听
        public static string Listen_Start(string? sessionId, string mode)
        {
            string message = @"{
                    ""session_id"": ""<会话ID>"",
                    ""type"": ""listen"",
                    ""state"": ""start"",
                    ""mode"": ""<监听模式>""
                }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            message = message.Replace("<会话ID>", sessionId);
            switch (mode)
            {
                case "realtime":
                    message = message.Replace("<监听模式>", "realtime");
                    break;
                case "manual":
                    message = message.Replace("<监听模式>", "manual");
                    break;
                default:
                    message = message.Replace("<监听模式>", "auto");
                    break;
            }
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        // 停止监听
        public static string Listen_Stop(string? sessionId)
        {
            string message = @"{
                    ""session_id"": ""<会话ID>"",
                    ""type"": ""listen"",
                    ""state"": ""stop""
                }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            message = message.Replace("<会话ID>", sessionId);
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        // 唤醒词检测
        public static string Listen_Detect(string text)
        {
            string message = @"{
                    ""type"": ""listen"",
                    ""state"": ""detect"",
                    ""text"": ""<唤醒词>""
                }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            message = message.Replace("<唤醒词>", text);
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        // 2. 语音合成相关消息

        // 服务端发送的TTS状态消息:
        // 状态类型:
        // "start": 开始播放
        // "stop": 停止播放  
        // "sentence_start": 新句子开始
        public static string TTS_Sentence_Start(string text,string? sessionId="")
        {
            string message = @"{
                    ""type"": ""tts"",
                    ""state"": ""sentence_start"",
                    ""text"": ""<文本内容>"",
                    ""session_id"": ""<会话ID>""
                }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            message = message.Replace("<文本内容>", text);
            if (string.IsNullOrEmpty(sessionId))
                message = message.Replace(",\"session_id\":\"<会话ID>\"", "");
            else
                message = message.Replace("<会话ID>", sessionId);
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        public static string TTS_Sentence_End(string text="", string? sessionId = "")
        {
            string message = @"{
                    ""type"": ""tts"",
                    ""state"": ""sentence_end"",
                    ""text"": ""<文本内容>"",
                    ""session_id"": ""<会话ID>""
                }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            if (string.IsNullOrEmpty(text))
                message = message.Replace(",\"text\":\"<文本内容>\"", "");
            else
                message = message.Replace("<文本内容>", text);
            if (string.IsNullOrEmpty(sessionId))
                message = message.Replace(",\"session_id\":\"<会话ID>\"", "");
            else
                message = message.Replace("<会话ID>", sessionId);
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        public static string TTS_Start()
        {
            string message = @"{
                    ""type"": ""tts"",
                    ""state"": ""start""
                }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        public static string TTS_Stop()
        {
            string message = @"{
                    ""type"": ""tts"",
                    ""state"": ""stop""
                }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        // 3. 中止消息
        public static string Abort()
        {
            string message = @"{
                ""session_id"": ""<会话ID>"",
                ""type"": ""abort"",
                ""reason"": ""wake_word_detected""
            }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        // 4. IoT设备相关消息

        // 设备描述
        public static string Device_Info()
        {
            string message = @"{
                ""session_id"": ""<会话ID>"",
                ""type"": ""iot"",
                ""descriptors"": <设备描述JSON>
            }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        // 设备状态
        public static string Device_Status()
        {
            string message = @"{
                ""session_id"": ""<会话ID>"",
                ""type"": ""iot"",
                ""states"": <状态JSON>
            }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        // 5. 情感状态消息
        // 服务端发送:
        public static string Emotion(string emo)
        {
            string message = @"{
                ""type"": ""llm"",
                ""emotion"": ""<情感类型>""
            }";
            message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
            //Console.WriteLine($"发送的消息: {message}");
            return message;
        }

        //二进制数据传输

        //- 音频数据使用二进制帧传输
        //- 客户端发送OPUS编码的音频数据
        //- 服务端返回OPUS编码的TTS音频数据

        //错误处理

        //当发生网络错误时，客户端会收到错误消息并关闭连接。客户端需要实现重连机制。

        //会话流程

        //1. 建立Websocket连接
        //2. 交换hello消息
        //3. 开始语音交互:
        //- 发送开始监听
        //- 发送音频数据
        //- 接收识别结果
        //- 接收TTS音频
        //4. 结束会话时关闭连接

    }
}