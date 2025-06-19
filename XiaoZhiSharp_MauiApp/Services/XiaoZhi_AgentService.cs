using Android.DeviceLock;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XiaoZhiSharp;

namespace XiaoZhiSharp_MauiApp.Services
{
    public class XiaoZhi_AgentService : INotifyPropertyChanged, IDisposable
    {
        #region 属性
        private readonly XiaoZhiAgent _agent;
        public XiaoZhiAgent Agent => _agent;
        private string _questionMessae = "";
        public string QuestionMessae
        {
            get => _questionMessae;
            set
            {
                if (_questionMessae != value)
                {
                    _questionMessae = value;
                    OnPropertyChanged(nameof(QuestionMessae));
                }
            }
        }
        private string _answerMessae = "";
        public string AnswerMessae
        {
            get => _answerMessae;
            set
            {
                if (_answerMessae != value)
                {
                    _answerMessae = value;
                    OnPropertyChanged(nameof(AnswerMessae));
                }
            }
        }
        #endregion
        public XiaoZhi_AgentService()
        {
            _agent = new XiaoZhiAgent();
            _agent.DeviceId = Global.DeviceId;
            _agent.OnMessageEvent += Agent_OnMessageEvent;
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                //_agent.AudioService = new Services.AudioService();
            }
            _ = _agent.Start();
        }

        private Task Agent_OnMessageEvent(string type, string message)
        {
            switch (type.ToLower())
            {
                case "question":
                    QuestionMessae = message;
                    break;
                case "answer":
                    AnswerMessae = message;
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
