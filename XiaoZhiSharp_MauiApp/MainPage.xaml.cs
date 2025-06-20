using XiaoZhiSharp_MauiApp.Services;
using System.Collections.Specialized;

namespace XiaoZhiSharp_MauiApp
{
    public partial class MainPage : ContentPage
    {
        private readonly XiaoZhi_AgentService _agentService;
        private readonly ICameraService? _cameraService;
        private System.Timers.Timer _updateTimer;
        private bool _isUpdatingUI = false;

        public MainPage(XiaoZhi_AgentService agentService, ICameraService? cameraService = null)
        {
            _cameraService = cameraService;
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

            // 创建消息内容容器
            var messageContainer = new VerticalStackLayout { Spacing = 8 };

            // 如果有图片，先添加图片
            if (!string.IsNullOrEmpty(message.ImagePath) && File.Exists(message.ImagePath))
            {
                var imageView = new Image
                {
                    Source = ImageSource.FromFile(message.ImagePath),
                    Aspect = Aspect.AspectFit,
                    MaximumHeightRequest = 200,
                    MaximumWidthRequest = 250,
                    BackgroundColor = Colors.LightGray
                };
                
                // 添加图片点击事件，可以查看大图
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await ShowImageFullscreen(message.ImagePath);
                imageView.GestureRecognizers.Add(tapGesture);
                
                messageContainer.Children.Add(imageView);
            }

            // 添加文本消息
            var messageLabel = new Label
            {
                Text = message.Content,
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap
            };
            messageContainer.Children.Add(messageLabel);

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

            messageFrame.Content = messageContainer;
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

        private async void OnCameraButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // 先检查是否有摄像头服务
                if (_cameraService == null)
                {
                    await DisplayAlert("提示", "摄像头功能不可用", "确定");
                    return;
                }

                // 检查设备支持
                if (!_cameraService.IsSupported)
                {
                    await DisplayAlert("提示", "设备不支持摄像头功能", "确定");
                    return;
                }

                // 检查权限
                if (!_cameraService.HasPermission)
                {
                    var granted = await _cameraService.RequestPermissionAsync();
                    if (!granted)
                    {
                        await DisplayAlert("提示", "需要摄像头权限才能使用此功能", "确定");
                        return;
                    }
                }

                try
                {
                    // 第一步：直接打开相机拍照
                    await DisplayAlert("拍照提示", "即将打开相机，请拍摄您要识别的内容", "确定");
                    
                    var imageData = await _cameraService.CapturePhotoAsync();
                    if (imageData == null || imageData.Length == 0)
                    {
                        await DisplayAlert("提示", "拍照失败或已取消", "确定");
                        return;
                    }

                    //XiaoZhiSharp.Services.ImageStorageService imageStorageService = new XiaoZhiSharp.Services.ImageStorageService();
                    //imageStorageService.PostImage("https://coze.nbee.net/image/v1/stream/1", "", "deviceId", "clientId", imageData);

                    // 第二步：保存照片并在聊天记录中显示
                    string imagePath = await SaveImageToLocal(imageData);
                    
                    // 第三步：拍照成功后询问问题（可选）
                    var question = await DisplayPromptAsync("拍照成功", 
                        "拍照成功！请输入您要询问关于这张图片的问题:\n(直接点击确定将使用默认问题)", 
                        "确定", "取消", 
                        "请描述这张图片的内容", 
                        maxLength: 200);

                    if (question == null) // 用户点击取消
                    {
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(question))
                    {
                        question = "请描述这张图片的内容"; // 默认问题
                    }

                    // 第四步：在聊天记录中显示用户的问题和图片
                    var userMessage = $"📷 {question}";
                    var imageMessage = new ChatMessage(userMessage, true) { ImagePath = imagePath };
                    _agentService.ChatHistory.Add(imageMessage);
                    await ScrollToBottom();

                    // 第五步：显示AI识别进度
                    var progressMessage = $"正在进行AI识别...\n问题: {question}";
                    var loadingTask = DisplayAlert("AI识别中", progressMessage, "请稍候");

                    try
                    {
                        // 第六步：调用AI识别服务（直接对已拍摄的图片进行识别）
                        var result = await _cameraService.ExplainImageAsync(imageData, question);

                        // 第七步：解析并显示结果
                        if (!string.IsNullOrEmpty(result))
                        {
                            // AI的回答
                            var aiResponse = $"根据您拍摄的图片，{ExtractAIResponse(result)}";

                            // 添加到聊天记录
                            await Task.Delay(300);
                            _agentService.ChatHistory.Add(new ChatMessage(aiResponse, false));
                            await ScrollToBottom();
                        }
                        else
                        {
                            await DisplayAlert("识别失败", "AI识别失败，请重试", "确定");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("错误", $"AI识别失败: {ex.Message}", "确定");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("错误", $"拍照过程出错: {ex.Message}", "确定");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"摄像头功能出错: {ex.Message}", "确定");
            }
        }

        private async Task<string> SaveImageToLocal(byte[] imageData)
        {
            try
            {
                // 创建应用文件夹
                var appDataPath = FileSystem.CacheDirectory;
                var imagesPath = Path.Combine(appDataPath, "CapturedImages");
                
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                // 生成文件名
                var fileName = $"camera_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filePath = Path.Combine(imagesPath, fileName);

                // 保存文件
                await File.WriteAllBytesAsync(filePath, imageData);
                
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存图片失败: {ex.Message}");
                return string.Empty;
            }
        }

        private string ExtractAIResponse(string jsonResult)
        {
            try
            {
                // 简单的JSON解析，提取AI回答
                if (jsonResult.Contains("\"success\": true"))
                {
                    // 如果有结果字段，提取结果
                    var start = jsonResult.IndexOf("\"result\":");
                    if (start >= 0)
                    {
                        start = jsonResult.IndexOf("\"", start + 9) + 1;
                        var end = jsonResult.IndexOf("\"", start);
                        if (end > start)
                        {
                            return jsonResult.Substring(start, end - start);
                        }
                    }
                    
                    // 如果有message字段，提取消息
                    start = jsonResult.IndexOf("\"message\":");
                    if (start >= 0)
                    {
                        start = jsonResult.IndexOf("\"", start + 10) + 1;
                        var end = jsonResult.IndexOf("\"", start);
                        if (end > start)
                        {
                            return jsonResult.Substring(start, end - start);
                        }
                    }
                    
                    return "AI识别成功，但未获取到具体描述。";
                }
                else
                {
                    // 提取错误消息
                    var start = jsonResult.IndexOf("\"message\":");
                    if (start >= 0)
                    {
                        start = jsonResult.IndexOf("\"", start + 10) + 1;
                        var end = jsonResult.IndexOf("\"", start);
                        if (end > start)
                        {
                            return $"识别失败: {jsonResult.Substring(start, end - start)}";
                        }
                    }
                    return "识别失败，请重试。";
                }
            }
            catch
            {
                return jsonResult; // 如果解析失败，返回原始结果
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

        private async Task ShowImageFullscreen(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    await DisplayAlert("提示", "图片文件不存在", "确定");
                    return;
                }

                // 创建全屏图片页面
                var fullscreenPage = new ContentPage
                {
                    Title = "查看图片",
                    BackgroundColor = Colors.Black
                };

                var imageView = new Image
                {
                    Source = ImageSource.FromFile(imagePath),
                    Aspect = Aspect.AspectFit,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand
                };

                // 添加点击关闭功能
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await Navigation.PopModalAsync();
                imageView.GestureRecognizers.Add(tapGesture);

                var closeButton = new Button
                {
                    Text = "关闭",
                    BackgroundColor = Colors.Gray,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.End,
                    Margin = new Thickness(0, 0, 0, 50)
                };
                closeButton.Clicked += async (s, e) => await Navigation.PopModalAsync();

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitionCollection
                    {
                        new RowDefinition(GridLength.Star),
                        new RowDefinition(GridLength.Auto)
                    }
                };

                grid.Children.Add(imageView);
                Grid.SetRow(imageView, 0);
                
                grid.Children.Add(closeButton);
                Grid.SetRow(closeButton, 1);

                fullscreenPage.Content = grid;
                await Navigation.PushModalAsync(fullscreenPage);
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"显示图片失败: {ex.Message}", "确定");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
        }
    }
}
