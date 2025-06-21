using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp_MauiApp.Services
{
    public class CameraService : XiaoZhiSharp.Services.ICameraService
    {
        public CameraService() { }
        // 检查相机是否可用
        public bool IsCameraAvailable()
        {
            return MediaPicker.Default.IsCaptureSupported;
        }
        // 检查是否有相机权限
        public bool HasCameraPermission()
        {
            var status = Permissions.CheckStatusAsync<Permissions.Camera>().Result;
            return status == PermissionStatus.Granted;
        }
        // 请求相机权限
        public async Task<bool> RequestCameraPermissionAsync()
        {
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            return status == PermissionStatus.Granted;
        }
        // 拍照并获取图像数据
        public async Task<byte[]?> CapturePhotoAsync()
        {
            try
            {
                // 检查权限
                if (!HasCameraPermission())
                {
                    if (!await RequestCameraPermissionAsync())
                    {
                        throw new PermissionException("Camera permission not granted");
                    }
                }
                // 拍照（保存到临时文件）
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null) return null;

                // 直接读取为内存流
                using var stream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing photo: {ex.Message}");
                throw;
            }
        }
    }
}
