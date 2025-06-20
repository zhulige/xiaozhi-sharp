using XiaoZhiSharp_MauiApp.Services;

namespace XiaoZhiSharp_MauiApp
{
    public partial class SettingsPage : ContentPage
    {
        private readonly XiaoZhi_AgentService _agentService;
        private System.Timers.Timer? _refreshTimer;

        public SettingsPage(XiaoZhi_AgentService agentService)
        {
            InitializeComponent();
            _agentService = agentService;
            BindingContext = _agentService;
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            StartRefreshTimer();
            UpdateDebugLogsDisplay();
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopRefreshTimer();
        }
        
        private void StartRefreshTimer()
        {
            if (_refreshTimer == null)
            {
                _refreshTimer = new System.Timers.Timer(1000); // 每秒刷新一次
                _refreshTimer.Elapsed += async (sender, e) =>
                {
                    if (_agentService.IsDebugMode)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            // 更新调试日志显示
                            UpdateDebugLogsDisplay();
                        });
                    }
                };
                _refreshTimer.Start();
            }
        }
        
        private void UpdateDebugLogsDisplay()
        {
            // 将日志倒序显示，最新的在最上面
            if (DebugLogsCollectionView != null)
            {
                DebugLogsCollectionView.ItemsSource = _agentService.DebugLogs.Reverse();
            }
        }
        
        private void StopRefreshTimer()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }

        private async void OnSaveSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                _agentService.AddDebugLog("保存设置中...");
                _agentService.SaveSettings();
                _agentService.AddDebugLog("设置已保存到本地存储");
                
                // 添加视觉反馈
                var button = sender as Button;
                if (button != null)
                {
                    var originalText = button.Text;
                    button.Text = "✅ 已保存";
                    await Task.Delay(1500);
                    button.Text = originalText;
                }
                
                await DisplayAlert("成功", "设置已保存到本地存储\n\n注意：要使设置生效，请点击'应用并重连'按钮", "确定");
            }
            catch (Exception ex)
            {
                _agentService.AddDebugLog($"保存设置失败: {ex.Message}");
                await DisplayAlert("错误", $"保存设置失败: {ex.Message}", "确定");
            }
        }

        private async void OnApplySettingsClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await DisplayAlert("确认", "应用设置将重新连接服务器，是否继续？", "是", "否");
                if (result)
                {
                    // 显示进度
                    var button = sender as Button;
                    if (button != null)
                    {
                        button.IsEnabled = false;
                        button.Text = "⏳ 正在应用...";
                    }
                    
                    _agentService.AddDebugLog("开始应用新设置...");
                    _agentService.AddDebugLog($"新的服务器URL: {_agentService.ServerUrl}");
                    _agentService.AddDebugLog($"新的设备ID: {_agentService.DeviceId}");
                    _agentService.AddDebugLog($"新的OTA URL: {_agentService.OtaUrl}");
                    
                    await _agentService.ApplySettings();
                    
                    // 恢复按钮
                    if (button != null)
                    {
                        button.IsEnabled = true;
                        button.Text = "🔄 应用并重连";
                    }
                    
                    await DisplayAlert("成功", "设置已应用，连接已重新建立\n\n请查看调试日志确认连接状态", "确定");
                    
                    // 如果调试模式关闭，临时开启以显示连接状态
                    if (!_agentService.IsDebugMode)
                    {
                        _agentService.IsDebugMode = true;
                        await Task.Delay(3000); // 等待3秒让用户看到连接状态
                        _agentService.IsDebugMode = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _agentService.AddDebugLog($"应用设置失败: {ex.Message}");
                await DisplayAlert("错误", $"应用设置失败: {ex.Message}", "确定");
            }
        }

        private async void OnResetSettingsClicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("确认", 
                "是否恢复默认设置？\n\n默认值：\n" +
                "• 服务器URL: wss://api.tenclass.net/xiaozhi/v1/\n" +
                "• OTA地址: https://api.tenclass.net/xiaozhi/ota/\n" +
                "• MAC地址: " + Global.DeviceId + "\n" +
                "• VAD阈值: 40\n" +
                "• 调试模式: 关闭", 
                "是", "否");
                
            if (result)
            {
                _agentService.AddDebugLog("恢复默认设置...");
                _agentService.ResetSettings();
                _agentService.AddDebugLog("已恢复所有默认设置");
                
                // 刷新UI显示
                var temp = BindingContext;
                BindingContext = null;
                BindingContext = temp;
                
                await DisplayAlert("成功", "已恢复默认设置\n\n注意：要使设置生效，请点击'应用并重连'按钮", "确定");
            }
        }

        private async void OnManualOtaCheckClicked(object sender, EventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Text = "⏳ 检查中...";
                }
                
                // 确保调试模式开启以查看检查过程
                var wasDebugMode = _agentService.IsDebugMode;
                if (!wasDebugMode)
                {
                    _agentService.IsDebugMode = true;
                }
                
                await _agentService.ManualOtaCheck();
                
                // 恢复按钮
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Text = "🔄 手动检查OTA";
                }
                
                // 显示结果
                var message = "OTA检查完成\n\n";
                message += $"OTA状态: {_agentService.OtaStatus}\n";
                message += $"验证状态: {_agentService.ActivationStatus}\n";
                if (!string.IsNullOrEmpty(_agentService.ActivationCode) && _agentService.ShowActivationCode)
                {
                    message += $"验证码: {_agentService.ActivationCode}\n";
                }
                if (!string.IsNullOrEmpty(_agentService.LatestVersion))
                {
                    message += $"最新版本: {_agentService.LatestVersion}";
                }
                
                await DisplayAlert("OTA检查结果", message, "确定");
                
                // 如果原本调试模式是关闭的，等待几秒后关闭
                if (!wasDebugMode)
                {
                    await Task.Delay(3000);
                    _agentService.IsDebugMode = false;
                }
            }
            catch (Exception ex)
            {
                _agentService.AddDebugLog($"OTA检查异常: {ex.Message}");
                await DisplayAlert("错误", $"OTA检查失败: {ex.Message}", "确定");
            }
        }

        private void OnClearLogsClicked(object sender, EventArgs e)
        {
            _agentService.ClearDebugLogs();
            UpdateDebugLogsDisplay();
            _agentService.AddDebugLog("调试日志已清空");
        }
    }
} 