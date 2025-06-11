using Microsoft.Extensions.Logging;
using XiaoZhiSharp_MauiBlazorApp.Services;

namespace XiaoZhiSharp_MauiBlazorApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // 配置平台特定的外观
            builder.ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                // Android 特定配置
                Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("CustomWebView", (handler, view) =>
                {
                    // 设置WebView背景透明
                    handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                });
#elif WINDOWS
                // Windows 特定配置
                Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("CustomWebView", (handler, view) =>
                {
                    // Windows WebView配置
                    handler.PlatformView.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
                });
#endif
            });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<XiaoZhi_AgentService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
