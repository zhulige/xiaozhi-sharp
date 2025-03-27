using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace XiaozhiAI.Models.IoT.Things
{
    public class Lamp : Thing
    {
        private bool isOn;
        private int brightness;
        private string color;

        public Lamp() : base("lamp", "智能灯", "一个可控制开关、亮度和颜色的智能灯") // 添加描述
        {
            
            isOn = false;
            brightness = 50;
            color = "#FFFFFF";

            // 更新状态
            States["power"] = isOn;
            States["brightness"] = brightness;
            States["color"] = color;

            // 定义属性
            Properties["power"] = new Dictionary<string, object>
            {
                ["type"] = "boolean",
                ["readable"] = true,
                ["writable"] = true
            };

            Properties["brightness"] = new Dictionary<string, object>
            {
                ["type"] = "integer",
                ["readable"] = true,
                ["writable"] = true,
                ["min"] = 0,
                ["max"] = 100
            };

            Properties["color"] = new Dictionary<string, object>
            {
                ["type"] = "string",
                ["readable"] = true,
                ["writable"] = true
            };
        }

        // 修改 Invoke 方法
        public override string Invoke(string actionId, JObject parameters)
        {
            switch (actionId)
            {
                case "turn_on":
                    States["power"] = true;
                    return "灯已打开";
                    
                case "turn_off":
                    States["power"] = false;
                    return "灯已关闭";
                    
                case "set_brightness":
                    if (parameters != null && parameters["brightness"] != null)
                    {
                        int brightness = parameters["brightness"].Value<int>();
                        States["brightness"] = brightness;
                        return $"亮度已设置为 {brightness}";
                    }
                    return "缺少亮度参数";
                    
                case "set_color":
                    if (parameters != null && parameters["color"] != null)
                    {
                        string color = parameters["color"].Value<string>();
                        States["color"] = color;
                        return $"颜色已设置为 {color}";
                    }
                    return "缺少颜色参数";
                    
                default:
                    return $"未知动作: {actionId}";
            }
        }

        protected override Dictionary<string, object> GetMethods()
        {
            var methods = base.GetMethods();
            
            // 添加灯特有的方法
            methods["set_brightness"] = new Dictionary<string, object>
            {
                ["description"] = "设置灯的亮度",
                ["parameters"] = new Dictionary<string, object>
                {
                    ["brightness"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "亮度值(0-100)",
                        ["min"] = 0,
                        ["max"] = 100
                    }
                }
            };
            
            methods["set_color"] = new Dictionary<string, object>
            {
                ["description"] = "设置灯的颜色",
                ["parameters"] = new Dictionary<string, object>
                {
                    ["color"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "颜色值(如#FFFFFF)"
                    }
                }
            };
            
            return methods;
        }
    }
}