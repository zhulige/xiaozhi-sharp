using System;
using System.Threading.Tasks;

namespace XiaoZhiSharp_MauiApp.Services
{
    public interface ICameraService
    {
        /// <summary>
        /// 是否支持摄像头
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// 是否有摄像头权限
        /// </summary>
        bool HasPermission { get; }

        /// <summary>
        /// 请求摄像头权限
        /// </summary>
        Task<bool> RequestPermissionAsync();

        /// <summary>
        /// 拍照并获取图像数据
        /// </summary>
        /// <returns>JPEG格式的图像数据</returns>
        Task<byte[]?> CapturePhotoAsync();

        /// <summary>
        /// 拍照并进行AI识别
        /// </summary>
        /// <param name="question">要询问的问题</param>
        /// <returns>AI识别结果</returns>
        Task<string> CaptureAndExplainAsync(string question);

        /// <summary>
        /// 对指定图像数据进行AI识别
        /// </summary>
        /// <param name="imageData">图像数据</param>
        /// <param name="question">要询问的问题</param>
        /// <returns>AI识别结果</returns>
        Task<string> ExplainImageAsync(byte[] imageData, string question);

        /// <summary>
        /// 设置AI识别服务的URL和Token
        /// </summary>
        void SetExplainUrl(string url, string token);

        /// <summary>
        /// 摄像头状态变化事件
        /// </summary>
        event EventHandler<bool>? CameraStatusChanged;
    }
} 