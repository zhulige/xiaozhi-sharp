using RestSharp;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Services
{
    public class ImageStorageService : IImageStorageService
    {
        public async Task<string> PostImage(string apiUrl, string token, string deviceId, string clientId, byte[] imageData, string question = "这个是什么？")
        {
            var client = new RestClient(apiUrl);
            var request = new RestRequest("", Method.Post);

            // 添加请求头和图像数据
            request.AddHeader("Device-Id", deviceId);
            request.AddHeader("Client-Id", clientId);
            if(!string.IsNullOrEmpty(token))
                request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddParameter("question", question);
            //request.AlwaysMultipartFormData = true;
            request.AddFile("file", imageData, "camera.jpg", "image/jpeg");

            // 发送请求并获取响应
            var response = await client.ExecuteAsync(request);

            return response.Content;
        }

        public async Task<string> BaiduImage(string deviceId)
        {
            var client = new RestClient("https://coze.nbee.net/image-classify/v1/");
            var request = new RestRequest("baidu?id="+deviceId, Method.Get);
            var response = await client.ExecuteAsync(request);

            return response.Content;
        }

        public async Task<string> XiaoZhiImage(string deviceId)
        {
            var client = new RestClient("https://coze.nbee.net/image-classify/v1/");
            var request = new RestRequest("xiaozhi?id=" + deviceId, Method.Get);
            var response = await client.ExecuteAsync(request);

            return response.Content;
        }
    }
}
