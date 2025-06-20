using Microsoft.Extensions.Logging;
using XiaoZhiSharp_MauiApp.Services;

namespace XiaoZhiSharp_MauiApp
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<XiaoZhi_AgentService>();
            
            // 注册摄像头服务
#if ANDROID
            builder.Services.AddSingleton<ICameraService, Platforms.Android.Services.CameraService>();
#elif MACCATALYST
            builder.Services.AddSingleton<ICameraService, Platforms.MacCatalyst.Services.CameraService>();
#endif
            
            return builder.Build();
        }
    }
}
