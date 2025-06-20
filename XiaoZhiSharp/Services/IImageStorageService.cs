using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Services
{
    public interface IImageStorageService
    {
        string PostImage(string apiUrl, string token, string deviceId, string clientId, byte[] imageData);
    }
}
