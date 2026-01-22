# GrassMidi

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![Language](https://img.shields.io/badge/Language-C%23-239120?style=flat-square&logo=csharp)
![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows)

MIDI 桥接工具。连接 MIDI 控制器与 OBS Studio / 系统功能。
用于把 MIDI 推子、旋钮、按键映射为 OBS 操作、媒体控制或系统命令。

## 功能

- **OBS**: 音量/静音、切场景、推流/录制、设前台窗口为源。
- **媒体**: 切歌、播放/暂停。
- **系统**: 主音量、运行程序、模拟按键。
- **网络**: HTTP 请求 (Webhook)。
- **配置**: Web 界面管理，自动识别 MIDI 信号。

## 技术

- .NET 8 (C#)
- NAudio
- OBS WebSocket 5.0
- ASP.NET Core (Web UI)
- Windows API

## 使用

### 准备
- Windows 10/11
- .NET 8 Runtime
- OBS Studio 28+ (开启 WebSocket)

### 运行
1. 下载 Release 解压。
2. 运行 `GrassMidi.exe`。
3. 浏览器打开 `http://localhost:5000`。
4. **设置**里选 MIDI 设备，填 OBS 信息。
5. **添加**里操作 MIDI 硬件，选功能，保存。

## 绑定类型

| 类型 | 功能 | 目标参数 |
|------|------|----------|
| **SystemVolume** | 系统主音量 | - |
| **ObsVolume** | OBS 源音量 | 源名称 |
| **ObsMute** | OBS 源静音 | 源名称 |
| **ObsSwitchScene** | 切场景 | 场景名 |
| **ObsSetForegroundWindow**| 前台窗口设为源 | 源名称 |
| **MediaPlayPause** | 媒体播放/暂停 | - |
| **RunProcess** | 运行程序 | 路径 (数据填参数) |
| **HttpRequest** | HTTP 请求 | URL (数据填 Method) |

## 协议

MIT License.
