using Windows.Media.Ocr;
using Windows.Graphics.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.Text;

namespace OcrCore;

public class ScreenshotCapture
{
    public static (Bitmap bitmap, int w, int h) CaptureFullScreen()
    {
        var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
        var bmp = new Bitmap(bounds.Width, bounds.Height);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
        return (bmp, bounds.Width, bounds.Height);
    }

    public static (Bitmap bitmap, int w, int h) CaptureRegion(int x, int y, int w, int h)
    {
        var bmp = new Bitmap(w, h);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.CopyFromScreen(x, y, 0, 0, new Size(w, h));
        return (bmp, w, h);
    }
}

public class OcrEngineWrapper
{
    private OcrEngine _engine;

    public OcrEngineWrapper(string langCode = "zh-CN")
    {
        var lang = new Windows.Globalization.Language(langCode);
        _engine = OcrEngine.TryCreateFromLanguage(lang)
                  ?? OcrEngine.TryCreateFromUserProfileLanguages()
                  ?? throw new Exception("OCR engine unavailable");
    }

    public async Task<OcrResult> RecognizeAsync(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        var ras = ms.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(ras);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
        return await _engine.RecognizeAsync(softwareBitmap);
    }
}

public class OcrResultFormatter
{
    public static string FormatCompact(OcrResult result, int w, int h)
    {
        if (result.Lines.Count == 0) return "(no text)";
        return $"[OCR {w}x{h} | {result.Lines.Count} lines | {result.Text.Length} chars]\n{result.Text.Trim()}";
    }

    public static string FormatDetailed(OcrResult result, int w, int h)
    {
        if (result.Lines.Count == 0) return "[OK] Screenshot captured, no text detected";

        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"[OK] Screen {w}x{h} | OCR {result.Lines.Count} lines | {result.Text.Length} chars");
        lines.AppendLine("--- coords text ---");

        foreach (var line in result.Lines)
        {
            var fw = line.Words[0];
            var lw = line.Words[^1];
            var rx = (int)(lw.BoundingRect.X + lw.BoundingRect.Width);
            var by = (int)(fw.BoundingRect.Y + fw.BoundingRect.Height);
            lines.AppendLine($"  ({fw.BoundingRect.X},{fw.BoundingRect.Y})-({rx},{by}) \"{line.Text}\"");
        }

        lines.AppendLine("--- full text ---");
        lines.Append(result.Text.Trim());
        return lines.ToString();
    }

    public static string FormatJson(OcrResult result, int w, int h)
    {
        var data = result.Lines.Select(l => new {
            text = l.Text,
            x = (int)l.Words[0].BoundingRect.X,
            y = (int)l.Words[0].BoundingRect.Y,
            w_rect = (int)(l.Words[^1].BoundingRect.X + l.Words[^1].BoundingRect.Width - l.Words[0].BoundingRect.X),
            h_rect = (int)(l.Words[0].BoundingRect.Y + l.Words[0].BoundingRect.Height - l.Words[0].BoundingRect.Y)
        }).ToList();
        return System.Text.Json.JsonSerializer.Serialize(new { width = w, height = h, lines = data, full_text = result.Text?.Trim() });
    }
}

public class OcrProcessor
{
    public static async Task<string> Run(string langCode, bool compact, bool json, int? rx, int? ry, int? rw, int? rh)
    {
        var (bitmap, w, h) = (rx.HasValue && ry.HasValue && rw.HasValue && rh.HasValue)
            ? ScreenshotCapture.CaptureRegion(rx.Value, ry.Value, rw.Value, rh.Value)
            : ScreenshotCapture.CaptureFullScreen();

        using (bitmap)
        {
            var engine = new OcrEngineWrapper(langCode);
            var result = await engine.RecognizeAsync(bitmap);

            if (json) return OcrResultFormatter.FormatJson(result, w, h);
            if (compact) return OcrResultFormatter.FormatCompact(result, w, h);
            return OcrResultFormatter.FormatDetailed(result, w, h);
        }
    }
}

// 閳光偓閳光偓 Window Info: 鐠囧棗鍩嗗В蹇氼攽閺傚洤鐡ч幍鈧仦鐐垫畱缁愭褰?閳光偓閳光偓

public static class WindowEnumerator
{
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    public class WindowEntry
    {
        public string Title { get; set; } = "";
        public int X { get; set; } public int Y { get; set; }
        public int W { get; set; } public int H { get; set; }
    }

    public static List<WindowEntry> Enumerate()
    {
        var list = new List<WindowEntry>();
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            var sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, 512);
            var title = sb.ToString().Trim();
            if (string.IsNullOrEmpty(title)) return true;
            GetWindowRect(hWnd, out RECT r);
            list.Add(new WindowEntry { Title = title, X = r.Left, Y = r.Top, W = r.Right - r.Left, H = r.Bottom - r.Top });
            return true;
        }, IntPtr.Zero);
        return list;
    }

    public static string MatchLine(int x, int y, List<WindowEntry> windows)
    {
        foreach (var w in windows)
            if (x >= w.X && x < w.X + w.W && y >= w.Y && y < w.Y + w.H)
                return w.Title;
        return "";
    }
}