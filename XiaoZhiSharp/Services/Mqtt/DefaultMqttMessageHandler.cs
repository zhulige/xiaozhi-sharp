using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XiaozhiAI.Services.Mqtt
{
    /// <summary>
    /// 默认MQTT消息处理器
    /// </summary>
    public class DefaultMqttMessageHandler : IMqttMessageHandler
    {
        /// <summary>
        /// 检查是否可以处理指定主题的消息
        /// </summary>
        public bool CanHandle(string topic)
        {
            // 处理默认订阅主题的消息
            return topic == Constants.Mqtt.TopicSubscribe;
        }

        /// <summary>
        /// 处理接收到的MQTT消息
        /// </summary>
        public async Task HandleMessageAsync(string topic, string payload)
        {
            Console.WriteLine($"处理来自主题 {topic} 的消息: {payload}");

            try
            {
                // 尝试解析JSON消息
                var message = JsonConvert.DeserializeObject<MqttMessage>(payload);
                
                if (message != null)
                {
                    // 根据消息类型执行不同操作
                    switch (message.Type?.ToLower())
                    {
                        case "command":
                            await HandleCommandMessage(message);
                            break;
                        case "notification":
                            HandleNotificationMessage(message);
                            break;
                        default:
                            Console.WriteLine($"未知消息类型: {message.Type}");
                            break;
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"解析消息失败: {ex.Message}");
                // 如果不是JSON格式，按纯文本处理
                HandleTextMessage(payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理命令类型的消息
        /// </summary>
        private async Task HandleCommandMessage(MqttMessage message)
        {
            Console.WriteLine($"收到命令: {message.Content}");                        
            // 示例：回复命令已接收
            await Task.CompletedTask;
        }

        /// <summary>
        /// 处理通知类型的消息
        /// </summary>
        private void HandleNotificationMessage(MqttMessage message)
        {
            Console.WriteLine($"收到通知: {message.Content}");
            
            // 处理通知逻辑
            // 例如：显示通知、播放提示音等
        }

        /// <summary>
        /// 处理纯文本消息
        /// </summary>
        private void HandleTextMessage(string text)
        {
            Console.WriteLine($"收到文本消息: {text}");
            
            // 处理纯文本消息的逻辑
        }
    }

         /// <summary>
    /// MQTT消息模型
    /// </summary>
        public class MqttMessage
    {
        /// <summary>
        /// 消息类型 (例如: command, notification)
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// 消息发送时间
        /// </summary>
        public DateTime? Timestamp { get; set; }
        
        /// <summary>
        /// 附加数据
        /// </summary>
        public object Data { get; set; }
    }
}