using Windows.Media.Ocr;
using Windows.Graphics.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

Console.OutputEncoding = System.Text.Encoding.UTF8;

try
{
    // 截屏
    var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
    using var bitmap = new Bitmap(bounds.Width, bounds.Height);
    using (var g = System.Drawing.Graphics.FromImage(bitmap))
        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);

    // Bitmap → SoftwareBitmap
    using var ms = new MemoryStream();
    bitmap.Save(ms, ImageFormat.Png);
    ms.Position = 0;
    var ras = ms.AsRandomAccessStream();
    var decoder = await BitmapDecoder.CreateAsync(ras);
    var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

    // OCR
    var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
    if (ocrEngine == null) { Console.WriteLine("[FAIL] OcrEngine 创建失败"); return; }

    var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
    if (ocrResult.Lines.Count == 0) { Console.WriteLine("[OK] 截图完成，未检测到文字"); return; }

    Console.WriteLine($"[OK] 截图 {bounds.Width}x{bounds.Height} | OCR {ocrResult.Lines.Count} 行 | 共 {ocrResult.Text.Length} 字符");
    Console.WriteLine("--- 坐标  文字 ---");

    foreach (var line in ocrResult.Lines)
    {
        var fw = line.Words[0];
        var lw = line.Words[^1];
        var r = fw.BoundingRect;
        int rx = (int)(lw.BoundingRect.X + lw.BoundingRect.Width);
        int by = (int)(r.Y + r.Height);
        Console.WriteLine($"  ({r.X},{r.Y})-({rx},{by})  \"{line.Text}\"");
    }

    Console.WriteLine("--- 全文 ---");
    Console.WriteLine(ocrResult.Text.Trim());
}
catch (Exception ex)
{
    Console.WriteLine($"[FAIL] {ex.GetType().Name}: {ex.Message}");
}
