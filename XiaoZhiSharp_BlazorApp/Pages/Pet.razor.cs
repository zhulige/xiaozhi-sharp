using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
                _agent = new XiaoZhiSharp.XiaoZhiAgent(OTA_VERSION_URL, WEB_SOCKET_URL, "");
                _agent.OnMessageEvent += _agent_OnMessageEvent;
                _agent.Start();
            }
        }

        public Pet()
        {
            
        }

        

        public void Dispose()
        {
            _agent.Stop();
        }

        public async Task SendMessage()
        {
            if (_agent != null)
            {
                await _agent.Send_Listen_Detect(this.newMessage);
                this.newMessage = string.Empty;
            }
        }

        public async Task SendAudio()
        {
            if (_agent != null)
                await _agent.StartRecording("auto");
        }

    }
}
