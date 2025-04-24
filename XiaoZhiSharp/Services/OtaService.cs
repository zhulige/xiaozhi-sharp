using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp.Services
{
    public class OtaService
    {
        public string? OTA_VERSION_URL { get; set; }
        public dynamic? OTA_INFO { get; set; }
        public string? MAC_ADDR { get; set; }
        public string? CLIENT_ID { get; set; }

        public OtaService(string url, string mac, string clientId)
        {
            Console.WriteLine("开源服务器OTA访问");
            OTA_VERSION_URL = url;
            MAC_ADDR = mac;
            CLIENT_ID = clientId;

            if (string.IsNullOrEmpty(MAC_ADDR))
                MAC_ADDR = SystemInfo.GetMacAddress();

            Thread _otaThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        GetOtaInfo().Wait();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    Thread.Sleep(1000 * 60);
                }
            });
            _otaThread.Start();
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        private async Task GetOtaInfo()
        {
            try
            {
                var client = new RestClient(OTA_VERSION_URL);
                var request = new RestRequest();
                request.AddHeader("Device-Id", MAC_ADDR);
                request.AddHeader("Client-Id", CLIENT_ID);
                request.AddHeader("Content-Type", "application/json");

                var postData = new
                {
                    version = 0,
                    uuid = "",
                    application = new
                    {
                        name = "xiaozhi",
                        version = "1.6.1",
                        compile_time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        idf_version = "4.4.3",
                        elf_sha256 = "1234567890abcdef1234567890abcdef1234567890abcdef"
                    },
                    ota = new
                    {
                        label = "xiaozhi"
                    },
                    board = new
                    {
                        type = "xiaozhi",
                        ssid = "xiaozhi",
                        rssi = 0,
                        channel = 0,
                        ip = "192.168.1.1",
                        mac = MAC_ADDR
                    },
                    flash_size = 0,
                    minimum_free_heap_size = 0,
                    mac_address = MAC_ADDR,
                    chip_model_name = "",
                    chip_info = new
                    {
                        model = 0,
                        cores = 0,
                        revision = 0,
                        features = 0
                    },
                    partition_table = new[]
                    {
                        new
                        {
                            label = "",
                            type = 0,
                            subtype = 0,
                            address = 0,
                            size = 0
                        }
                    }
                };

                request.AddJsonBody(postData);
                var response = await client.ExecutePostAsync(request);
                Console.WriteLine(response.Content);
                if (!response.IsSuccessful)
                {
                    Console.WriteLine("\n");
                    Console.WriteLine($"OTA检查失败: {response.StatusCode} {response.ErrorMessage}");
                    return;
                }

                if (!string.IsNullOrEmpty(response.Content))
                {
                    OTA_INFO = JsonConvert.DeserializeObject<dynamic>(response.Content);

                    // 处理激活码
                    if (OTA_INFO?.activation?.code != null)
                    {
                        Console.WriteLine($"请先登录xiaozhi.me,绑定Code：{OTA_INFO.activation.code}");
                    }

                    // 检查是否有新版本
                    if (OTA_INFO?.version != null)
                    {
                        // 实现版本比较和更新逻辑
                        CompareAndUpdateVersion(OTA_INFO.version);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取OTA版本信息时发生异常: {ex.Message}");
            }
        }

        private void CompareAndUpdateVersion(string newVersion)
        {
            // 实现版本比较和更新逻辑
            // ...
        }

    }
}
