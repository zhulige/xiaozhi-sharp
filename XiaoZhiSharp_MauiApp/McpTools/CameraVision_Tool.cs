using System;
using System.Threading.Tasks;
using System.Text.Json;
using XiaoZhiSharp_MauiApp.Services;

namespace XiaoZhiSharp_MauiApp.McpTools
{
    /// <summary>
    /// 摄像头视觉识别工具 - 实现拍照和AI图像识别功能
    /// </summary>
    public class CameraVision_Tool
    {
        private readonly ICameraService? _cameraService;

        public CameraVision_Tool(ICameraService? cameraService)
        {
            _cameraService = cameraService;
        }

        /// <summary>
        /// 拍照识别功能
        /// </summary>
        /// <param name="question">要询问的问题</param>
        /// <returns>识别结果</returns>
        public async Task<string> TakePhotoAndRecognize(string question = "请描述这张图片的内容")
        {
            try
            {
                if (_cameraService == null)
                {
                    return "{\"success\": false, \"message\": \"摄像头服务不可用\"}";
                }

                if (!_cameraService.IsSupported)
                {
                    return "{\"success\": false, \"message\": \"设备不支持摄像头功能\"}";
                }

                Console.WriteLine($"开始拍照识别，问题: {question}");

                // 调用摄像头服务进行拍照和识别
                var result = await _cameraService.CaptureAndExplainAsync(question);
                
                Console.WriteLine($"拍照识别完成: {result}");
                return result;
            }
            catch (Exception ex)
            {
                var errorMessage = $"拍照识别失败: {ex.Message}";
                Console.WriteLine(errorMessage);
                return $"{{\"success\": false, \"message\": \"{errorMessage}\"}}";
            }
        }

        /// <summary>
        /// 仅拍照功能
        /// </summary>
        /// <returns>拍照结果</returns>
        public async Task<string> TakePhoto()
        {
            try
            {
                if (_cameraService == null)
                {
                    return "{\"success\": false, \"message\": \"摄像头服务不可用\"}";
                }

                if (!_cameraService.IsSupported)
                {
                    return "{\"success\": false, \"message\": \"设备不支持摄像头功能\"}";
                }

                Console.WriteLine("开始拍照...");

                var imageData = await _cameraService.CapturePhotoAsync();
                
                if (imageData != null && imageData.Length > 0)
                {
                    Console.WriteLine($"拍照成功，图片大小: {imageData.Length} bytes");
                    return $"{{\"success\": true, \"message\": \"拍照成功\", \"imageSize\": {imageData.Length}}}";
                }
                else
                {
                    return "{\"success\": false, \"message\": \"拍照失败或用户取消\"}";
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"拍照失败: {ex.Message}";
                Console.WriteLine(errorMessage);
                return $"{{\"success\": false, \"message\": \"{errorMessage}\"}}";
            }
        }

        /// <summary>
        /// 设置AI识别服务
        /// </summary>
        /// <param name="url">识别服务URL</param>
        /// <param name="token">认证Token</param>
        /// <returns>设置结果</returns>
        public string SetVisionService(string url, string token = "")
        {
            try
            {
                if (_cameraService == null)
                {
                    return "{\"success\": false, \"message\": \"摄像头服务不可用\"}";
                }

                _cameraService.SetExplainUrl(url, token);
                Console.WriteLine($"AI识别服务已设置: {url}");
                
                return $"{{\"success\": true, \"message\": \"AI识别服务设置成功\", \"url\": \"{url}\"}}";
            }
            catch (Exception ex)
            {
                var errorMessage = $"设置AI识别服务失败: {ex.Message}";
                Console.WriteLine(errorMessage);
                return $"{{\"success\": false, \"message\": \"{errorMessage}\"}}";
            }
        }

        /// <summary>
        /// 获取摄像头状态
        /// </summary>
        /// <returns>摄像头状态信息</returns>
        public string GetCameraStatus()
        {
            try
            {
                if (_cameraService == null)
                {
                    return "{\"success\": false, \"message\": \"摄像头服务不可用\", \"supported\": false, \"hasPermission\": false}";
                }

                var status = new
                {
                    success = true,
                    supported = _cameraService.IsSupported,
                    hasPermission = _cameraService.HasPermission,
                    message = _cameraService.IsSupported ? 
                        (_cameraService.HasPermission ? "摄像头就绪" : "需要摄像头权限") : 
                        "设备不支持摄像头"
                };

                return JsonSerializer.Serialize(status);
            }
            catch (Exception ex)
            {
                return $"{{\"success\": false, \"message\": \"获取摄像头状态失败: {ex.Message}\"}}";
            }
        }
    }
} 