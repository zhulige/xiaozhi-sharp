using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddFile("file", imageData, "camera.jpg", "image/jpeg");

            // 发送请求并获取响应
            var response = client.ExecuteAsync(request);

            return string.Empty;
        }

        public async Task<string> XiaoZhiPostImage(string apiUrl, string token, string deviceId, string clientId, byte[] imageData, string question = "请描述这张图片的内容")
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                // 构造multipart/form-data请求
                var content = new MultipartFormDataContent();

                // 添加问题字段
                content.Add(new StringContent(question), "question");

                // 添加图片数据
                var imageContent = new ByteArrayContent(imageData);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "file", "camera.jpg");

                // 设置请求头
                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = content
                };

                // 添加设备标识
                request.Headers.Add("Device-Id", deviceId);
                request.Headers.Add("Client-Id", clientId);

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Add("Authorization", $"Bearer {token}");
                }

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"AI识别结果: {result}");
                    return result;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"AI识别失败，状态码: {response.StatusCode}，响应: {errorBody}");
                    return $"{{\"success\": false, \"message\": \"AI识别失败，状态码: {response.StatusCode}\"}}";
                }

            }
            catch (Exception ex)
            {
                string errorMessage = $"XiaoZhiPostImage方法发生异常: {ex.Message}";
                return $"{{\"success\": false, \"message\": \"AI识别失败\"}}";
            }
        }
    }
}
