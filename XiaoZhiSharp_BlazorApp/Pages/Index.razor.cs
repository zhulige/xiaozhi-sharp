using System.Threading.Tasks;

namespace XiaoZhiSharp_BlazorApp.Pages
{
    public partial class Index : IDisposable
    {
        private static XiaoZhiSharp.XiaoZhiAgent _agent;
        private static string OTA_VERSION_URL = "https://api.tenclass.net/xiaozhi/ota/";
        private static string WEB_SOCKET_URL = "wss://api.tenclass.net/xiaozhi/v1/";
        public static List<string> MessageList = new List<string>();
        public static string MyMessage = string.Empty;
        public static string Message = string.Empty;
        public static string Emotion = string.Empty;
        public static string EmotionText = string.Empty;

        public Index()
        {
            _agent = new XiaoZhiSharp.XiaoZhiAgent(OTA_VERSION_URL, WEB_SOCKET_URL, "");
            _agent.OnMessageEvent += _agent_OnMessageEvent;
            _agent.Start();
        }

        private void _agent_OnMessageEvent(string message)
        {
            dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(message);
            if (msg != null)
            {
                if (msg.type == "stt") {
                    MyMessage = msg.text;
                    _ = _agent.StopRecording();
                }
                if (msg.type == "llm") {
                    Emotion = msg.emotion;
                    EmotionText = msg.text;
                }
                if (msg.type == "tts")
                {
                    if (msg.state == "sentence_start" || msg.state == "sentence_end")
                    {
                        Message = msg.text;
                        //_ = _agent.StopRecording();
                    }
                    if (msg.state == "stop")
                    {
                        //_ = _agent.StopRecording();
                        _ = _agent.StartRecording("auto");
                    }
                }
            }
            MessageList.Add(message);
        }

        public void Dispose()
        {
            _agent.Stop();
        }

        public async Task SendMessage()
        {
            if (_agent != null)
                await _agent.Send_Listen_Detect(this.newMessage);
        }

        public async Task SendAudio()
        {
            if (_agent != null)
                await _agent.StartRecording("auto");
        }

    }
}
