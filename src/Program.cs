using OcrCore;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Parse arguments
string lang = "zh-CN";
bool compact = false;
bool json = false;
int continuousMs = 0;
int? rx = null, ry = null, rw = null, rh = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--lang" when i + 1 < args.Length: lang = args[++i]; break;
        case "--compact": compact = true; break;
        case "--json": json = true; break;
        case "--region" when i + 4 < args.Length:
            rx = int.Parse(args[++i]); ry = int.Parse(args[++i]);
            rw = int.Parse(args[++i]); rh = int.Parse(args[++i]); break;
        case "--every" when i + 1 < args.Length:
            continuousMs = int.Parse(args[++i]); break;
    }
}

// Run once or continuous
if (continuousMs > 0)
{
    while (true)
    {
        var result = await OcrProcessor.Run(lang, compact, json, rx, ry, rw, rh);
        Console.WriteLine($"--- {DateTime.Now:HH:mm:ss} ---");
        Console.WriteLine(result);
        Console.WriteLine();
        await Task.Delay(continuousMs);
    }
}
else
{
    var result = await OcrProcessor.Run(lang, compact, json, rx, ry, rw, rh);
    Console.WriteLine(result);
}
