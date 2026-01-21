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

// Enable Static Files (for index.html)
app.UseFileServer();

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

Console.WriteLine("GrassMidi 已启动。请在浏览器或 OBS 停靠窗口中打开 http://localhost:5000");

app.Run("http://*:5000");

// Records
record MidiMessagePayload(int Channel, int Note, bool IsControlChange, int Value, long Timestamp);

