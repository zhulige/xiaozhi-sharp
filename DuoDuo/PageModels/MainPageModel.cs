using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuoDuo.PageModels
{
    public class MainPageModel : INotifyPropertyChanged
    {
        private string _statusMessage = "启动中...";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        private string _questionMessage = "";
        public string QuestionMessae
        {
            get => _questionMessage;
            set
            {
                if (_questionMessage != value)
                {
                    _questionMessage = value;
                    OnPropertyChanged(nameof(QuestionMessae));
                }
            }
        }

        private string _answerMessage = "";
        public string AnswerMessae
        {
            get => _answerMessage;
            set
            {
                if (_answerMessage != value)
                {
                    _answerMessage = value;
                    OnPropertyChanged(nameof(AnswerMessae));
                }
            }
        }

        private string _emotion = "normal";
        public string Emotion
        {
            get => _emotion;
            set
            {
                if (_emotion != value)
                {
                    _emotion = value;
                    OnPropertyChanged(nameof(Emotion));
                }
            }
        }

        private string _emotionText = "😊";
        public string EmotionText
        {
            get => _emotionText;
            set
            {
                if (_emotionText != value)
                {
                    _emotionText = value;
                    OnPropertyChanged(nameof(EmotionText));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}