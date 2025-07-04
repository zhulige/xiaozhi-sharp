﻿using DuoDuo.Services;
using Camera.MAUI;
using Microsoft.Extensions.Logging;

namespace DuoDuo
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCameraView()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<McpService>();
            builder.Services.AddSingleton<CameraService>();
            builder.Services.AddSingleton<AgentService>();
            return builder.Build();
        }
    }
}
