using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using XiaoZhiSharp_MauiApp.Services;
using AndroidX.Core.Content;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;

namespace XiaoZhiSharp_MauiApp.Platforms.Android.Services
{
    public class CameraService : ICameraService
    {
        private readonly HttpClient _httpClient;
        private string _explainUrl = "";
        private string _explainToken = "";

        public CameraService()
        {
            _httpClient = new HttpClient();
        }

        public bool IsSupported => MediaPicker.Default.IsCaptureSupported;

        public bool HasPermission
        {
            get
            {
                var context = Platform.CurrentActivity?.ApplicationContext ?? global::Android.App.Application.Context;
                return ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.Camera) == Permission.Granted;
            }
        }

        public event EventHandler<bool>? CameraStatusChanged;

        public async Task<bool> RequestPermissionAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                var hasPermission = status == PermissionStatus.Granted;
                CameraStatusChanged?.Invoke(this, hasPermission);
                return hasPermission;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"请求摄像头权限时出错: {ex.Message}");
                return false;
            }
        }

        public async Task<byte[]?> CapturePhotoAsync()
        {
            try
            {
                if (!HasPermission)
                {
                    var granted = await RequestPermissionAsync();
                    if (!granted)
                    {
                        System.Diagnostics.Debug.WriteLine("没有摄像头权限");
                        return null;
                    }
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "拍照进行AI识别"
                });

                if (photo == null)
                    return null;

                // 读取图片数据
                using var sourceStream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await sourceStream.CopyToAsync(memoryStream);
                
                var originalSize = memoryStream.Length;
                System.Diagnostics.Debug.WriteLine($"拍照成功，原始图片大小: {originalSize} bytes");
                
                // 压缩图片
                var compressedData = CompressImage(memoryStream.ToArray());
                System.Diagnostics.Debug.WriteLine($"图片压缩后大小: {compressedData.Length} bytes (压缩比: {(double)compressedData.Length / originalSize:P2})");
                
                return compressedData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"拍照时出错: {ex.Message}");
                return null;
            }
        }

        private byte[] CompressImage(byte[] imageData, int maxWidth = 800, int maxHeight = 600, int quality = 80)
        {
            try
            {
                using var originalBitmap = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);
                if (originalBitmap == null)
                {
                    System.Diagnostics.Debug.WriteLine("无法解码图片，返回原数据");
                    return imageData;
                }

                // 计算缩放比例
                float scaleWidth = (float)maxWidth / originalBitmap.Width;
                float scaleHeight = (float)maxHeight / originalBitmap.Height;
                float scaleFactor = Math.Min(scaleWidth, scaleHeight);
                
                // 如果图片已经很小，不需要缩放
                if (scaleFactor >= 1.0f)
                {
                    scaleFactor = 1.0f;
                }

                int newWidth = (int)(originalBitmap.Width * scaleFactor);
                int newHeight = (int)(originalBitmap.Height * scaleFactor);

                System.Diagnostics.Debug.WriteLine($"图片缩放: {originalBitmap.Width}x{originalBitmap.Height} -> {newWidth}x{newHeight} (比例: {scaleFactor:F2})");

                // 创建缩放后的位图
                using var scaledBitmap = Bitmap.CreateScaledBitmap(originalBitmap, newWidth, newHeight, true);
                
                // 压缩为JPEG
                using var stream = new MemoryStream();
                scaledBitmap.Compress(Bitmap.CompressFormat.Jpeg, quality, stream);
                
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"压缩图片时出错: {ex.Message}");
                return imageData; // 返回原数据
            }
        }

        public async Task<string> CaptureAndExplainAsync(string question)
        {
            var imageData = await CapturePhotoAsync();
            if (imageData == null)
            {
                return "{\"success\": false, \"message\": \"拍照失败\"}";
            }
            
            return await ExplainImageAsync(imageData, question);
        }

        public async Task<string> ExplainImageAsync(byte[] imageData, string question)
        {
            try
            {
                if (string.IsNullOrEmpty(_explainUrl))
                {
                    return "{\"success\": false, \"message\": \"AI识别服务URL未设置\"}";
                }

                if (imageData == null || imageData.Length == 0)
                {
                    return "{\"success\": false, \"message\": \"图像数据无效\"}";
                }

                System.Diagnostics.Debug.WriteLine($"开始AI识别，问题: {question}，图像大小: {imageData.Length} bytes");

                // 构造multipart/form-data请求
                using var content = new MultipartFormDataContent();
                
                // 添加问题字段
                content.Add(new StringContent(question), "question");
                
                // 添加图片数据
                var imageContent = new ByteArrayContent(imageData);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "file", "camera.jpg");

                // 设置请求头
                var request = new HttpRequestMessage(HttpMethod.Post, _explainUrl)
                {
                    Content = content
                };

                // 添加设备标识
                request.Headers.Add("Device-Id", GetDeviceId());
                request.Headers.Add("Client-Id", GetClientId());
                
                if (!string.IsNullOrEmpty(_explainToken))
                {
                    request.Headers.Add("Authorization", $"Bearer {_explainToken}");
                }

                var response = await _httpClient.SendAsync(request);
                
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
                System.Diagnostics.Debug.WriteLine($"AI识别时出错: {ex.Message}");
                return $"{{\"success\": false, \"message\": \"AI识别时出错: {ex.Message}\"}}";
            }
        }

        public void SetExplainUrl(string url, string token)
        {
            _explainUrl = url;
            _explainToken = token;
            System.Diagnostics.Debug.WriteLine($"设置AI识别服务URL: {url}");
        }

        private string GetDeviceId()
        {
            try
            {
                return global::Android.Provider.Settings.Secure.GetString(
                    Platform.CurrentActivity?.ContentResolver ?? global::Android.App.Application.Context.ContentResolver,
                    global::Android.Provider.Settings.Secure.AndroidId) ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private string GetClientId()
        {
            return Guid.NewGuid().ToString();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 