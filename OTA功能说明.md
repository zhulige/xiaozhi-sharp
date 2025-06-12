# XiaoZhiSharp OTA功能说明

## 概述

XiaoZhiSharp 现在支持完整的 OTA（Over-The-Air）功能，按照小智设备的OTA协议标准实现。OTA功能在连接WebSocket之前自动执行，获取服务器配置信息，包括WebSocket URL、Token、MQTT配置、激活信息等。

## 功能特性

- ✅ 自动OTA检查：启动时自动进行OTA检查
- ✅ 动态配置：根据OTA响应自动配置WebSocket URL和Token
- ✅ 设备信息上报：支持MAC地址、Client ID、User-Agent等信息上报
- ✅ WiFi信息上报：支持上报WiFi连接信息（SSID、信号强度等）
- ✅ 固件更新检查：检查是否有可用的固件更新
- ✅ MQTT配置获取：获取MQTT服务器配置信息
- ✅ 时间同步：获取服务器时间信息
- ✅ 设备激活：支持设备激活码和消息

## 快速开始

### 基本使用

```csharp
using XiaoZhiSharp;
using XiaoZhiSharp.Protocols;

// 创建Agent实例
var agent = new XiaoZhiAgent();

// 订阅OTA事件
agent.OnOtaEvent += async (otaResponse) =>
{
    if (otaResponse != null)
    {
        Console.WriteLine("OTA检查成功");
        
        // 检查固件更新
        if (otaResponse.Firmware?.Url != null)
        {
            Console.WriteLine($"发现固件更新: {otaResponse.Firmware.Version}");
        }
        
        // 获取激活信息
        if (otaResponse.Activation != null)
        {
            Console.WriteLine($"激活码: {otaResponse.Activation.Code}");
        }
    }
};

// 启动（会自动进行OTA检查）
await agent.Start();
```

### 自定义设备信息

```csharp
var agent = new XiaoZhiAgent();

// 自定义设备信息
agent.DeviceId = "00:11:22:33:44:55";
agent.ClientId = "your-custom-client-id";
agent.UserAgent = "my-device/1.0.0";
agent.CurrentVersion = "1.0.0";

// 自定义OTA URL
agent.OtaUrl = "https://your-server.com/api/ota/";

await agent.Start();
```

### 带WiFi信息的OTA检查

```csharp
var agent = new XiaoZhiAgent();

// 手动进行OTA检查并上报WiFi信息
var otaResponse = await agent.CheckOtaUpdateWithWifi(
    ssid: "My-WiFi",
    rssi: -45,
    channel: 6,
    ip: "192.168.1.100"
);
```

### 仅OTA检查（不启动WebSocket）

```csharp
var agent = new XiaoZhiAgent();

// 仅进行OTA检查
var otaResponse = await agent.CheckOtaUpdate();

if (otaResponse != null)
{
    // 处理OTA响应
    Console.WriteLine($"服务器时间: {DateTimeOffset.FromUnixTimeMilliseconds(otaResponse.ServerTime?.Timestamp ?? 0)}");
}
```

## OTA请求数据结构

### 基本请求字段

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| application | object | ✅ | 应用程序信息 |
| application.version | string | ✅ | 当前版本号 |
| application.elf_sha256 | string | ✅ | 固件哈希值 |
| board | object | ✅ | 开发板信息 |
| board.type | string | ✅ | 开发板类型 |
| board.name | string | ✅ | 开发板名称 |
| mac_address | string | ❌ | MAC地址 |
| uuid | string | ❌ | 客户端UUID |

### HTTP请求头

| 请求头 | 必填 | 说明 |
|--------|------|------|
| Device-Id | ✅ | 设备唯一标识（MAC地址） |
| Client-Id | ✅ | 客户端UUID |
| User-Agent | ✅ | 客户端名称和版本 |
| Accept-Language | ❌ | 客户端语言 |

## OTA响应数据结构

### 成功响应

```json
{
  "activation": {
    "code": "激活码",
    "message": "激活消息"
  },
  "mqtt": {
    "endpoint": "mqtt.example.com",
    "client_id": "GID_test@@@device-id@@@uuid",
    "username": "device_12345",
    "password": "password",
    "publish_topic": "device-server"
  },
  "websocket": {
    "url": "wss://api.tenclass.net/xiaozhi/v1/",
    "token": "test-token"
  },
  "server_time": {
    "timestamp": 1633024800000,
    "timezone": "Asia/Shanghai",
    "timezone_offset": -480
  },
  "firmware": {
    "version": "1.0.0",
    "url": "https://example.com/firmware/1.0.0.bin"
  }
}
```

### 错误响应

```json
{
  "error": "错误信息"
}
```

## 事件处理

### OnOtaEvent 事件

OTA检查完成后触发，参数为 `OtaResponse?` 类型。

```csharp
agent.OnOtaEvent += async (otaResponse) =>
{
    if (otaResponse != null)
    {
        // OTA检查成功
        
        // WebSocket配置
        if (otaResponse.WebSocket != null)
        {
            Console.WriteLine($"WebSocket: {otaResponse.WebSocket.Url}");
        }
        
        // MQTT配置
        if (otaResponse.Mqtt != null)
        {
            Console.WriteLine($"MQTT: {otaResponse.Mqtt.Endpoint}");
        }
        
        // 固件信息
        if (otaResponse.Firmware != null)
        {
            Console.WriteLine($"固件版本: {otaResponse.Firmware.Version}");
            if (!string.IsNullOrEmpty(otaResponse.Firmware.Url))
            {
                Console.WriteLine($"固件下载: {otaResponse.Firmware.Url}");
            }
        }
        
        // 激活信息
        if (otaResponse.Activation != null)
        {
            Console.WriteLine($"激活码: {otaResponse.Activation.Code}");
        }
        
        // 服务器时间
        if (otaResponse.ServerTime != null)
        {
            var time = DateTimeOffset.FromUnixTimeMilliseconds(otaResponse.ServerTime.Timestamp);
            Console.WriteLine($"服务器时间: {time}");
        }
    }
    else
    {
        // OTA检查失败，使用默认配置
        Console.WriteLine("OTA检查失败，使用默认配置");
    }
};
```

## 工作流程

1. **初始化**: 创建 `XiaoZhiAgent` 实例
2. **OTA检查**: 调用 `Start()` 方法时自动进行OTA检查
3. **获取配置**: 从OTA响应中获取WebSocket URL、Token等配置
4. **连接WebSocket**: 使用OTA响应的配置信息连接WebSocket服务器
5. **正常工作**: 开始语音聊天功能

## 注意事项

- OTA检查失败时，会使用默认配置继续启动
- 可以通过 `LatestOtaResponse` 属性获取最新的OTA响应数据
- 支持手动调用 `CheckOtaUpdate()` 或 `CheckOtaUpdateWithWifi()` 进行OTA检查
- OTA服务会自动处理HTTP请求重试和超时
- 建议在生产环境中正确设置 `DeviceId`、`ClientId` 和 `UserAgent`

## 示例代码

完整的示例代码请参考 `XiaoZhiSharp/OtaExample.cs` 文件。 