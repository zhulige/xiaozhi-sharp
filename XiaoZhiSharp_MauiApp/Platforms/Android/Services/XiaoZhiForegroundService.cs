using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;

namespace XiaoZhiSharp_MauiApp.Platforms.Android.Services
{
    [Service(ForegroundServiceType = ForegroundService.TypeMicrophone)]
    public class XiaoZhiForegroundService : Service
    {
        private const string CHANNEL_ID = "XiaoZhiChannel";
        private const int NOTIFICATION_ID = 1001;
        private PowerManager.WakeLock? _wakeLock;

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            try
            {
                // 创建通知渠道
                CreateNotificationChannel();

                // 创建通知
                var notification = CreateNotification();

                // 启动前台服务
                StartForeground(NOTIFICATION_ID, notification);

                // 获取唤醒锁以防止CPU休眠
                AcquireWakeLock();

                // 返回 StartCommandResult.RedeliverIntent 确保服务被杀死后能重启
                return StartCommandResult.RedeliverIntent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动前台服务失败: {ex.Message}");
                return StartCommandResult.NotSticky;
            }
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    CHANNEL_ID,
                    "小智助手服务",
                    NotificationImportance.High)
                {
                    Description = "保持小智助手在后台运行，防止被系统杀死",
                    LockscreenVisibility = NotificationVisibility.Public
                };
                channel.SetShowBadge(false);
                channel.EnableLights(false);
                channel.EnableVibration(false);

                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        private Notification CreateNotification()
        {
            try
            {
                var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
                    ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                    : PendingIntentFlags.UpdateCurrent;

                // 创建点击通知时的Intent
                var intent = new Intent(this, typeof(MainActivity));
                var pendingIntent = PendingIntent.GetActivity(this, 0, intent, pendingIntentFlags);

                var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                    .SetContentTitle("小智助手")
                    .SetContentText("正在后台运行，随时为您服务")
                    .SetContentIntent(pendingIntent)
                    .SetOngoing(true) // 设置为持续通知
                    .SetPriority(NotificationCompat.PriorityHigh)
                    .SetCategory(NotificationCompat.CategoryService)
                    .SetAutoCancel(false) // 不能被用户取消
                    .SetShowWhen(false)
                    .SetSilent(true); // 静默通知

                // 尝试设置图标，如果失败则使用默认图标
                try
                {
                    // 使用应用图标
                    var packageInfo = PackageManager?.GetPackageInfo(PackageName!, 0);
                    if (packageInfo != null && packageInfo.ApplicationInfo != null)
                    {
                        builder.SetSmallIcon(packageInfo.ApplicationInfo.Icon);
                    }
                    else
                    {
                        // 使用系统默认图标
                        builder.SetSmallIcon(Microsoft.Maui.Resource.Mipmap.appicon);
                    }
                }
                catch
                {
                    // 如果获取图标失败，使用最基本的系统图标
                    builder.SetSmallIcon(Microsoft.Maui.Resource.Mipmap.appicon);
                }

                return builder.Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建通知失败: {ex.Message}");
                // 返回最基本的通知
                return new NotificationCompat.Builder(this, CHANNEL_ID)
                    .SetContentTitle("小智助手")
                    .SetContentText("后台运行中")
                    .SetSmallIcon(Microsoft.Maui.Resource.Mipmap.appicon)
                    .Build();
            }
        }

        private void AcquireWakeLock()
        {
            try
            {
                var powerManager = (PowerManager?)GetSystemService(PowerService);
                // 使用更强的WakeLock类型，防止CPU休眠
                _wakeLock = powerManager?.NewWakeLock(WakeLockFlags.Partial | WakeLockFlags.AcquireCausesWakeup, "XiaoZhi::WakeLock");
                _wakeLock?.Acquire();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取WakeLock失败: {ex.Message}");
            }
        }

        private void ReleaseWakeLock()
        {
            _wakeLock?.Release();
            _wakeLock = null;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            ReleaseWakeLock();
            StopForeground(true);
            
            // 尝试重启服务（如果不是正常停止）
            try
            {
                var intent = new Intent(this, typeof(XiaoZhiForegroundService));
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    StartForegroundService(intent);
                }
                else
                {
                    StartService(intent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重启服务失败: {ex.Message}");
            }
        }

        public override void OnTaskRemoved(Intent? rootIntent)
        {
            base.OnTaskRemoved(rootIntent);
            // 当任务被移除时，确保服务继续运行
            System.Diagnostics.Debug.WriteLine("任务被移除，但服务继续运行");
        }

        public static void StartService(Context context)
        {
            var intent = new Intent(context, typeof(XiaoZhiForegroundService));
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        public static void StopService(Context context)
        {
            var intent = new Intent(context, typeof(XiaoZhiForegroundService));
            context.StopService(intent);
        }
    }
} 