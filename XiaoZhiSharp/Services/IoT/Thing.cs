using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XiaozhiAI.Models.IoT
{
    public abstract class Thing
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public Dictionary<string, object> States { get; protected set; }
        public Dictionary<string, object> Properties { get; protected set; }

        protected Thing(string id, string name, string description = "")
        {
            Id = id;
            Name = name;
            Description = description;
            States = new Dictionary<string, object>();
            Properties = new Dictionary<string, object>();
        }

        // 修改返回类型为 string
        public virtual string Invoke(string actionId, JObject parameters)
        {
            return $"未实现的动作: {actionId}";
        }

        // 其他方法保持不变
        public virtual Dictionary<string, object> GetDescriptor()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Id,  // 使用 Id 作为名称，因为服务端可能期望使用这个作为标识符
                ["description"] = Description,
                ["properties"] = GetFormattedProperties(),
                ["methods"] = GetMethods()
            };
        }

        // 格式化属性为服务端期望的格式
        protected virtual Dictionary<string, object> GetFormattedProperties()
        {
            var formattedProps = new Dictionary<string, object>();
            
            foreach (var prop in Properties)
            {
                var propInfo = prop.Value as Dictionary<string, object>;
                if (propInfo != null)
                {
                    // 创建一个新的属性描述，只包含服务端期望的字段
                    var formattedProp = new Dictionary<string, object>
                    {
                        ["description"] = $"{Name}的{prop.Key}属性",
                        ["type"] = propInfo.ContainsKey("type") ? propInfo["type"] : "string"
                    };
                    
                    formattedProps[prop.Key] = formattedProp;
                }
            }
            
            return formattedProps;
        }

        // 添加获取设备支持的方法列表
        protected virtual Dictionary<string, object> GetMethods()
        {
            // 默认实现，子类可以覆盖此方法提供具体的方法列表
            return new Dictionary<string, object>
            {
                ["turn_on"] = new Dictionary<string, object>
                {
                    ["description"] = "打开设备",
                    ["parameters"] = new Dictionary<string, object>()
                },
                ["turn_off"] = new Dictionary<string, object>
                {
                    ["description"] = "关闭设备",
                    ["parameters"] = new Dictionary<string, object>()
                }
            };
        }

        public virtual Dictionary<string, object> GetState()
        {
            return States;
        }
    }
}