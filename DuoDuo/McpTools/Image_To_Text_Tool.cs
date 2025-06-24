using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;
using XiaoZhiSharp.Services;

namespace DuoDuo.McpTools
{
    [McpServerToolType]
    public sealed class Image_To_Text_Tool
    {
        [McpServerTool, Description("拍一张照片并进行解释。在用户要求你展示某物后使用此工具。")]
        public static async Task<string> Take_Photo()
        {
            XiaoZhiSharp.Services.ImageStorageService imageStorageService = new XiaoZhiSharp.Services.ImageStorageService();
            //string res = await imageStorageService.XiaoZhiPostImage("https://api.xiaozhi.me/mcp/vision/explain", "", Global.DeviceId, Global.ClientId, Global.PhotoData);
            //byte[] originalImageData= File.ReadAllBytes("Resources/Images/img1.jpg");
            //byte[] originalImageData = File.ReadAllBytes("img1.jpg");
            //string res = await imageStorageService.PostImage("https://api.xiaozhi.me/mcp/vision/explain", "", Global.DeviceId, Global.ClientId, Global.PhotoData);
            //string res = await imageStorageService.BaiduImage(Global.DeviceId);
            string res = await imageStorageService.XiaoZhiImage(Global.DeviceId);
            return res;//"我看见一个大帅哥";
        }
    }
} 