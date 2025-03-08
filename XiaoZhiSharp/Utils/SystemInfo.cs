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
    }
}