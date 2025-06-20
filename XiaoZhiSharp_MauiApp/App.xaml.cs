using Microsoft.Extensions.DependencyInjection;
using XiaoZhiSharp_MauiApp.Services;

namespace XiaoZhiSharp_MauiApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            
            // 添加窗口关闭事件处理
            window.Destroying += Window_Destroying;
            
            return window;
        }
        
        private void Window_Destroying(object? sender, EventArgs e)
        {
            // 在窗口关闭时，获取XiaoZhi_AgentService并释放
            var agentService = IPlatformApplication.Current?.Services?.GetService<XiaoZhi_AgentService>();
            if (agentService != null)
            {
                agentService.Dispose();
            }
        }
    }
}