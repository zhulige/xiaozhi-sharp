using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace XiaoZhiSharp_MauiBlazorApp.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            // 获取主窗口并自定义标题栏
            var window = Application.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window != null)
            {
                var windowHandle = WindowNative.GetWindowHandle(window);
                var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow != null && AppWindowTitleBar.IsCustomizationSupported())
                {
                    var titleBar = appWindow.TitleBar;
                    
                    // 设置标题栏颜色为蓝色主题 - 使用Microsoft.UI.Colors
                    titleBar.BackgroundColor = ColorHelper.FromArgb(255, 74, 144, 226); // #4a90e2
                    titleBar.ForegroundColor = Microsoft.UI.Colors.White;
                    titleBar.InactiveBackgroundColor = ColorHelper.FromArgb(255, 91, 160, 242); // #5ba0f2
                    titleBar.InactiveForegroundColor = Microsoft.UI.Colors.White;
                    
                    // 按钮颜色
                    titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                    titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                    titleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(50, 255, 255, 255);
                    titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                    titleBar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(100, 255, 255, 255);
                    titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                    
                    titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                    titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.White;
                }
            }
        }
    }
}
