using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XiaozhiAI.Models.IoT
{
    public class ThingManager
    {
        private static ThingManager _instance;
        public  string ID;
        private readonly Dictionary<string, Thing> _things;

        // 单例模式
        public static ThingManager GetInstance()
        {
            return _instance ??= new ThingManager();
        }

        private ThingManager()
        {
            _things = new Dictionary<string, Thing>();
        }
        /// <summary>
            /// 执行物联网命令
            /// </summary>
            /// <param name="command">命令对象</param>
            /// <returns>执行结果</returns>
        public string Invoke(JObject command)
    {
        try
        {
            string name = command["name"]?.ToString();
            string method = command["method"]?.ToString();
            JObject parameters = command["parameters"] as JObject ?? new JObject();
            
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(method))
            {
                return "命令格式不正确: 缺少name或method字段";
            }
            
            // 查找对应的设备
            if (!_things.TryGetValue(name, out Thing thing))
            {
                return $"未找到设备: {name}";
            }
            
            // 执行命令
            return thing.Invoke(method, parameters);
        }
        catch (Exception ex)
        {
            return $"执行命令失败: {ex.Message}";
        }
    }
        // 添加设备
        public void AddThing(Thing thing)
        {
            if (!_things.ContainsKey(thing.Id))
            {
                _things.Add(thing.Id, thing);
                Console.WriteLine($"添加设备: {thing.Name} (ID: {thing.Id})");
            }
        }

        // 移除设备
        public void RemoveThing(string thingId)
        {
            if (_things.ContainsKey(thingId))
            {
                _things.Remove(thingId);
                Console.WriteLine($"移除设备: {thingId}");
            }
        }

        // 获取设备
        public Thing GetThing(string thingId)
        {
            return _things.TryGetValue(thingId, out var thing) ? thing : null;
        }

        // 获取所有设备
        public List<Thing> GetAllThings()
        {
            return _things.Values.ToList();
        }



        // 获取所有设备描述的JSON
        public string GetDescriptorsJson()
        {
            var descriptorsObj = new JObject();
            var descriptorsArray = new JArray();
            
            foreach (var thing in _things.Values)
            {
                descriptorsArray.Add(JObject.FromObject(thing.GetDescriptor()));
            }
            
            descriptorsObj["type"] = "iot";
            descriptorsObj["descriptors"] = descriptorsArray;
            
            return descriptorsObj.ToString(Formatting.None);
        }

        // 获取所有设备状态的JSON
        public string GetStatesJson()
        {
            // 创建一个数组来存储设备状态
            var statesArray = new JArray();
            
            foreach (var thing in _things.Values)
            {
                // 创建包含 name 和 state 的对象
                var thingState = new JObject
                {
                    ["name"] = ID,
                    ["state"] = JObject.FromObject(thing.GetState())
                };
                
                statesArray.Add(thingState);
            }
            
            // 创建最终的 JSON 对象，直接将 statesArray 赋值给 states 字段
            var result = new JObject
            {
                ["session_id"] = null,  // 实际使用时可能需要传入会话ID
                ["type"] = "iot",
                ["states"] = statesArray
            };
            Console.WriteLine(result.ToString());
            return result.ToString(Formatting.None);
        }
    }
}