using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.AspNetCore.Components.WebView.Maui;
using System.Text;
using AndroidX.Core.View;
using Android.Views;

namespace XiaoZhiSharp_MauiBlazorApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            // 获取 Android ID 并格式化为 MAC 地址格式
            var androidId = Android.Provider.Settings.Secure.GetString(ContentResolver, Android.Provider.Settings.Secure.AndroidId);
            var formattedAndroidId = FormatAndroidIdToMacFormat(androidId);
            Global.DeviceId = formattedAndroidId;

            base.OnCreate(savedInstanceState);

            // 设置状态栏颜色和样式
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#4a90e2")); // 设置为蓝色
                
                // 设置状态栏文字为白色（适配深色背景）
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    var decorView = Window?.DecorView;
                    if (decorView != null)
                    {
                        // 清除SYSTEM_UI_FLAG_LIGHT_STATUS_BAR标志，使状态栏文字变白
                        var flags = (int)decorView.SystemUiVisibility;
                        flags &= ~(int)SystemUiFlags.LightStatusBar;
                        decorView.SystemUiVisibility = (StatusBarVisibility)flags;
                    }
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
