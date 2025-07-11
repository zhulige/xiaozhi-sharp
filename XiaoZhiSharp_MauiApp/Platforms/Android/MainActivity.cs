﻿using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using System.Text;
using XiaoZhiSharp_MauiApp.Platforms.Android.Services;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Android.Runtime;
using Microsoft.Maui.ApplicationModel;

namespace XiaoZhiSharp_MauiApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int REQUEST_NOTIFICATION_PERMISSION = 1002;
        private const int REQUEST_IGNORE_BATTERY_OPTIMIZATIONS = 1003;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // 获取 Android ID 并格式化为 MAC 地址格式
            var androidId = Android.Provider.Settings.Secure.GetString(ContentResolver, Android.Provider.Settings.Secure.AndroidId);
            var formattedAndroidId = FormatAndroidIdToMacFormat(androidId);
            Global.DeviceId = formattedAndroidId;

            base.OnCreate(savedInstanceState);
            
            // 检查并请求通知权限（Android 13+）
            Task.Run(async () =>
            {
                await Task.Delay(3000); // 等待3秒，确保应用完全初始化
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        // 请求忽略电池优化
                        RequestIgnoreBatteryOptimizations();
                        
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                        {
                            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.PostNotifications) != Permission.Granted)
                            {
                                ActivityCompat.RequestPermissions(this, new[] { Android.Manifest.Permission.PostNotifications }, REQUEST_NOTIFICATION_PERMISSION);
                            }
                            else
                            {
                                // 权限已授予，启动前台服务
                                StartForegroundServiceIfNeeded();
                            }
                        }
                        else
                        {
                            // Android 13以下不需要通知权限
                            StartForegroundServiceIfNeeded();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"检查通知权限失败: {ex.Message}");
                    }
                });
            });
        }

        protected override void OnResume()
        {
            base.OnResume();
            // 确保前台服务在应用恢复时运行
            Task.Run(async () =>
            {
                await Task.Delay(1500); // 延迟1.5秒
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        StartForegroundServiceIfNeeded();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"OnResume启动前台服务失败: {ex.Message}");
                    }
                });
            });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // 应用销毁时停止前台服务
            try
            {
                XiaoZhiForegroundService.StopService(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止前台服务失败: {ex.Message}");
            }
        }

        private void StartForegroundServiceIfNeeded()
        {
            try
            {
                XiaoZhiForegroundService.StartService(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动前台服务失败: {ex.Message}");
            }
        }

        private void RequestIgnoreBatteryOptimizations()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    var powerManager = (PowerManager?)GetSystemService(PowerService);
                    if (powerManager != null && !powerManager.IsIgnoringBatteryOptimizations(PackageName))
                    {
                        var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                        intent.SetData(Android.Net.Uri.Parse($"package:{PackageName}"));
                        StartActivityForResult(intent, REQUEST_IGNORE_BATTERY_OPTIMIZATIONS);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"请求忽略电池优化失败: {ex.Message}");
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == REQUEST_NOTIFICATION_PERMISSION)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    // 通知权限已授予，启动前台服务
                    StartForegroundServiceIfNeeded();
                }
                else
                {
                    // 用户拒绝了通知权限，但仍然可以尝试启动服务
                    StartForegroundServiceIfNeeded();
                }
            }
        }

        // 将 Android ID 格式化为 MAC 地址格式
        public string FormatAndroidIdToMacFormat(string androidId)
        {
            if (string.IsNullOrEmpty(androidId))
            {
                return string.Empty;
            }

            StringBuilder formattedId = new StringBuilder();
            for (int i = 0; i < 12; i++)
            {
                formattedId.Append(androidId[i]);
                if ((i + 1) % 2 == 0 && i < 12 - 1)
                {
                    formattedId.Append(":");
                }
            }
            return formattedId.ToString();
        }
    }
}
