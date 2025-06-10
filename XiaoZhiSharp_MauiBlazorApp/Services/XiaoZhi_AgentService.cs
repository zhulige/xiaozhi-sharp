using XiaoZhiSharp;

namespace XiaoZhiSharp_MauiBlazorApp.Services
{
    public class XiaoZhi_AgentService
    {
        private readonly XiaoZhiAgent _agent;

        public string QuestionMessae = "123";
        public string AnswerMessae = "456";
        public XiaoZhiAgent Agent
        {
            get { return _agent; }
        }

        public XiaoZhi_AgentService()
        {
            XiaoZhiSharp.Global.IsDebug = false;
            //XiaoZhiSharp.Global.IsAudio = false;
            _agent = new XiaoZhiAgent();
            _agent.DeviceId = Global.DeivceId;
            //_agent.WsUrl = "wss://coze.nbee.net/xiaozhi/v1/"; 
            _agent.OnMessageEvent += Agent_OnMessageEvent;
            if (DeviceInfo.Platform == DevicePlatform.Android) { 
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
            //LogConsole.InfoLine($"[{type}] {message}");
        }
    }
}
