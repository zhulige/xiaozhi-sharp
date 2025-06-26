using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XiaoZhiSharp;
using XiaoZhiSharp.Utils;

namespace DuoDuo.Services
{
    public class AgentService
    {
        private readonly XiaoZhiAgent _agent;
        private readonly McpService _mcpService;

        #region 属性
        public XiaoZhiAgent Agent{ get { return _agent; } }
        public PageModels.MainPageModel MainPageModel { get; set; } = new PageModels.MainPageModel();
        #endregion

        public AgentService(McpService mcpService)
        {
            XiaoZhiSharp.Global.IsMcp = true;

            _mcpService = mcpService;

            _agent = new XiaoZhiAgent();
            _agent.DeviceId = Global.DeviceId;
            _agent.OnMessageEvent += Agent_OnMessageEvent;
#if ANDROID
            _agent.AudioService = new Services.AndroidAudioService();
#endif
            _ = _agent.Start();

            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                while (true)
                {
                    MainPageModel.StatusMessage = $"连接状态: {_agent.ConnectState}";
                    if (_agent.ConnectState != System.Net.WebSockets.WebSocketState.Open)
                    {
                        await _agent.Restart();
                        MainPageModel.StatusMessage = $"连接状态: Restart";
                        await Task.Delay(3000);
                    }
                    await Task.Delay(2000);
                }
            });
        }

        private async Task Agent_OnMessageEvent(string type, string message)
        {
            switch (type)
            {
                case "question":
                    MainPageModel.QuestionMessae = message;
                    break;
                case "answer":
                    MainPageModel.AnswerMessae = message;
                    break;
                case "emotion":
                    MainPageModel.Emotion = message;
                    break;
                case "emotion_text":
                    MainPageModel.EmotionText = message;
                    break;
                case "answer_stop":
                    break;
                case "audio_start":
                    MainPageModel.StatusAudio = "说话中...";
                    break;
                case "audio_stop":
                    MainPageModel.StatusAudio = "";
                    break;
                case "mcp":
                    string resultMessage = await _mcpService.McpMessageHandle(message);
                    if(!string.IsNullOrEmpty(resultMessage))
                        await _agent.McpMessage(resultMessage);
                    break;
                default:
                    break;
            }
        }
    }
}
