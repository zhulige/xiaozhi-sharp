using XiaoZhiSharp;

namespace XiaoZhiSharp_MauiBlazorApp.Services
{
    public class XiaoZhi_AgentService
    {
        private readonly XiaoZhiAgent _agent;

        public string QuestionMessae = "";
        public string AnswerMessae = "";
        public string Emotion = "normal";
        public XiaoZhiAgent Agent
        {
            get { return _agent; }
        }
        public int VadCounter
        {
            get { return _agent.AudioService?.VadCounter ?? 0; }
        }

        public XiaoZhi_AgentService()
        {
            XiaoZhiSharp.Global.VadThreshold = 40;
            XiaoZhiSharp.Global.IsDebug = false;
            //XiaoZhiSharp.Global.IsAudio = false;
            _agent = new XiaoZhiAgent();
            _agent.DeviceId = Global.DeivceId;
            //_agent.WsUrl = "wss://coze.nbee.net/xiaozhi/v1/"; 
            _agent.OnMessageEvent += Agent_OnMessageEvent;
            
            // 根据平台注册相应的音频服务
            if (DeviceInfo.Platform == DevicePlatform.Android) 
            { 
                _agent.AudioService = new Services.AudioService();
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                _agent.AudioService = new Services.AudioService();
            }
            
            _agent.Start();
        }

        private async Task Agent_OnMessageEvent(string type, string message)
        {
            if (type == "question")
                QuestionMessae = message;
            if (type == "answer")
                AnswerMessae = message;
            if (type == "emotion")
                Emotion = message;
            if (type == "answer_stop")
                await _agent.StartRecording("auto");

            //LogConsole.InfoLine($"[{type}] {message}");
        }
    }
}
