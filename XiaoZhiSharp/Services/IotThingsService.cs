using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace XiaoZhiSharp.Services;

#region 基础实现基类
// 特性定义 ---------------------------------------------------
[AttributeUsage(AttributeTargets.Property)]
public class IoTPropertyAttribute : Attribute
{
    public string Description { get; }
    public string Type { get; }

    public IoTPropertyAttribute(string description, string type)
    {
        Description = description;
        Type = type;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class IoTMethodAttribute : Attribute
{
    public string Description { get; }

    public IoTMethodAttribute(string description)
    {
        Description = description;
    }
}

// 设备基类 ---------------------------------------------------
public abstract class IoTDevice
{
    [JsonIgnore]
    public abstract string DeviceName { get; }

    [JsonIgnore]
    public abstract string Description { get; }

    // 新增状态获取方法
    public Dictionary<string, object> GetCurrentState()
    {
        var state = new Dictionary<string, object>();
        foreach (var prop in GetType().GetProperties())
        {
            if (prop.GetCustomAttribute<IoTPropertyAttribute>() != null)
            {
                state[prop.Name] = prop.GetValue(this);
            }
        }
        return state;
    }

    public Dictionary<string, object> GetProperties()
    {
        var props = new Dictionary<string, object>();
        foreach (var prop in GetType().GetProperties())
        {
            var attr = prop.GetCustomAttribute<IoTPropertyAttribute>();
            if (attr != null)
            {
                props[prop.Name] = new
                {
                    description = attr.Description,
                    type = attr.Type
                };
            }
        }
        return props;
    }

    public Dictionary<string, object> GetMethods()
    {
        var methods = new Dictionary<string, object>();
        foreach (var method in GetType().GetMethods())
        {
            var attr = method.GetCustomAttribute<IoTMethodAttribute>();
            if (attr != null)
            {
                var parameters = new Dictionary<string, object>();
                foreach (var param in method.GetParameters())
                {
                    parameters[param.Name] = new
                    {
                        description = param.Name + " parameter",
                        type = param.ParameterType.Name.ToLower()
                    };
                }

                methods[method.Name] = new
                {
                    description = attr.Description,
                    parameters
                };
            }
        }
        return methods;
    }
}

// 状态反馈
public class StateReport
{
    [JsonProperty("session_id")]
    public string SessionId { get; set; } = "";

    [JsonProperty("type")]
    public string Type { get; } = "iot";

    [JsonProperty("states")]
    public List<DeviceState> States { get; set; } = new List<DeviceState>();
}

public class DeviceState
{
    [JsonProperty("name")]
    public string DeviceName { get; set; }

    [JsonProperty("state")]
    public Dictionary<string, object> State { get; set; }
}
#endregion


#region 具体实例实现类
public class Lamp : IoTDevice
{
    public override string DeviceName => "Lamp";
    public override string Description => "一个测试用的灯";

    [IoTProperty("灯是否打开", "boolean")]
    public bool Power { get; private set; }

    [IoTMethod("打开灯")]
    public void TurnOn()
    {
        Power = true;
        Console.WriteLine("灯已打开");
    }

    [IoTMethod("关闭灯")]
    public void TurnOff() 
    {
        Power = false;
        Console.WriteLine("灯已闭灯");
    }
}

public class DuoJI : IoTDevice
{
    public override string DeviceName => "DuoJI";
    public override string Description => "一个ID为1号的180度舵机";

    [IoTProperty("当前角度", "number")]
    public double Angle { get; private set; }

    [IoTMethod("控制舵机转动到多少度数")]
    public void TurnAngle(double angles)
    {
        Angle = Math.Clamp(angles, 0, 180);
        Console.WriteLine($"舵机转动到 {Angle} 度");
    }
}

public class Camre : IoTDevice
{
    public override string DeviceName => "Camre";
    public override string Description => "AI机器人的摄像头眼睛";

    [IoTProperty("摄像头是否已经打开", "boolean")]
    public bool Open { get; private set; }

    [IoTProperty("视觉识别状态", "boolean")]
    public bool VL { get; private set; }

    [IoTMethod("打开摄像头")]
    public void TurnOn() => Open = true;

    [IoTMethod("关闭摄像头")]
    public void TurnOff() => Open = false;

    [IoTMethod("推理当前视觉内容")]
    public void VLProcessing() => VL = true;
}

#endregion


#region Iot服务端返回调用处理
// 序列化模型 -------------------------------------------------
public class IoTDescriptor
{
    public string session_id = "";
    public string type = "iot";
    public List<object> descriptors = new List<object>();

    public void AddDevice(IoTDevice device)
    {
        descriptors.Add(new
        {
            name = device.DeviceName,
            description = device.Description,
            properties = device.GetProperties(),
            methods = device.GetMethods()
        });
    }
}

// 命令处理器 -------------------------------------------------
public class IoTCommandHandler
{
    private readonly Dictionary<string, IoTDevice> devices;
    private string currentSessionId = "";
    public IoTCommandHandler(params IoTDevice[] devices)
    {
        this.devices = new Dictionary<string, IoTDevice>();
        foreach (var device in devices)
        {
            this.devices[device.DeviceName] = device;
        }
    }
    public (bool Success, string StateJson) HandleCommand(string json)
    {
        try
        {
            var command = JsonConvert.DeserializeObject<CommandRequest>(json);
            if (command == null)
            {
                Console.WriteLine("DeserializeObject为空");
                return (false, "");
            }
            currentSessionId = command.session_id;
            foreach (var cmd in command.commands)
            {
                if (devices.TryGetValue(cmd.name, out var device))
                {
                    var method = device.GetType().GetMethod(cmd.method);
                    if (method != null)
                    {
                        var parameters = method.GetParameters();
                        var args = new object[parameters.Length];

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (cmd.parameters.TryGetValue(parameters[i].Name, out var value))
                            {
                                args[i] = Convert.ChangeType(value, parameters[i].ParameterType);
                            }
                        }

                        method.Invoke(device, args);
                        Console.WriteLine($"[执行成功] {cmd.name}.{cmd.method}");
                    }
                    else
                    {
                        Console.WriteLine($"[错误] 未找到方法: {cmd.method}");
                    }
                }
                else
                {
                    Console.WriteLine($"[错误] 未找到设备: {cmd.name}");
                }
            }
            return (true, GenerateStateReport());
        }
        catch (Exception ex)
        {
            return (false, $"{{\"error\":\"{ex.Message}\"}}");
        }

    }
    private string GenerateStateReport()
    {
        var report = new StateReport
        {
            SessionId = currentSessionId,
            States = new List<DeviceState>()
        };

        foreach (var devicePair in devices)
        {
            report.States.Add(new DeviceState
            {
                DeviceName = devicePair.Key,
                State = devicePair.Value.GetCurrentState()
            });
        }

        return JsonConvert.SerializeObject(report, Formatting.Indented);
    }
}


// 命令模型 ---------------------------------------------------
public class CommandRequest
{
    public string type;
    public List<Command> commands;
    public string session_id;
}

public class Command
{
    public string name;
    public string method;
    public Dictionary<string, object> parameters;
}
#endregion

