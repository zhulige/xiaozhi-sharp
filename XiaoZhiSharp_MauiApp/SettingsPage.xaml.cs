using XiaoZhiSharp_MauiApp.Services;

namespace XiaoZhiSharp_MauiApp
{
    public partial class SettingsPage : ContentPage
    {
        private readonly XiaoZhi_AgentService _agentService;

        public SettingsPage(XiaoZhi_AgentService agentService)
        {
            InitializeComponent();
            _agentService = agentService;
            BindingContext = _agentService;
        }

        private async void OnSaveSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                _agentService.SaveSettings();
                await DisplayAlert("成功", "设置已保存", "确定");
            }
            catch (Exception ex)
            {
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
                    await _agentService.ApplySettings();
                    await DisplayAlert("成功", "设置已应用，连接已重新建立", "确定");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"应用设置失败: {ex.Message}", "确定");
            }
        }

        private async void OnResetSettingsClicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("确认", "是否恢复默认设置？", "是", "否");
            if (result)
            {
                _agentService.ResetSettings();
                await DisplayAlert("成功", "已恢复默认设置", "确定");
            }
        }

        private async void OnManualOtaCheckClicked(object sender, EventArgs e)
        {
            try
            {
                await _agentService.ManualOtaCheck();
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"OTA检查失败: {ex.Message}", "确定");
            }
        }

        private void OnClearLogsClicked(object sender, EventArgs e)
        {
            _agentService.ClearDebugLogs();
        }
    }
} 