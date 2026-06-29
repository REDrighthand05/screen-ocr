// ScreenOcr MCP Server — uses OcrCore namespace
using OcrCore;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Length > 0 && args[0] == "--direct") {
    var result = await OcrProcessor.Run("zh-CN", false, false, null, null, null, null);
    Console.WriteLine(result);
    return;
}

while (true) {
    var header = Console.ReadLine();
    if (string.IsNullOrEmpty(header)) break;
    if (!header.StartsWith("Content-Length:")) continue;
    int contentLength = int.Parse(header.AsSpan("Content-Length: ".Length));
    Console.ReadLine();
    var buffer = new char[contentLength];
    int totalRead = 0;
    while (totalRead < contentLength)
        totalRead += Console.In.ReadBlock(buffer, totalRead, contentLength - totalRead);
    var json = new string(buffer, 0, totalRead);
    JsonRpcRequest? req;
    try { req = JsonSerializer.Deserialize<JsonRpcRequest>(json); } catch { break; }
    if (req == null) break;

    JsonRpcResponse resp;
    try {
        resp = req.Method switch {
            "initialize" => HandleInit(req),
            "tools/list" => HandleTools(req),
            "tools/call" => await HandleCall(req),
            "notifications/initialized" => new JsonRpcResponse { Id = null },
            _ => new JsonRpcResponse { Id = req.Id, Error = new JsonRpcError { Code = -32601, Message = "Unknown: " + req.Method } }
        };
    } catch (Exception ex) {
        resp = new JsonRpcResponse { Id = req.Id, Error = new JsonRpcError { Code = -32603, Message = ex.Message } };
    }
    var respJson = JsonSerializer.Serialize(resp, JsonContext.Default.JsonRpcResponse);
    var respBytes = System.Text.Encoding.UTF8.GetBytes(respJson);
    Console.WriteLine("Content-Length: " + respBytes.Length);
    Console.WriteLine();
    Console.Write(respJson);
    Console.Out.Flush();
}

static JsonRpcResponse HandleInit(JsonRpcRequest req) =>
    new JsonRpcResponse { Id = req.Id, Result = JsonSerializer.SerializeToElement(new {
        protocolVersion = "2024-11-05", capabilities = new { tools = new { } },
        serverInfo = new { name = "screen-ocr", version = "0.2.0" }
    }) };

static JsonRpcResponse HandleTools(JsonRpcRequest req) =>
    new JsonRpcResponse { Id = req.Id, Result = JsonSerializer.SerializeToElement(new {
        tools = new[] {
            new { name = "screen_ocr", description = "全屏OCR识别",
                  inputSchema = new { type = "object", properties = new {
                      lang = new { type = "string", description = "zh-CN/en-US" },
                      compact = new { type = "boolean" },
                      json = new { type = "boolean" },
                      region = new { type = "string", description = "x,y,w,h" }
                  }, required = new string[] { } } }
        }
    }) };

static async Task<JsonRpcResponse> HandleCall(JsonRpcRequest req) {
    string toolName = req.Params.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
    string lang = "zh-CN"; bool compact = false; bool jsonOutput = false;
    int? rx = null, ry = null, rw = null, rh = null;

    if (req.Params.TryGetProperty("arguments", out var ap)) {
        if (ap.TryGetProperty("lang", out var l)) lang = l.GetString() ?? "zh-CN";
        if (ap.TryGetProperty("compact", out var c)) compact = c.GetBoolean();
        if (ap.TryGetProperty("json", out var j)) jsonOutput = j.GetBoolean();
        if (ap.TryGetProperty("region", out var reg)) {
            var parts = reg.GetString()?.Split(',');
            if (parts?.Length == 4) { rx = int.Parse(parts[0]); ry = int.Parse(parts[1]); rw = int.Parse(parts[2]); rh = int.Parse(parts[3]); }
        }
    }

    var text = await OcrProcessor.Run(lang, compact, jsonOutput, rx, ry, rw, rh);
    var fullText = (await OcrProcessor.Run(lang, false, false, null, null, null, null)).Split('\n', StringSplitOptions.RemoveEmptyEntries).Length > 2
        ? "screen scanned" : "(no text)";

    object result = new { content = new[] { new { type = "text", text } } };
    return new JsonRpcResponse { Id = req.Id, Result = JsonSerializer.SerializeToElement(result) };
}

class JsonRpcRequest { [JsonPropertyName("id")] public JsonElement? Id { get; set; } [JsonPropertyName("method")] public string Method { get; set; } = ""; [JsonPropertyName("params")] public JsonElement Params { get; set; } }
class JsonRpcResponse { [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0"; [JsonPropertyName("id")] public JsonElement? Id { get; set; } [JsonPropertyName("result")] public JsonElement? Result { get; set; } [JsonPropertyName("error")] public JsonRpcError? Error { get; set; } }
class JsonRpcError { [JsonPropertyName("code")] public int Code { get; set; } [JsonPropertyName("message")] public string Message { get; set; } = ""; }
[JsonSerializable(typeof(JsonRpcResponse))] [JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
partial class JsonContext : JsonSerializerContext { }