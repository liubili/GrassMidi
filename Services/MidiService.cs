using System.Runtime.InteropServices;
using NAudio.Midi;
using GrassMidi.Models;

namespace GrassMidi.Services;

/// <summary>
/// MIDI 服务 - 处理核心 MIDI 输入逻辑
/// </summary>
public class MidiService : IDisposable
{
    private MidiIn? _midiIn;
    private readonly ConfigService _configService;
    private readonly AudioService _audioService;
    private readonly ObsService _obsService;

    // UI "学习模式" 事件
    public event Action<int, int, bool, int>? OnMidiMessageReceived; // Channel, Note, IsControlChange, Value

    public MidiService(ConfigService configService, AudioService audioService, ObsService obsService)
    {
        _configService = configService;
        _audioService = audioService;
        _obsService = obsService;

        ConnectToDevice();
    }

    /// <summary>
    /// 获取所有 MIDI 输入设备
    /// </summary>
    public List<string> GetInputDevices()
    {
        var devices = new List<string>();
        for (int i = 0; i < MidiIn.NumberOfDevices; i++)
        {
            devices.Add(MidiIn.DeviceInfo(i).ProductName);
        }
        return devices;
    }

    /// <summary>
    /// 连接到配置的设备
    /// </summary>
    public void ConnectToDevice()
    {
        Close();

        var config = _configService.GetConfig();
        if (string.IsNullOrEmpty(config.SelectedMidiDevice)) return;

        int deviceIndex = -1;
        for (int i = 0; i < MidiIn.NumberOfDevices; i++)
        {
            if (MidiIn.DeviceInfo(i).ProductName == config.SelectedMidiDevice)
            {
                deviceIndex = i;
                break;
            }
        }

        if (deviceIndex == -1)
        {
             Console.WriteLine($"未找到 MIDI 设备 '{config.SelectedMidiDevice}'");
             return;
        }

        try
        {
            _midiIn = new MidiIn(deviceIndex);
            _midiIn.MessageReceived += MidiIn_MessageReceived;
            _midiIn.Start();
            Console.WriteLine($"已连接到 MIDI 设备: {config.SelectedMidiDevice}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"连接 MIDI 设备出错: {ex.Message}");
        }
    }

    private void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
    {
        int channel = e.MidiEvent.Channel;
        int note = -1;
        int value = 0;
        bool isControlChange = false;

        if (e.MidiEvent.CommandCode == MidiCommandCode.ControlChange)
        {
            var cc = (ControlChangeEvent)e.MidiEvent;
            note = (int)cc.Controller;
            value = cc.ControllerValue;
            isControlChange = true;
        }
        else if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOn)
        {
            var noteEvent = (NoteEvent)e.MidiEvent;
            note = noteEvent.NoteNumber;
            value = noteEvent.Velocity;
            isControlChange = false;
        }
        else
        {
            return; // 忽略其他消息
        }

        // 通知 UI 用于学习模式
        OnMidiMessageReceived?.Invoke(channel, note, isControlChange, value);

        // 处理绑定
        ProcessBinding(channel, note, isControlChange, value);
    }

    public void TriggerMidiEvent(int channel, int note, bool isControlChange, int value)
    {
        OnMidiMessageReceived?.Invoke(channel, note, isControlChange, value);
        ProcessBinding(channel, note, isControlChange, value);
    }

