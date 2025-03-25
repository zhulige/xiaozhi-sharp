using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Protocols
{
    public class IotThingsProtocol : IotThings
    {

    }

    public class IotThings
    {
        public string type { get; set; } = "iot";
        public List<IotThingsDescriptor> descriptors { get; set; } = new List<IotThingsDescriptor>();
        public string session_id { get; set; } = "";
    }

    public class IotThingsDescriptor {
        // lamp
        public string name { get; set; } = "";
        // 一个可控制开关、亮度和颜色的智能灯
        public string description { get; set; } = "";
        public List<IotThingsPropertie> properties { get; set; } = new List<IotThingsPropertie>();
        public List<IotThingsMethods> methods { get; set; } = new List<IotThingsMethods>();
    }

    public class IotThingsPropertie {
        // power
        public string key { get; set; } = "";
        // 智能灯的power属性
        public string description { get; set; } = "";
        // boolean
        public string type { get; set; } = "";
    }

    public class IotThingsMethods {
        // turn_on
        public string key { get; set; } = "";
        // 打开设备
        public string description { get; set; } = "";
        public dynamic? parameters { get; set; } = null;
    }
}
