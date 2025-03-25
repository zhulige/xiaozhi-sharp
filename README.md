


# xiaozhi-sharp 小智 AI 聊天机器人 (XiaoZhi AI Chatbot)

```
using XiaoZhiSharp;
using XiaoZhiSharp.Protocols;

XiaoZhiAgent _xiaoZhiAgent = new XiaoZhiAgent(OTA_VERSION_URL, WEB_SOCKET_URL, MAC_ADDR);
_xiaoZhiAgent.OnMessageEvent += _xiaoZhiAgent_OnMessageEvent;
_xiaoZhiAgent.OnIotEvent += _xiaoZhiAgent_OnIotEvent;
_xiaoZhiAgent.OnAudioEvent += _xiaoZhiAgent_OnAudioEvent;
_xiaoZhiAgent.Start();
```

##  [English](#english-version) | [中文](#中文版本)

---

## English Version

[![SVG Banners](https://svg-banners.vercel.app/api?type=origin&text1=Hi😃，XiaoZhi&text2=XiaoZhi_AI_Chatbot_Client_written_in_CSharp&width=830&height=210)](https://github.com/xinnan-tech/xiaozhi-esp32-server)

## Project Introduction
xiaozhi-sharp is a meticulously crafted XiaoZhi client in C#, which not only serves as an excellent code learning example but also allows you to easily experience the intelligent interaction brought by XiaoZhi AI without the need for related hardware.  
This client defaults to connecting to the [xiaozhi.me](https://xiaozhi.me/) official server, providing you with stable and reliable services.

## XiaoZhi AI Server Debugging Tool
Outputs all commands and lets you understand how XiaoZhi works. Why wait? Just use it!<br>
<br>
<img src="doc/202503101011.png" width="480" />

## XiaoZhi AI Console Client
<img src="doc/202503101010.png" width="480" />

## Running Guide
To run this project, follow the steps below:

## Prerequisites
Ensure that your system has installed the .NET Core SDK. If not installed, you can download and install the version suitable for your system from the [official website](https://dotnet.microsoft.com/zh-cn/).

## Running the Project:
After successful compilation, use the following command to run the project:
```bash
dotnet run
```

After the project starts, you will see relevant information output to the console. Follow the prompts to start chatting with XiaoZhi AI.

## Notes
Ensure that your network connection is stable to use XiaoZhi AI smoothly.  
If you encounter any issues during the process, first check the error messages output to the console or verify if the project configuration is correct, such as whether the global variable `MAC_ADDR` has been modified as required.

## Contributions and Feedback
If you find any issues with the project or have suggestions for improvement, feel free to submit an Issue or Pull Request. Your feedback and contributions are essential for the development and improvement of the project.

### Join the Community
Welcome to join our community to share experiences, propose suggestions, or get help!

<div style="text-align: center;">
    <img src="https://fileserver.developer.huaweicloud.com/FileServer/getFile/communitytemp/20250320/community/289/905/458/0001739151289905458.20250320010018.32864321130799519033275788702529:20250320020019:2415:1BF2B548196B8C212002694F96BAF79F8EB068E88A639E85BD05FCCFC574D788.jpg" height="300" />
</div>

---

## 中文版本

[![SVG Banners](https://svg-banners.vercel.app/api?type=origin&text1=你好😃，小智&text2=CSharp编写的小智AI智能体客户端&width=830&height=210)](https://github.com/xinnan-tech/xiaozhi-esp32-server)

## 项目简介
xiaozhi-sharp 是一个用 C# 精心打造的小智客户端，它不仅可以作为代码学习的优质示例，还能让你在没有相关硬件条件的情况下，轻松体验到小智 AI 带来的智能交互乐趣。  
本客户端默认接入 [xiaozhi.me](https://xiaozhi.me/) 官方服务器，为你提供稳定可靠的服务。

## 小智AI服务器调试利器
输出全部指令、让你了解小智的工作原理。拿来就能用还等什么！<br>
<br>
<img src="doc/202503101011.png" width="480" />

## 小智AI 控制台客户端
<img src="doc/202503101010.png" width="480" />

## 运行指南
要运行本项目，你需要按照以下步骤操作：

## 前提条件
确保你的系统已经安装了 .NET Core SDK。如果尚未安装，可以从 [官方网站](https://dotnet.microsoft.com/zh-cn/) 下载并安装适合你系统的版本。

## 运行项目：
编译成功后，使用以下命令运行项目：
```bash
dotnet run
```

项目启动后，你将看到控制台输出相关信息，按照提示进行操作，即可开始与小智 AI 进行畅快的聊天互动。

## 注意事项
请确保你的网络连接正常，这样才能顺利使用小智AI。  
在运行过程中，如果遇到任何问题，可以先查看控制台输出的错误信息，或者检查项目的配置是否正确，例如全局变量 `MAC_ADDR` 是否已经按照要求进行修改。

## 贡献与反馈
如果你在使用过程中发现了项目中的问题，或者有任何改进的建议，欢迎随时提交 Issue 或者 Pull Request。你的反馈和贡献将对项目的发展和完善起到重要的作用。

### 加入社群
欢迎加入我们的社区，分享经验、提出建议或获取帮助！

<div style="text-align: center;">
    <img src="https://fileserver.developer.huaweicloud.com/FileServer/getFile/communitytemp/20250320/community/289/905/458/0001739151289905458.20250320010018.32864321130799519033275788702529:20250320020019:2415:1BF2B548196B8C212002694F96BAF79F8EB068E88A639E85BD05FCCFC574D788.jpg" height="300" />
</div>

