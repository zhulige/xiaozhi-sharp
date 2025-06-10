# Windows平台实现说明

## 功能特性

### 音频服务
- ✅ **录音功能**：使用NAudio WaveInEvent进行音频录制
- ✅ **播放功能**：使用NAudio WaveOutEvent进行音频播放
- ✅ **静音检测**：RMS算法检测静音，避免发送空白音频
- ✅ **PCM音频处理**：支持16位单声道PCM音频数据
- ✅ **缓冲播放**：5秒音频缓冲区，支持流式播放

### 音频参数
- **录音采样率**: 16kHz
- **播放采样率**: 24kHz
- **位深**: 16位
- **声道**: 单声道
- **帧时长**: 60毫秒

### 权限配置
已在`Package.appxmanifest`中添加必要权限：
- `microphone` - 麦克风录音权限
- `runFullTrust` - 完全信任权限

## 技术实现

### 依赖库
- **NAudio** - Windows音频处理库
- **Microsoft.Maui.Controls** - MAUI框架
- **XiaoZhiSharp** - 核心SDK

### 架构设计
```
AudioService (Windows)
├── WaveInEvent (录音)
├── WaveOutEvent (播放)
├── BufferedWaveProvider (缓冲)
└── 静音检测算法
```

### 平台注册
在`XiaoZhi_AgentService.cs`中通过`DeviceInfo.Platform == DevicePlatform.WinUI`检测Windows平台并注册相应的音频服务。

## 使用说明

1. **构建项目**：
   ```bash
   dotnet build --framework net9.0-windows10.0.19041.0
   ```

2. **运行应用**：
   ```bash
   dotnet run --framework net9.0-windows10.0.19041.0
   ```

3. **功能测试**：
   - 确保麦克风已连接并授权
   - 测试录音和播放功能
   - 验证与小智服务器的语音交互

## 注意事项

- 需要Windows 10 build 19041.0或更高版本
- 首次运行时需要授权麦克风权限
- 建议在有良好音频设备的环境下测试 