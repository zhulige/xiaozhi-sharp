using Android.Content;
using Android.Content.PM;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace DuoDuo.Platforms.Android.McpTools
{
    [McpServerToolType]
    public sealed class AndroidApp_Tool
    {
        [McpServerTool, Description("打开指定的Android应用。参数：appName - 应用名称或包名")]
        public static string OpenApp(string appName)
        {
            try
            {
                var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
                var packageManager = context.PackageManager;

                // 首先尝试通过包名打开
                if (TryOpenAppByPackageName(context, packageManager, appName))
                {
                    return $"成功打开应用: {appName}";
                }

                // 如果包名方式失败，尝试通过应用名称查找
                var packageName = FindPackageByAppName(packageManager, appName);
                if (!string.IsNullOrEmpty(packageName))
                {
                    if (TryOpenAppByPackageName(context, packageManager, packageName))
                    {
                        return $"成功打开应用: {appName} (包名: {packageName})";
                    }
                }

                return $"未找到应用: {appName}";
            }
            catch (Exception ex)
            {
                return $"打开应用失败: {ex.Message}";
            }
        }

        [McpServerTool, Description("列出所有已安装的应用")]
        public static string ListInstalledApps()
        {
            try
            {
                var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
                var packageManager = context.PackageManager;
                var packages = packageManager.GetInstalledApplications(PackageInfoFlags.MatchAll);

                var appList = new List<object>();
                foreach (var package in packages)
                {
                    // 只列出有启动器图标的应用（用户应用）
                    var launchIntent = packageManager.GetLaunchIntentForPackage(package.PackageName);
                    if (launchIntent != null)
                    {
                        var appName = packageManager.GetApplicationLabel(package)?.ToString() ?? package.PackageName;
                        appList.Add(new
                        {
                            Name = appName,
                            PackageName = package.PackageName
                        });
                    }
                }

                // 按应用名称排序
                appList = appList.OrderBy(app => ((dynamic)app).Name).ToList();

                return JsonSerializer.Serialize(new
                {
                    Success = true,
                    Count = appList.Count,
                    Apps = appList
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [McpServerTool, Description("搜索应用。参数：keyword - 搜索关键词")]
        public static string SearchApp(string keyword)
        {
            try
            {
                var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
                var packageManager = context.PackageManager;
                var packages = packageManager.GetInstalledApplications(PackageInfoFlags.MatchAll);

                var searchResults = new List<object>();
                keyword = keyword.ToLower();

                foreach (var package in packages)
                {
                    var launchIntent = packageManager.GetLaunchIntentForPackage(package.PackageName);
                    if (launchIntent != null)
                    {
                        var appName = packageManager.GetApplicationLabel(package)?.ToString() ?? package.PackageName;
                        
                        // 搜索应用名称或包名
                        if (appName.ToLower().Contains(keyword) || package.PackageName.ToLower().Contains(keyword))
                        {
                            searchResults.Add(new
                            {
                                Name = appName,
                                PackageName = package.PackageName
                            });
                        }
                    }
                }

                return JsonSerializer.Serialize(new
                {
                    Success = true,
                    Keyword = keyword,
                    Count = searchResults.Count,
                    Results = searchResults
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [McpServerTool, Description("打开系统设置")]
        public static string OpenSettings()
        {
            try
            {
                var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
                var intent = new Intent(global::Android.Provider.Settings.ActionSettings);
                intent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(intent);
                return "成功打开系统设置";
            }
            catch (Exception ex)
            {
                return $"打开设置失败: {ex.Message}";
            }
        }

        [McpServerTool, Description("打开指定的系统设置页面。参数：settingType - 设置类型(wifi/bluetooth/location/app_settings/notification/display/sound/battery)")]
        public static string OpenSpecificSettings(string settingType)
        {
            try
            {
                var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
                Intent intent;

                switch (settingType.ToLower())
                {
                    case "wifi":
                        intent = new Intent(global::Android.Provider.Settings.ActionWifiSettings);
                        break;
                    case "bluetooth":
                        intent = new Intent(global::Android.Provider.Settings.ActionBluetoothSettings);
                        break;
                    case "location":
                        intent = new Intent(global::Android.Provider.Settings.ActionLocationSourceSettings);
                        break;
                    case "app_settings":
                        intent = new Intent(global::Android.Provider.Settings.ActionApplicationDetailsSettings);
                        intent.SetData(global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
                        break;
                    case "notification":
                        intent = new Intent(global::Android.Provider.Settings.ActionNotificationListenerSettings);
                        break;
                    case "display":
                        intent = new Intent(global::Android.Provider.Settings.ActionDisplaySettings);
                        break;
                    case "sound":
                        intent = new Intent(global::Android.Provider.Settings.ActionSoundSettings);
                        break;
                    case "battery":
                        intent = new Intent(global::Android.Provider.Settings.ActionBatterySaverSettings);
                        break;
                    default:
                        return $"不支持的设置类型: {settingType}";
                }

                intent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(intent);
                return $"成功打开{settingType}设置";
            }
            catch (Exception ex)
            {
                return $"打开设置失败: {ex.Message}";
            }
        }

        private static bool TryOpenAppByPackageName(Context context, PackageManager packageManager, string packageName)
        {
            try
            {
                var launchIntent = packageManager.GetLaunchIntentForPackage(packageName);
                if (launchIntent != null)
                {
                    launchIntent.AddFlags(ActivityFlags.NewTask);
                    context.StartActivity(launchIntent);
                    return true;
                }
            }
            catch (Exception)
            {
                // 忽略异常，返回false
            }
            return false;
        }

        private static string? FindPackageByAppName(PackageManager packageManager, string appName)
        {
            try
            {
                var packages = packageManager.GetInstalledApplications(PackageInfoFlags.MatchAll);
                appName = appName.ToLower();

                // 首先尝试精确匹配
                foreach (var package in packages)
                {
                    var launchIntent = packageManager.GetLaunchIntentForPackage(package.PackageName);
                    if (launchIntent != null)
                    {
                        var installedAppName = packageManager.GetApplicationLabel(package)?.ToString()?.ToLower();
                        if (installedAppName == appName)
                        {
                            return package.PackageName;
                        }
                    }
                }

                // 如果精确匹配失败，尝试模糊匹配
                foreach (var package in packages)
                {
                    var launchIntent = packageManager.GetLaunchIntentForPackage(package.PackageName);
                    if (launchIntent != null)
                    {
                        var installedAppName = packageManager.GetApplicationLabel(package)?.ToString()?.ToLower();
                        if (installedAppName?.Contains(appName) == true)
                        {
                            return package.PackageName;
                        }
                    }
                }

                // 特殊应用名称映射
                var specialApps = new Dictionary<string, string[]>
                {
                    { "微信", new[] { "com.tencent.mm", "wechat" } },
                    { "支付宝", new[] { "com.eg.android.AlipayGphone", "alipay" } },
                    { "qq", new[] { "com.tencent.mobileqq", "com.tencent.qq" } },
                    { "淘宝", new[] { "com.taobao.taobao", "taobao" } },
                    { "抖音", new[] { "com.ss.android.ugc.aweme", "douyin", "tiktok" } },
                    { "百度", new[] { "com.baidu.searchbox", "baidu" } },
                    { "高德地图", new[] { "com.autonavi.minimap", "amap" } },
                    { "美团", new[] { "com.sankuai.meituan", "meituan" } },
                    { "京东", new[] { "com.jingdong.app.mall", "jd" } },
                    { "网易云音乐", new[] { "com.netease.cloudmusic", "cloudmusic" } },
                    { "bilibili", new[] { "tv.danmaku.bili", "com.bilibili.app.in" } },
                    { "知乎", new[] { "com.zhihu.android", "zhihu" } },
                    { "小红书", new[] { "com.xingin.xhs", "xiaohongshu" } },
                    { "携程", new[] { "ctrip.android.view", "ctrip" } },
                    { "饿了么", new[] { "me.ele", "eleme" } }
                };

                // 检查特殊应用映射
                foreach (var kvp in specialApps)
                {
                    if (appName.Contains(kvp.Key) || kvp.Value.Any(v => appName.Contains(v)))
                    {
                        foreach (var possiblePackage in kvp.Value)
                        {
                            if (IsPackageInstalled(packageManager, possiblePackage))
                            {
                                return possiblePackage;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 忽略异常
            }

            return null;
        }

        private static bool IsPackageInstalled(PackageManager packageManager, string packageName)
        {
            try
            {
                packageManager.GetPackageInfo(packageName, PackageInfoFlags.MatchAll);
                return true;
            }
            catch (PackageManager.NameNotFoundException)
            {
                return false;
            }
        }
    }
} 