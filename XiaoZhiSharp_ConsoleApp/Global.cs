using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp_ConsoleApp
{
    public class Global
    {
        public static string CurrentVersion = "0.1.0";
        public static string DeviceId = "b8:31:b5:95:61:05";
        public static string ClientId = ""; // Guid.NewGuid().ToString();
        public static Pipe McpClientToServerPipe = new Pipe();
        public static Pipe McpServerToClientPipe = new Pipe();
        public static string? McpVisionUrl = "";
        public static string? McpVisionToken = "";
    }
}
