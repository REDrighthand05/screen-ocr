# Screen OCR

![Windows](https://img.shields.io/badge/platform-Windows-blue)
![C#](https://img.shields.io/badge/language-C%23-178600)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)
![Version](https://img.shields.io/badge/version-0.2.0-orange)

**让 AI 看见你的屏幕。** Screen OCR 是一个纯本地的屏幕文字识别工具，利用 Windows 内置的 OCR 引擎 (Windows.Media.Ocr)，无需网络、无需 API Key、无需安装第三方依赖。

---

## ✨ 特性

* **纯本地运行** — 零网络请求，所有数据留在本机
* **零依赖** — 只需 Windows 10+ 系统自带 OCR 语言包，不需要 Python、Node.js 或任何第三方库
* **全屏/区域扫描** — `--region x,y,w,h` 指定区域，只扫你需要的那块屏幕
* **多种输出格式** — 详细坐标、纯文本(`--compact`)、结构化 JSON(`--json`)
* **连续监控** — `--every N` 每 N 毫秒自动扫描一次，追踪屏幕变化
* **多语言** — `--lang en-US` 切换英文识别
* **MCP Server** — 内置 Model Context Protocol 支持，可直接注册为 Codex Desktop 工具

---

## 🚀 快速开始

### 从源码运行

```powershell
# 全屏 OCR（详细坐标+文字）
dotnet run --project src/core.csproj

# 纯文本模式（简洁输出）
dotnet run --project src/core.csproj -- --compact

# 扫描屏幕左上角 400x300 区域
dotnet run --project src/core.csproj -- --region 0,0,400,300 --compact

# 每 5 秒扫描一次（监控模式）
dotnet run --project src/core.csproj -- --every 5000 --compact

# 输出为 JSON（供程序解析）
dotnet run --project src/core.csproj -- --json
```

### 使用编译好的可执行文件

```powershell
.\output\ScreenOcr.exe --compact
.\output\ScreenOcr.exe --region 200,100,600,400
```

### MCP Server 模式

```powershell
.\output\ScreenOcrMcp.exe
# stdin/stdout JSON-RPC 协议通信
```

---

## 🔌 注册为 Codex Desktop 插件

在 `~/.codex/config.toml` 中添加：

```toml
[mcp_servers."screen-ocr"]
command = "D:\\path\\to\\ScreenOcrMcp.exe"
args = []
```

重启 Codex Desktop 后即可在对话中直接调用 `screen_ocr` 工具。

---

## 📋 参数说明

| 参数 | 说明 | 示例 |
|------|------|------|
| `--region x,y,w,h` | 指定扫描区域 | `--region 100,200,400,300` |
| `--lang zh-CN/en-US` | 识别语言选择 | `--lang en-US` |
| `--compact` | 纯文本输出(无坐标) | `--compact` |
| `--json` | JSON 结构化输出 | `--json` |
| `--every N` | 连续监控间隔(毫秒) | `--every 5000` |

---

## 🏗 架构

```
ScreenOcr.exe
├── ScreenshotCapture   全屏/区域截图
├── OcrEngineWrapper    WinRT OCR 引擎封装
├── OcrResultFormatter  三种输出格式
└── OcrProcessor        调度器(截图→OCR→格式化)

ScreenOcrMcp.exe (MCP Server)
└── stdin/stdout JSON-RPC 供 AI agent 调用
```

---

## 📄 License

MIT