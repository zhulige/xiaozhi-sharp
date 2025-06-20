using System;
using System.Threading.Tasks;
using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Models;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp
{
    /// <summary>
    /// OTA功能使用示例
    /// </summary>
    public class OtaExample
    {
        /// <summary>
        /// 基本OTA使用示例
        /// </summary>
        public static async Task BasicExample()
        {
            LogConsole.InfoLine("=== 基本OTA使用示例 ===");

            // 创建XiaoZhiAgent实例
            var agent = new XiaoZhiAgent();

            // 订阅事件
            agent.OnMessageEvent += (type, message) =>
            {
                LogConsole.InfoLine($"[{type}] {message}");
                return Task.CompletedTask;
            };

            agent.OnOtaEvent += (otaResponse) =>
            {
                if (otaResponse != null)
                {
                    LogConsole.InfoLine("OTA检查成功，获取到服务器配置");
                    
                    // 可以访问各种配置信息
                    if (otaResponse.WebSocket != null)
                    {
                        LogConsole.InfoLine($"WebSocket URL: {otaResponse.WebSocket.Url}");
                        LogConsole.InfoLine($"WebSocket Token: {otaResponse.WebSocket.Token}");
                    }

                    if (otaResponse.Mqtt != null)
                    {
                        LogConsole.InfoLine($"MQTT服务器: {otaResponse.Mqtt.Endpoint}");
                        LogConsole.InfoLine($"MQTT客户端ID: {otaResponse.Mqtt.ClientId}");
                    }
                }
                else
                {
                    LogConsole.InfoLine("OTA检查失败，使用默认配置");
                }
                return Task.CompletedTask;
            };

            // 启动（会自动进行OTA检查）
            await agent.Start();

            LogConsole.InfoLine("XiaoZhiAgent已启动，OTA检查完成");
        }

        /// <summary>
        /// 自定义OTA请求示例
        /// </summary>
        public static async Task CustomOtaExample()
        {
            LogConsole.InfoLine("=== 自定义OTA请求示例 ===");

            var agent = new XiaoZhiAgent();

            // 设置自定义参数
            agent.CurrentVersion = "1.2.3";
            agent.UserAgent = "custom-device/1.2.3";

            agent.OnOtaEvent += (otaResponse) =>
            {
                LogConsole.InfoLine("收到OTA响应");
                return Task.CompletedTask;
            };

            // 手动进行OTA检查（带WiFi信息）
            var otaResponse = await agent.CheckOtaUpdateWithWifi(
                ssid: "Test-WiFi",
                rssi: -45,
                channel: 6,
                ip: "192.168.1.100"
            );

            if (otaResponse != null)
            {
                LogConsole.InfoLine("自定义OTA检查成功");
            }
        }

        /// <summary>
        /// 仅OTA检查示例（不启动WebSocket）
        /// </summary>
        public static async Task OtaOnlyExample()
        {
            LogConsole.InfoLine("=== 仅OTA检查示例 ===");

            var agent = new XiaoZhiAgent();

            // 仅进行OTA检查，不启动WebSocket连接
            var otaResponse = await agent.CheckOtaUpdate();

            if (otaResponse != null)
            {
                LogConsole.InfoLine("OTA检查完成");
                
                // 检查是否有固件更新
                if (otaResponse.Firmware != null && !string.IsNullOrEmpty(otaResponse.Firmware.Url))
                {
                    LogConsole.InfoLine($"发现固件更新: {otaResponse.Firmware.Version}");
                    LogConsole.InfoLine($"下载地址: {otaResponse.Firmware.Url}");
                    
                    // 这里可以添加下载固件的逻辑
                }
                else
                {
                    LogConsole.InfoLine("没有固件更新");
                }

                // 显示服务器时间
                if (otaResponse.ServerTime != null)
                {
                    var serverTime = DateTimeOffset.FromUnixTimeMilliseconds(otaResponse.ServerTime.Timestamp);
                    LogConsole.InfoLine($"服务器时间: {serverTime}");
                    LogConsole.InfoLine($"时区: {otaResponse.ServerTime.Timezone}");
                }

                // 显示激活信息
                if (otaResponse.Activation != null)
                {
                    LogConsole.InfoLine($"激活码: {otaResponse.Activation.Code}");
                    LogConsole.InfoLine($"激活消息: {otaResponse.Activation.Message}");
                }
            }
            else
            {
                LogConsole.InfoLine("OTA检查失败");
            }
        }
    }
} 