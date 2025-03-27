using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace XiaozhiAI.Models.IoT.Things
{
    public class Speaker : Thing
    {
        private bool isOn;
        private int volume;
        private bool isMuted;

        public Speaker() : base("speaker", "智能音箱", "一个可控制音量和静音的智能音箱") // 添加描述
        {
            isOn = false;
            volume = 50;
            isMuted = false;

            // 更新状态
            States["power"] = isOn;
            States["volume"] = volume;
            States["muted"] = isMuted;

            // 定义属性
            Properties["power"] = new Dictionary<string, object>
            {
                ["type"] = "boolean",
                ["readable"] = true,
                ["writable"] = true
            };

            Properties["volume"] = new Dictionary<string, object>
            {
                ["type"] = "integer",
                ["readable"] = true,
                ["writable"] = true,
                ["min"] = 0,
                ["max"] = 100
            };

            Properties["muted"] = new Dictionary<string, object>
            {
                ["type"] = "boolean",
                ["readable"] = true,
                ["writable"] = true
            };
        }

        protected override Dictionary<string, object> GetMethods()
        {
            var methods = base.GetMethods();
            
            // 添加音箱特有的方法
            methods["set_volume"] = new Dictionary<string, object>
            {
                ["description"] = "设置音量",
                ["parameters"] = new Dictionary<string, object>
                {
                    ["volume"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "音量值(0-100)",
                        ["min"] = 0,
                        ["max"] = 100
                    }
                }
            };
            
            methods["mute"] = new Dictionary<string, object>
            {
                ["description"] = "静音",
                ["parameters"] = new Dictionary<string, object>()
            };
            
            methods["unmute"] = new Dictionary<string, object>
            {
                ["description"] = "取消静音",
                ["parameters"] = new Dictionary<string, object>()
            };
            
            return methods;
        }
    }
}