using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XiaozhiAI.Services.Mqtt
{
    /// <summary>
    /// MQTT消息处理器管理器
    /// </summary>
    public class MqttMessageHandlerManager
    {
        private readonly List<IMqttMessageHandler> _handlers = new List<IMqttMessageHandler>();
        private readonly MqttService _mqttService;

        public MqttMessageHandlerManager(MqttService mqttService)
        {
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
            _mqttService.MessageReceived += OnMessageReceived;
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <param name="handler">消息处理器</param>
        public void RegisterHandler(IMqttMessageHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
                
            _handlers.Add(handler);
        }

        /// <summary>
        /// 取消注册消息处理器
        /// </summary>
        /// <param name="handler">消息处理器</param>
        public void UnregisterHandler(IMqttMessageHandler handler)
        {
            _handlers.Remove(handler);
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private async void OnMessageReceived(object sender, MqttMessageReceivedEventArgs e)
        {
            var relevantHandlers = _handlers.Where(h => h.CanHandle(e.Topic)).ToList();
            
            if (relevantHandlers.Count == 0)
            {
                Console.WriteLine($"没有找到处理主题 {e.Topic} 的处理器");
                return;
            }

            foreach (var handler in relevantHandlers)
            {
                try
                {
                    await handler.HandleMessageAsync(e.Topic, e.Payload);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理消息时出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 发布消息到指定主题
        /// </summary>
        /// <param name="topic">主题</param>
        /// <param name="payload">消息内容</param>
        /// <returns>发布任务</returns>
        public Task PublishAsync(string topic, string payload)
        {
            return _mqttService.PublishAsync(payload, topic);
        }

        /// <summary>
        /// 订阅主题
        /// </summary>
        /// <param name="topic">要订阅的主题</param>
        /// <returns>订阅任务</returns>
        public Task SubscribeAsync(string topic)
        {
            return _mqttService.SubscribeAsync(topic);
        }

        /// <summary>
        /// 取消订阅主题
        /// </summary>
        /// <param name="topic">要取消订阅的主题</param>
        /// <returns>取消订阅任务</returns>
        public Task UnsubscribeAsync(string topic)
        {
            return _mqttService.UnsubscribeAsync(topic);
        }
    }
}