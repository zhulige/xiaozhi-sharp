using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace XiaoZhiSharp.Services
{
    public class ImageStorageService : IImageStorageService
    {
        public string PostImage(string apiUrl, string token, string deviceId, string clientId, byte[] imageData)
        {
            var client = new RestClient(apiUrl);
            var request = new RestRequest("", Method.Post);

            // 添加请求头和图像数据
            request.AddHeader("Device-Id", deviceId);
            request.AddHeader("Client-Id", clientId);
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddFile("file", imageData, "image.jpg", "image/jpeg");

            // 发送请求并获取响应
            var response = client.ExecuteAsync(request);

            return string.Empty;
        }
    }
}
