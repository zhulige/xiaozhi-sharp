using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using System.Text;

namespace XiaoZhiSharp_MauiApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            // 获取 Android ID 并格式化为 MAC 地址格式
            var androidId = Android.Provider.Settings.Secure.GetString(ContentResolver, Android.Provider.Settings.Secure.AndroidId);
            var formattedAndroidId = FormatAndroidIdToMacFormat(androidId);
            Global.DeviceId = formattedAndroidId;

            base.OnCreate(savedInstanceState);
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
