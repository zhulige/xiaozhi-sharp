using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DuoDuo.McpTools
{
    [McpServerToolType]
    public sealed class IotThings_Tool
    {
        [McpServerTool, Description("开灯")]
        public static string Light_ON()
        {
            return "开灯成功";
        }

        [McpServerTool, Description("关灯")]
        public static string Light_OFF()
        {
            return "关灯成功";
        }
    }
} 