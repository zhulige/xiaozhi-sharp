using System;
using System.IO;

namespace XiaozhiAI.Services
{
    public static class Constants
    {
        // 音频参数
        public const int DEFAULT_SAMPLE_RATE = 24000;
        public const int DEFAULT_CHANNELS = 1;
        public const int DEFAULT_FRAME_DURATION = 60;
        
        // WebSocket消息类型
        public const string MSG_TYPE_HELLO = "hello";
        public const string MSG_TYPE_GOODBYE = "goodbye";
        public const string MSG_TYPE_TTS = "tts";
        public const string MSG_TYPE_LISTEN = "listen";
        public const string MSG_TYPE_IOT = "iot";
        public const string MSG_TYPE_ABORT = "abort";

        //VAD参数
        public const int VAD_SAMPLE_RATE = 16000;
        public const int VAD_FRAME_DURATION = 30;
        public const float VAD_THRESHOLD = 0.5f;
        public const int VAD_WINDOW_SIZE = 512;


        // TTS状态
        public const string TTS_STATE_IDLE = "idle";
        public const string TTS_STATE_START = "start";
        public const string TTS_STATE_SENTENCE_START = "sentence_start";
        public const string TTS_STATE_STOP = "stop";
        
        // 监听状态
        public const string LISTEN_STATE_START = "start";
        public const string LISTEN_STATE_STOP = "stop";

        // MQTT 配置
        public static class Mqtt
        {
            public const string BrokerAddress = "iot.dfrobot.com.cn";
            public const int BrokerPort = 1883;
            public const string ClientId = "XiaozhiAI_Client";
            public const string Username = "W7xR5OmHg";
            public const string Password = "ZnxR5OiHRz";
            public const string TopicSubscribe = "beUljnoHg";
            public const string TopicPublish = "beUljnoHg";
        }
    }
}