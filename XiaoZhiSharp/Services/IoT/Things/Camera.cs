using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace XiaozhiAI.Models.IoT.Things
{
    public class Camera : Thing
    {
        private bool isOn;
        private bool isRecording;

        public Camera() : base("camera", "智能摄像头", "一个可控制开关和录像的智能摄像头") // 添加描述
        {
            isOn = false;
            isRecording = false;

            // 更新状态
            States["power"] = isOn;
            States["recording"] = isRecording;

            // 定义属性
            Properties["power"] = new Dictionary<string, object>
            {
                ["type"] = "boolean",
                ["readable"] = true,
                ["writable"] = true
            };

            Properties["recording"] = new Dictionary<string, object>
            {
                ["type"] = "boolean",
                ["readable"] = true,
                ["writable"] = true
            };
        }

        protected override Dictionary<string, object> GetMethods()
        {
            var methods = base.GetMethods();
            
            // 添加摄像头特有的方法
            methods["start_recording"] = new Dictionary<string, object>
            {
                ["description"] = "开始录像",
                ["parameters"] = new Dictionary<string, object>()
            };
            
            methods["stop_recording"] = new Dictionary<string, object>
            {
                ["description"] = "停止录像",
                ["parameters"] = new Dictionary<string, object>()
            };
            
            return methods;
        }
    }
}