using XiaoZhiSharp_MauiApp.Services;
using System.Collections.Specialized;

namespace XiaoZhiSharp_MauiApp
{
    public partial class MainPage : ContentPage
    {
        private readonly XiaoZhi_AgentService _agentService;
        private System.Timers.Timer _updateTimer;
        private bool _isUpdatingUI = false;

        public MainPage(XiaoZhi_AgentService agentService)
        {
            InitializeComponent();
            _agentService = agentService;
            BindingContext = _agentService;

            // 监听聊天历史变化
            _agentService.ChatHistory.CollectionChanged += OnChatHistoryChanged;

            // 启动定时器更新UI（主要用于更新音频强度等实时数据）
            _updateTimer = new System.Timers.Timer(100);
            _updateTimer.Elapsed += OnTimerElapsed;
            _updateTimer.Start();
        }

        private void OnChatHistoryChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateChatMessages();
            });
        }

        private void UpdateChatMessages()
        {
            // 如果没有聊天记录，显示欢迎消息
            WelcomeFrame.IsVisible = _agentService.ChatHistory.Count == 0;

            // 清除现有的消息（除了欢迎消息）
            var messagesToRemove = ChatMessagesLayout.Children
                .Where(c => c != WelcomeFrame)
                .ToList();
            
            foreach (var message in messagesToRemove)
            {
                ChatMessagesLayout.Children.Remove(message);
            }

            // 添加所有聊天消息
            foreach (var message in _agentService.ChatHistory)
            {
                AddMessageToUI(message);
            }

            // 滚动到底部
            _ = ScrollToBottom();
        }

        private void AddMessageToUI(ChatMessage message)
        {
            // 时间标签
            var timeLabel = new Label
            {
                Text = message.Timestamp.ToString("HH:mm"),
                FontSize = 12,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 5)
            };
            ChatMessagesLayout.Children.Add(timeLabel);

            // 消息框架
            var messageFrame = new Frame
            {
                Padding = new Thickness(10, 8),
                CornerRadius = 10,
                HasShadow = false,
                Margin = new Thickness(0, 2)
            };

            var messageLabel = new Label
            {
                Text = message.Content,
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap
            };

            if (message.IsUser)
            {
                messageFrame.BackgroundColor = Color.FromHex("#dcf8c6");
                messageFrame.HorizontalOptions = LayoutOptions.End;
                messageFrame.Margin = new Thickness(50, 2, 0, 2);
                messageLabel.TextColor = Colors.Black;
            }
            else
            {
                messageFrame.BackgroundColor = Colors.White;
                messageFrame.HorizontalOptions = LayoutOptions.Start;
                messageFrame.Margin = new Thickness(0, 2, 50, 2);
                messageLabel.TextColor = Colors.Black;
            }

            messageFrame.Content = messageLabel;
            ChatMessagesLayout.Children.Add(messageFrame);
        }

        private async Task ScrollToBottom()
        {
            await Task.Delay(100); // 等待UI更新
            await ChatScrollView.ScrollToAsync(0, ChatMessagesLayout.Height, false);
        }

        private async void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_isUpdatingUI) return;
            
            _isUpdatingUI = true;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // 由于XiaoZhi_AgentService已经实现了INotifyPropertyChanged，
                // 它的属性变化会自动通知UI更新，所以这里什么都不需要做
                // 定时器主要是为了确保UI能及时响应
            });
            _isUpdatingUI = false;
        }

        private async void OnRecordingButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (_agentService.IsRecording)
                {
                    await _agentService.Agent.StopRecording();
                }
                else
                {
                    await _agentService.Agent.StartRecording("auto");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"录音操作失败: {ex.Message}", "确定");
            }
        }

        private async void OnStopChatClicked(object sender, EventArgs e)
        {
            try
            {
                await _agentService.Agent.ChatAbort();
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"停止对话失败: {ex.Message}", "确定");
            }
        }

        private async void OnMessageSendClicked(object sender, EventArgs e)
        {
            var message = MessageEntry.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(message))
            {
                MessageEntry.Text = string.Empty;
                await _agentService.Agent.ChatMessage(message);
                await ScrollToBottom();
            }
        }

        private void OnClearChatClicked(object sender, EventArgs e)
        {
            _agentService.ClearChatHistory();
            UpdateChatMessages();
        }

        private void OnChatPageClicked(object sender, TappedEventArgs e)
        {
            // 当前已在聊天页面
        }

        private async void OnSettingsPageClicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage(_agentService));
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
        }
    }
}
