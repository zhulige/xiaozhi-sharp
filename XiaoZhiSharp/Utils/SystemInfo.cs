using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Utils
{
    public class SystemInfo
    {
        /// <summary>
        /// 获取 MAC 地址
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddress()
        {
            string macAddresses = "";

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 仅考虑以太网、无线局域网和虚拟专用网络等常用接口类型
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     nic.NetworkInterfaceType == NetworkInterfaceType.Ppp))
                {
                    PhysicalAddress address = nic.GetPhysicalAddress();
                    byte[] bytes = address.GetAddressBytes();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        macAddresses += bytes[i].ToString("X2");
                        if (i != bytes.Length - 1)
                        {
                            macAddresses += ":";
                        }
                    }
                    break; // 通常只取第一个符合条件的 MAC 地址
                }
            }

            return macAddresses.ToLower();
        }

        /// <summary>
        /// 生成客户端UUID（UUID v4格式）
        /// </summary>
        /// <returns>UUID字符串</returns>
        public static string GenerateClientId()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 获取应用程序版本
        /// </summary>
        /// <returns>版本字符串</returns>
        public static string GetApplicationVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }

        /// <summary>
        /// 获取User-Agent字符串
        /// </summary>
        /// <param name="appName">应用名称</param>
        /// <param name="version">版本号</param>
        /// <returns>User-Agent字符串</returns>
        public static string GetUserAgent(string appName = "xiaozhi-sharp", string? version = null)
        {
            version ??= GetApplicationVersion();
            return $"{appName}/{version}";
        }
    }
}