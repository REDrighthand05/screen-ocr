# Screen OCR

让纯文本大模型看见你的屏幕。

任何能够调用 exe 并读取其输出的平台（Codex、Cursor、Claude Desktop 等），装上 ScreenOcr.exe + ScreenClicker.exe，就能让物美价廉的纯文本模型获得「感知 - 思考 - 行动 - 验证」的完整闭环。

```powershell
& .\ScreenOcr.exe
```

输出示例：
```
[OK] 截图 1536x864 | OCR 12 行 | 共 345 字符
  (450,380)-(510,408)  "确定"
  (450,420)-(510,448)  "取消"
```

配套点击器：https://github.com/REDrighthand05/screen-clicker

## 技术栈
C# (.NET 8.0) | Windows.Media.Ocr | 零外部依赖 | Windows 10/11

## License
MIT