    private void ProcessBinding(int channel, int note, bool isControlChange, int value)
    {
        var config = _configService.GetConfig();
        // 查找所有匹配的绑定 (支持一对多)
        var bindings = config.Bindings.Where(b => 
            b.Channel == channel && 
            b.Note == note && 
            b.IsControlChange == isControlChange);

        float normalizedVolume = value / 127f;

        foreach (var binding in bindings)
        {
            switch (binding.Type)
            {
                case BindingType.SystemVolume:
                    _audioService.SetMasterVolume(normalizedVolume);
                    break;
                case BindingType.ObsVolume:
                    // 使用立方使得音量调节更平滑 (Cubic Taper)
                    // 直接传递 Multiplier (0-1)，避免 dB 转换的 -inf 问题
                    float inputMul = normalizedVolume * normalizedVolume * normalizedVolume;
                    _obsService.SetVolume(binding.Target, inputMul, useDb: false);
                    break;
                case BindingType.ObsMute:
                    if (value > 0) _obsService.ToggleMute(binding.Target);
                    break;
                case BindingType.ObsSwitchScene:
                    if (value > 0) _obsService.SetCurrentScene(binding.Target);
                    break;
                
                // Media Controls
                case BindingType.MediaPlayPause:
                    if (value > 0) InputHelper.TogglePlayPause();
                    break;
                case BindingType.MediaNext:
                    if (value > 0) InputHelper.NextTrack();
                    break;
                case BindingType.MediaPrev:
                    if (value > 0) InputHelper.PrevTrack();
                    break;
                 case BindingType.MediaStop:
                    if (value > 0) InputHelper.StopMedia();
                    break;
                
                // OBS Stream/Record
                case BindingType.ObsStartStream:
                    if (value > 0) _obsService.StartStreaming();
                    break;
                case BindingType.ObsStopStream:
                    if (value > 0) _obsService.StopStreaming();
                    break;
                case BindingType.ObsStartRecord:
                    if (value > 0) _obsService.StartRecording();
                    break;
                case BindingType.ObsStopRecord:
                    if (value > 0) _obsService.StopRecording();
                    break;
                case BindingType.ObsSaveReplay:
                    if (value > 0) _obsService.SaveReplayBuffer();
                    break;
                
                case BindingType.ObsSetForegroundWindow:
                    if (value > 0)
                    {
                        var info = WindowHelper.GetForegroundWindowInfo();
                        if (!string.IsNullOrEmpty(info))
                        {
                            Console.WriteLine($"Switching Source '{binding.Target}' to: {info}");
                            _obsService.SetInputTarget(binding.Target, info);
                        }
                    }
                    break;

                // Expansion
                case BindingType.RunProcess:
                    if (value > 0) ActionExecutor.RunProcess(binding.Target, binding.Data);
                    break;
                case BindingType.KeyboardKey:
                    if (value > 0 && byte.TryParse(binding.Data, out byte vk)) InputHelper.PressKey(vk);
                    break;
                case BindingType.HttpRequest:
                    if (value > 0) ActionExecutor.SendHttpRequest(binding.Target, binding.Data);
                    break;
            }
        }
    }

    /// <summary>
    /// 独立执行器，用于处理非 MIDI 相关的通用操作
    /// </summary>
    public static class ActionExecutor
    {
        public static void RunProcess(string path, string args)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法启动进程: {ex.Message}");
            }
        }

        public static async void SendHttpRequest(string url, string methodData)
        {
            try
            {
                using var client = new HttpClient();
                HttpResponseMessage? response = null;
                
                // data format: "METHOD|BODY" or just "METHOD" or JSON config
                // Simple parsing for now:
                string method = methodData;
                string body = "";
                
                if (methodData.Contains('|')) {
                    var parts = methodData.Split('|', 2);
                    method = parts[0];
                    body = parts[1];
                }

                if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    response = await client.PostAsync(url, content);
                }
                else
                {
                    response = await client.GetAsync(url);
                }
                
                Console.WriteLine($"HTTP 请求 ({url}) [{method}] 状态码: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HTTP 请求失败: {ex.Message}");
            }
        }
    }

    private void Close()
    {
        if (_midiIn != null)
        {
            try
            {
                _midiIn.Stop();
                _midiIn.Dispose();
            }
            catch {}
            _midiIn = null;
        }
    }

    public void Dispose()
    {
        Close();
    }
}

/// <summary>
/// 键盘输入辅助类 (P/Invoke)
/// </summary>
public static class InputHelper
{
    private const int KEYEVENTF_EXTENDEDKEY = 1;
    private const int KEYEVENTF_KEYUP = 2;
    private const int VK_MEDIA_NEXT_TRACK = 0xB0;
    private const int VK_MEDIA_PREV_TRACK = 0xB1;
    private const int VK_MEDIA_STOP = 0xB2;
    private const int VK_MEDIA_PLAY_PAUSE = 0xB3;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    public static void TogglePlayPause()
    {
        keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_KEYUP, 0);
    }

    public static void NextTrack()
    {
        keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_KEYUP, 0);
    }

    public static void PrevTrack()
    {
        keybd_event(VK_MEDIA_PREV_TRACK, 0, KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(VK_MEDIA_PREV_TRACK, 0, KEYEVENTF_KEYUP, 0);
    }

    public static void StopMedia()
    {
        keybd_event(VK_MEDIA_STOP, 0, KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(VK_MEDIA_STOP, 0, KEYEVENTF_KEYUP, 0);
    }

    public static void PressKey(byte vkCode)
    {
        keybd_event(vkCode, 0, 0, 0);
        keybd_event(vkCode, 0, KEYEVENTF_KEYUP, 0);
    }
}
