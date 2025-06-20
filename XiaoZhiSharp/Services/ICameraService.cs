using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Services
{
    public interface ICameraService
    {
        /// <summary>
        /// 拍照并获取图像数据
        /// </summary>
        Task<byte[]?> CapturePhotoAsync();
    }
}
