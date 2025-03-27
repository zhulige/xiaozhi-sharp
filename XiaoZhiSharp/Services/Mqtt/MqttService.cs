using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace XiaozhiAI.Services.Mqtt
{
    public class MqttService : IDisposable
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _options;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected;

        // 消息接收事件
        public event EventHandler<MqttMessageReceivedEventArgs> MessageReceived;

        public MqttService()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            _cancellationTokenSource = new CancellationTokenSource();

            // 配置MQTT客户端选项
            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(Constants.Mqtt.BrokerAddress, Constants.Mqtt.BrokerPort)
                .WithClientId(Constants.Mqtt.ClientId)
                .WithCredentials(Constants.Mqtt.Username, Constants.Mqtt.Password)
                .WithCleanSession()
                .Build();

            // 设置连接事件处理
            _mqttClient.ConnectedAsync += HandleConnectedAsync;
            _mqttClient.DisconnectedAsync += HandleDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
        }

        /// <summary>
        /// 连接到MQTT服务器并订阅主题
        /// </summary>
        public async Task ConnectAsync()
        {
            try
            {
                if (!_mqttClient.IsConnected)
                {
                    await _mqttClient.ConnectAsync(_options, _cancellationTokenSource.Token);
                    Console.WriteLine("已连接到MQTT服务器");
                    _isConnected = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT连接失败: {ex.Message}");
                _isConnected = false;
            }
        }

        /// <summary>
        /// 断开与MQTT服务器的连接
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
                Console.WriteLine("已断开MQTT连接");
                _isConnected = false;
            }
        }

        /// <summary>
        /// 发布消息到指定主题
        /// </summary>
        /// <param name="topic">主题，默认为配置中的发布主题</param>
        /// <param name="payload">消息内容</param>
        /// <param name="qos">服务质量等级</param>
        /// <param name="retain">是否保留消息</param>
        public async Task PublishAsync(string payload, string topic = null, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
        {
            if (!_isConnected)
            {
                await ConnectAsync();
            }

            var actualTopic = string.IsNullOrEmpty(topic) ? Constants.Mqtt.TopicPublish : topic;
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(actualTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qos)
                .WithRetainFlag(retain)
                .Build();

            await _mqttClient.PublishAsync(message, _cancellationTokenSource.Token);
            Console.WriteLine($"已发布消息到主题 {actualTopic}: {payload}");
        }

        /// <summary>
        /// 订阅主题
        /// </summary>
        /// <param name="topic">要订阅的主题，默认为配置中的订阅主题</param>
        /// <param name="qos">服务质量等级</param>
        public async Task SubscribeAsync(string topic = null, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce)
        {
            if (!_isConnected)
            {
                await ConnectAsync();
            }

            var actualTopic = string.IsNullOrEmpty(topic) ? Constants.Mqtt.TopicSubscribe : topic;
            
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(actualTopic).WithQualityOfServiceLevel(qos))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions, _cancellationTokenSource.Token);
            Console.WriteLine($"已订阅主题: {actualTopic}");
        }

        /// <summary>
        /// 取消订阅主题
        /// </summary>
        /// <param name="topic">要取消订阅的主题</param>
        public async Task UnsubscribeAsync(string topic)
        {
            if (_isConnected)
            {
                var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                    .WithTopicFilter(topic)
                    .Build();

                await _mqttClient.UnsubscribeAsync(unsubscribeOptions, _cancellationTokenSource.Token);
                Console.WriteLine($"已取消订阅主题: {topic}");
            }
        }

        // 连接成功事件处理
        private async Task HandleConnectedAsync(MqttClientConnectedEventArgs args)
        {
            Console.WriteLine("MQTT客户端已连接");
            // 连接成功后自动订阅默认主题
            await SubscribeAsync();
        }

        // 断开连接事件处理
        private Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            Console.WriteLine($"MQTT客户端已断开连接: {args.Reason}");
            
            // 如果不是主动断开连接，尝试重新连接
            if (_isConnected)
            {
                Task.Run(async () =>
                {
                    Console.WriteLine("尝试重新连接...");
                    await Task.Delay(5000); // 等待5秒后重试
                    await ConnectAsync();
                });
            }
            
            return Task.CompletedTask;
        }

        // 接收消息事件处理
        private Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            var topic = args.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload ?? Array.Empty<byte>());
            
            Console.WriteLine($"收到来自主题 {topic} 的消息: {payload}");
            
            // 触发消息接收事件
            MessageReceived?.Invoke(this, new MqttMessageReceivedEventArgs(topic, payload));
            
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _mqttClient.DisconnectAsync().Wait();
            _cancellationTokenSource.Dispose();
            _mqttClient.Dispose();
        }
    }

    // 消息接收事件参数类
    public class MqttMessageReceivedEventArgs : EventArgs
    {
        public string Topic { get; }
        public string Payload { get; }

        public MqttMessageReceivedEventArgs(string topic, string payload)
        {
            Topic = topic;
            Payload = payload;
        }
    }
}