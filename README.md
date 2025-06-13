# 小智Sharp SDK & Client

<p>
  <a href="https://github.com/zhulige/xiaozhi-sharp/releases/latest">
    <img src="https://img.shields.io/github/v/release/zhulige/xiaozhi-sharp?style=flat-square&logo=github&color=blue" alt="Release"/>
  </a>
  <a href="https://opensource.org/licenses/MIT">
    <img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License: MIT"/>
  </a>
  <a href="https://github.com/zhulige/xiaozhi-sharp/stargazers">
    <img src="https://img.shields.io/github/stars/zhulige/xiaozhi-sharp?style=flat-square&logo=github" alt="Stars"/>
  </a>
  <a href="https://github.com/zhulige/xiaozhi-sharp/releases/latest">
    <img src="https://img.shields.io/github/downloads/zhulige/xiaozhi-sharp/total?style=flat-square&logo=github&color=52c41a1&maxAge=86400" alt="Download"/>
  </a>
</p>

## 项目简介 
XiaoZhiSharp 是使用 C# 语言编写的 “小智SDK”，并提供了ConsoleApp 应用示例。

**跨平台支持**：本项目支持以下平台：
- **操作系统**：Windows、MacOS、Linux、Android、IOS
- **硬件平台**：x86、x86_64、arm、arm_64
- **开发板**：ASUS Tinker Board2s、Raspberry Pi
- **服务器地址** https://xiaozhi.me

## 功能
- ChatMessage
- ChatAudio
- ChatAbort
- McpTool

## 示例
``` C#
using XiaoZhiSharp;
using XiaoZhiSharp.Protocols;

XiaoZhiAgent agent = new XiaoZhiAgent();
agent.OnMessageEvent += Agent_OnMessageEvent;
agent.OnOtaEvent += Agent_OnOtaEvent;
await agent.Start(); // 会自动进行OTA检查，然后连接WebSocket

private static Task Agent_OnMessageEvent(string type, string message)
{
    LogConsole.InfoLine($"[{type}] {message}");
    return Task.CompletedTask;
}

private static Task Agent_OnOtaEvent(OtaResponse? otaResponse)
{
    if (otaResponse != null)
    {
        LogConsole.InfoLine("OTA检查完成，获取到服务器配置信息");
        // 处理OTA响应数据，包括WebSocket URL、Token、MQTT配置等
    }
    return Task.CompletedTask;
}
```

详见 XiaoZhiSharp_ConsoleApp 项目。

## NuGet
```
dotnet add package XiaoZhiSharp --version 1.0.6
```

## 相关资源
https://opus-codec.org/downloads/

## 贡献与反馈
如果你在使用过程中发现了项目中的问题，或者有任何改进的建议，欢迎随时提交 Issue 或者 Pull Request。你的反馈和贡献将对项目的发展和完善起到重要的作用。

## 加入社群
欢迎加入我们的社区，分享经验、提出建议或获取帮助！

## 项目统计
[![Star History Chart](https://api.star-history.com/svg?repos=zhulige/xiaozhi-sharp&type=Date)](https://www.star-history.com/#zhulige/xiaozhi-sharp&Date)
