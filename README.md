# Screen OCR

让 AI agent 看见你的屏幕。纯本地、零依赖、毫秒级。

## 用法

ScreenOcr.exe 截取当前屏幕 → Windows.Media.Ocr → 输出文字 + 坐标。

`powershell
& ".\bin\ScreenOcr.exe"
# [OK] 截图 1536x864 | OCR 12 行 | 共 345 字符
#   (450,380)-(510,408)  "确定"
#   (450,420)-(510,448)  "取消"
`

## 配合 AI Agent

1. Agent 调用 ScreenOcr.exe → 得到屏幕文字
2. Agent 理解文字 → 决定下一步
3. Agent 调用 ScreenClicker.exe → 执行点击

## 许可证 MIT
