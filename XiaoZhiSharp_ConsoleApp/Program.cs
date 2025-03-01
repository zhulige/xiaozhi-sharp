using Newtonsoft.Json;
using RestSharp;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace XiaoZhiCSharp_ConsoleApp
{
    class Program
    {
        #region 全局变量
        static string OTA_VERSION_URL = "https://api.tenclass.net/xiaozhi/ota/";
        static string MAC_ADDR = "c8:b2:9b:3a:52:c3";
        #endregion
        static dynamic? mqtt_info;
        static MqttClient? mqttc;

        static string sessionId;
        static string state;

        static void Main(string[] args)
        {
            Console.WriteLine("====================================");
            Console.WriteLine("欢迎使用 XiaoZhiSharp ！");
            //初始化小智
            GetConfig();

            if (mqtt_info != null)
            {
                string endpoint = mqtt_info.endpoint;
                string clientId = mqtt_info.client_id;
                string username = mqtt_info.username;
                string password = mqtt_info.password;
                string publishTopic = mqtt_info.publish_topic;
                string subscribeTopic = mqtt_info.subscribe_topic;
                Console.WriteLine("小智AI--初始化成功！");
            }
            else {
                Console.WriteLine("小智AI--初始化失败,请重启！");
                return;
            }

            // 创建MQTT客户端实例
            mqttc = new MqttClient(System.Convert.ToString(mqtt_info.endpoint), 8883, true, null, null, MqttSslProtocols.TLSv1_2);
            mqttc.MqttMsgPublishReceived += Mqttc_MqttMsgPublishReceived;
            mqttc.MqttMsgSubscribed += Mqttc_MqttMsgSubscribed;

            mqttc.Connect(
                    System.Convert.ToString(mqtt_info.client_id),
                    System.Convert.ToString(mqtt_info.username),
                    System.Convert.ToString(mqtt_info.password)
                    );

            mqttc.Subscribe(new string[] { (string)mqtt_info.subscribe_topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            Console.WriteLine("小智AI--开始聊天吧！");
            Console.WriteLine("====================================");
            while (true) {
                Console.Write("我：");
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(sessionId))
                {
                    SendMsg_Hello();
                    Thread.Sleep(1000);
                }

                SendMsg_Listen_Detect(input);
                state = "start";
                while (!string.IsNullOrEmpty(state))
                {
                    Thread.Sleep(1000);
                }
                Thread.Sleep(100);
            }
        }

        private static void Mqttc_MqttMsgSubscribed(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgSubscribedEventArgs e)
        {
            
        }

        private static void Mqttc_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            try
            {
                string message = Encoding.UTF8.GetString(e.Message);
                dynamic msg = JsonConvert.DeserializeObject<dynamic>(message);
                //Console.WriteLine(msg);
                if (msg.type == "hello") {
                    sessionId = msg.session_id;
                    //Console.WriteLine(sessionId);
                }
                if (msg.type == "tts" && msg.state == "sentence_start") {
                    state = (string)msg.state;
                    Console.WriteLine($"小智AI:{(string)msg.text}");
                }
                if (msg.type == "tts" && msg.state == "sentence_end")
                {
                    state = string.Empty;
                }
                if (msg.type == "goodbye") {
                    sessionId = string.Empty;
                    state = string.Empty;
                }
            }
            catch (Exception ex)
            {

            }
            
        }

        static void SendMsg_Hello() {
            var msg = new
            {
                type = "hello",
                version = 3,
            };
            PushMqttMsg(msg);
        }

        static void SendMsg_Listen_Detect(string input)
        {
            var msg = new
            {
                session_id = sessionId,
                type = "listen",
                state = "detect",
                text = input,
            };
            PushMqttMsg(msg);
        }

        public static void PushMqttMsg(dynamic message)
        {
            try
            {
                string jsonMessage = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                mqttc.Publish(System.Convert.ToString(mqtt_info.publish_topic), Encoding.UTF8.GetBytes(jsonMessage));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"推送MQTT消息时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        static void GetConfig()
        {
            try
            {
                var client = new RestClient(OTA_VERSION_URL);
                var request = new RestRequest();
                request.AddHeader("Device-Id", MAC_ADDR);
                request.AddHeader("Content-Type", "application/json");

                DateTime currentUtcTime = DateTime.UtcNow;
                string format = "MMM dd yyyyT HH:mm:ssZ";
                string formattedTime = currentUtcTime.ToString(format, System.Globalization.CultureInfo.InvariantCulture);

                var postData = new
                {
                    application = new
                    {
                        name = "xiaozhi",
                        version = "0.9.9",
                        compile_time = formattedTime,
                        idf_version = "v5.3.2-dirty",
                        elf_sha256 = "22986216df095587c42f8aeb06b239781c68ad8df80321e260556da7fcf5f522"
                    },
                };

                request.AddJsonBody(postData);

                var response = client.Post(request);
                dynamic? content = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);
                if (content.activation != null) {
                    Console.WriteLine($"请先登录xiaozhi.me,绑定Code：{(string)content.activation.code}");
                }
                //Console.WriteLine(response.Content);
                mqtt_info = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content).mqtt;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取OTA版本信息时发生异常: {ex.Message}");
            }
        }
    }
}