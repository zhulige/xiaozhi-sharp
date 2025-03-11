using NAudio.SoundFont;
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
        public string? OTA_VERSION_URL { get; set; } = "https://api.tenclass.net/xiaozhi/ota/";
        public dynamic? OTA_INFO { get; set; }
        public string? MAC_ADDR { get; set; }

        public OtaService(string url,string mac)
        {
            OTA_VERSION_URL = url;
            MAC_ADDR = mac;
            if(string.IsNullOrEmpty(MAC_ADDR))
                MAC_ADDR = SystemInfo.GetMacAddress();

            Thread _otaThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {

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
        private void GetOtaInfo()
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
                    flash_size = 16777216,
                    minimum_free_heap_size = 8318916,
                    mac_address = MAC_ADDR,
                    chip_model_name = "esp32s3",
                    chip_info = new { 
                        model = 9,
                        cores = 2,
                        revision = 2,
                        features = 18
                    },
                    application = new
                    {
                        name = "xiaozhi",
                        version = "1.1.2",
                        idf_version = "v5.3.2 - dirty"
                    },
                    partition_table = new List<string>(),
                    ota = new { 
                        label = "factory"
                    },
                    board = new { 
                        type = "",
                        mac = MAC_ADDR
                    }
                };

                request.AddJsonBody(postData);

                var response = client.Post(request);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("获取OTA版本信息失败!");
                }
                if (response.Content != null && response.Content != "")
                {
                    OTA_INFO = JsonConvert.DeserializeObject<dynamic>(response.Content);
                    if (OTA_INFO != null && OTA_INFO.activation != null)
                    {
                        Console.WriteLine($"请先登录xiaozhi.me,绑定Code：{(string)OTA_INFO.activation.code}");
                    }
                    //Console.WriteLine(response.Content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取OTA版本信息时发生异常: {ex.Message}");
            }
        }
    }
}
