using System.Threading.Tasks;

namespace XiaoZhiSharp_BlazorApp.Pages
{
    public partial class Pet : IDisposable
    {
        private static XiaoZhiSharp.XiaoZhiAgent _agent;
        private static string OTA_VERSION_URL = "https://api.tenclass.net/xiaozhi/ota/";
        private static string WEB_SOCKET_URL = "wss://api.tenclass.net/xiaozhi/v1/";
        public static List<string> MessageList = new List<string>();
        public static string MyMessage = " ";
        public static string Message = "我叫小智";
        public static string Emotion = " ";
        public static string EmotionText = "😊";

        private static bool _isFirstTime = true;

        protected override void OnInitialized()
        {
            if (_isFirstTime)
            {
                // 这是第一次启动
                _isFirstTime = false;
                // 在这里添加第一次启动时的逻辑
                OTA_VERSION_URL = Global.Configuration.GetSection("ConnectionStrings").GetSection("XIAOZHI_OTA_VERSION_URL").Value;
                WEB_SOCKET_URL = Global.Configuration.GetSection("ConnectionStrings").GetSection("XIAOZHI_WEB_SOCKET_URL").Value;
                _agent = new XiaoZhiSharp.XiaoZhiAgent(OTA_VERSION_URL, WEB_SOCKET_URL, "");
                _agent.OnMessageEvent += _agent_OnMessageEvent;
                _agent.Start();
            }
        }

        public Pet()
        {
            
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
                    if (Emotion == "crying") { 
                        _isA = true; 
                    }
                    //if (Emotion == "happy") { 
                    //    _isB = true; 
                    //}
                    //if (Emotion == "laughing") { 
                    //    _isC = true; 
                    //}
                    //if (Emotion == "crying") { _isD = true; }
                }
                if (msg.type == "tts")
                {
                    if (msg.state == "sentence_start" || msg.state == "sentence_end")
                    {
                        Message = msg.text;
                    }
                    if (msg.state == "stop")
                    {
                        _agent.StartRecording("auto");
                    }
                }
            }
            //MessageList.Add(message);
        }

        public void Dispose()
        {
            _agent.Stop();
        }

        public async Task SendMessage()
        {
            if (_agent != null)
            {
                await _agent.SendMessage(this._newMessage);
                this._newMessage = string.Empty;
            }
        }

        public async Task SendAudio()
        {
            if (_agent != null)
            { 
                if(_isAudioSwitchValue)
                    await _agent.StartRecording("auto");
                else
                    await _agent.StopRecording();
            }
                
        }

    }
}
