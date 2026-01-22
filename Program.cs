using GrassMidi.Models;
using GrassMidi.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add Services
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<AudioService>();
builder.Services.AddSingleton<ObsService>();
builder.Services.AddSingleton<MidiService>();

var app = builder.Build();

// Serve Embedded index.html
var assembly = System.Reflection.Assembly.GetExecutingAssembly();
var resourceName = "GrassMidi.wwwroot.index.html";
string indexHtml;

using (var stream = assembly.GetManifestResourceStream(resourceName))
{
    if (stream == null) throw new InvalidOperationException("Could not find embedded resource: " + resourceName);
    using (var reader = new StreamReader(stream))
    {
        indexHtml = reader.ReadToEnd();
    }
}

app.MapGet("/", () => Results.Content(indexHtml, "text/html"));
app.MapGet("/index.html", () => Results.Content(indexHtml, "text/html"));

// Start Services
var midiService = app.Services.GetRequiredService<MidiService>();
var obsService = app.Services.GetRequiredService<ObsService>(); // Triggers connect

// Simple State for "Learn Mode"
MidiMessagePayload? lastMidiMessage = null;
midiService.OnMidiMessageReceived += (ch, note, isCc, val) => {
    lastMidiMessage = new MidiMessagePayload(ch, note, isCc, val, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
};

// API Endpoints

app.MapGet("/api/config", (ConfigService cs) => cs.GetConfig());

app.MapPost("/api/config", (ConfigService cs, MidiService ms, ObsService os, [FromBody] AppConfig config) => {
    cs.SaveConfig(config);
    // Reconnect/Refresh services if needed
    ms.ConnectToDevice();
    os.Connect();
    return Results.Ok();
});

app.MapGet("/api/devices", (MidiService ms) => ms.GetInputDevices());

app.MapGet("/api/obs/status", (ObsService os) => new { Connected = os.IsConnected });

app.MapGet("/api/midi/last", () => lastMidiMessage);

// 模拟 MIDI 输入 (Inject)
// 允许外部脚本或 App 触发 "学习模式" 或直接触发绑定
app.MapPost("/api/midi/inject", (MidiService ms, [FromBody] MidiMessagePayload payload) => {
    // 触发学习事件
    ms.TriggerMidiEvent(payload.Channel, payload.Note, payload.IsControlChange, payload.Value);
    return Results.Ok();
});

// 直接执行动作 (Execute Action)
// 允许外部直接调用服务功能 (OBS, Media, etc)
app.MapPost("/api/actions/execute", (MidiService ms, ObsService os, AudioService audio, [FromBody] ActionRequest req) => {
    try {
        switch (req.Type) {
             case BindingType.SystemVolume:
                audio.SetMasterVolume(req.Value / 127f);
                break;
            case BindingType.ObsSwitchScene:
                os.SetCurrentScene(req.Target);
                break;
            case BindingType.ObsStartStream:
                os.StartStreaming();
                break;
            case BindingType.ObsStopStream:
                os.StopStreaming();
                break;
            case BindingType.RunProcess:
                GrassMidi.Services.MidiService.ActionExecutor.RunProcess(req.Target, req.Data);
                break;
            case BindingType.HttpRequest:
                GrassMidi.Services.MidiService.ActionExecutor.SendHttpRequest(req.Target, req.Data);
                break;
            // ... 可根据需要补充更多 case
            default:
                // 如果是简单触发类型，尝试通用处理? 目前主要支持显式调用的
                break;
        }
        return Results.Ok();
    } catch (Exception ex) {
        return Results.Problem(ex.Message);
    }
});

Console.WriteLine("GrassMidi 已启动。请在浏览器或 OBS 停靠窗口中打开 http://localhost:5000");

app.Run("http://*:5000");

// Records
record MidiMessagePayload(int Channel, int Note, bool IsControlChange, int Value, long Timestamp);
record ActionRequest(BindingType Type, string Target, string Data, int Value);

