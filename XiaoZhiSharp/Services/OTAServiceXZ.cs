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
    public class OtaServiceXZ
    {
        public string? OTA_VERSION_URL { get; set; } = "https://api.tenclass.net/xiaozhi/ota/";
        public dynamic? OTA_INFO { get; set; }
        public string? MAC_ADDR { get; set; }

        public OtaServiceXZ(string url, string mac)
        {
            Console.WriteLine("小智官方服务器OTA访问");
            OTA_VERSION_URL = url;
            MAC_ADDR = mac;
            if (string.IsNullOrEmpty(MAC_ADDR))
                MAC_ADDR = SystemInfo.GetMacAddress();

            Thread _otaThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        GetOtaInfo();
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
                var postData = new
                {

                    application = new
                    {
                        name = "XiaoZhiSharp",
                        version = "1.0.1"
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