using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp.Services
{
    /// <summary>
    /// OTA服务类
    /// </summary>
    public class OtaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _userAgent;
        private readonly string _deviceId;
        private readonly string _clientId;
        private readonly string _acceptLanguage;

        public OtaService(string userAgent, string deviceId, string clientId, string acceptLanguage = "zh-CN")
        {
            _httpClient = new HttpClient();
            _userAgent = userAgent;
            _deviceId = deviceId;
            _clientId = clientId;
            _acceptLanguage = acceptLanguage;

            // 设置HTTP客户端的超时时间
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 执行OTA检查
        /// </summary>
        /// <param name="otaUrl">OTA服务器地址</param>
        /// <param name="request">OTA请求数据</param>
        /// <returns>OTA响应数据</returns>
        public async Task<OtaResponse?> CheckOtaAsync(string otaUrl, OtaRequest request)
        {
            try
            {
                LogConsole.InfoLine($"开始OTA检查，URL: {otaUrl}");

                // 设置请求头
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, otaUrl);
                httpRequest.Headers.Add("Device-Id", _deviceId);
                httpRequest.Headers.Add("Client-Id", _clientId);
                httpRequest.Headers.Add("User-Agent", _userAgent);
                httpRequest.Headers.Add("Accept-Language", _acceptLanguage);

                // 序列化请求体
                var jsonContent = JsonConvert.SerializeObject(request, Formatting.None, 
                    new JsonSerializerSettings 
                    { 
                        NullValueHandling = NullValueHandling.Ignore 
                    });
                
                LogConsole.InfoLine($"OTA请求数据: {jsonContent}");
                
                httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 发送请求
                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogConsole.InfoLine($"OTA响应状态码: {response.StatusCode}");
                LogConsole.InfoLine($"OTA响应内容: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // 解析成功响应
                    var otaResponse = JsonConvert.DeserializeObject<OtaResponse>(responseContent);
                    LogConsole.InfoLine("OTA检查成功");
                    return otaResponse;
                }
                else
                {
                    // 解析错误响应
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<OtaErrorResponse>(responseContent);
                        LogConsole.ErrorLine($"OTA检查失败: {errorResponse?.Error ?? "未知错误"}");
                    }
                    catch
                    {
                        LogConsole.ErrorLine($"OTA检查失败，HTTP状态码: {response.StatusCode}, 响应内容: {responseContent}");
                    }
                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                LogConsole.ErrorLine($"OTA网络请求异常: {httpEx.Message}");
                return null;
            }
            catch (TaskCanceledException tcEx)
            {
                LogConsole.ErrorLine($"OTA请求超时: {tcEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                LogConsole.ErrorLine($"OTA检查异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建默认的OTA请求对象
        /// </summary>
        /// <param name="version">当前应用版本</param>
        /// <param name="elfSha256">ELF文件SHA256哈希值</param>
        /// <param name="boardType">开发板类型</param>
        /// <param name="boardName">开发板名称</param>
        /// <returns>OTA请求对象</returns>
        public OtaRequest CreateDefaultOtaRequest(string version = "1.0.0", string elfSha256 = "", 
            string boardType = "xiaozhi-sharp", string boardName = "xiaozhi-sharp-client")
        {
            var request = new OtaRequest
            {
                Application = new ApplicationInfo
                {
                    Name = "xiaozhi",
                    Version = version,
                    ElfSha256 = !string.IsNullOrEmpty(elfSha256) ? elfSha256 : GenerateDefaultSha256(),
                    CompileTime = DateTime.UtcNow.ToString("MMM dd yyyy HH:mm:ss") + "Z",
                    IdfVersion = "net8.0"
                },
                MacAddress = _deviceId,
                Uuid = _clientId,
                Board = new BoardInfo
                {
                    Type = boardType,
                    Name = boardName,
                    Mac = _deviceId
                },
                Version = 2,
                Language = _acceptLanguage
            };

            return request;
        }

        /// <summary>
        /// 创建包含网络信息的OTA请求对象
        /// </summary>
        /// <param name="version">当前应用版本</param>
        /// <param name="elfSha256">ELF文件SHA256哈希值</param>
        /// <param name="boardType">开发板类型</param>
        /// <param name="boardName">开发板名称</param>
        /// <param name="ssid">WiFi网络名称</param>
        /// <param name="rssi">WiFi信号强度</param>
        /// <param name="channel">WiFi频道</param>
        /// <param name="ip">设备IP地址</param>
        /// <returns>OTA请求对象</returns>
        public OtaRequest CreateWifiOtaRequest(string version = "1.0.0", string elfSha256 = "",
            string boardType = "xiaozhi-sharp-wifi", string boardName = "xiaozhi-sharp-wifi-client",
            string ssid = "", int rssi = -50, int channel = 1, string ip = "")
        {
            var request = CreateDefaultOtaRequest(version, elfSha256, boardType, boardName);
            
            // 添加WiFi信息
            request.Board.Ssid = ssid;
            request.Board.Rssi = rssi;
            request.Board.Channel = channel;
            request.Board.Ip = ip;

            return request;
        }

        /// <summary>
        /// 生成默认的SHA256哈希值（示例值）
        /// </summary>
        /// <returns>SHA256哈希字符串</returns>
        private string GenerateDefaultSha256()
        {
            // 这里生成一个示例SHA256值，实际使用时应该是真实的文件哈希
            return "c8a8ecb6d6fbcda682494d9675cd1ead240ecf38bdde75282a42365a0e396033";
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 