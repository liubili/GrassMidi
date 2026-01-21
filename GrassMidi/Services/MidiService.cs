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

    private void ProcessBinding(int channel, int note, bool isControlChange, int value)
    {
        var config = _configService.GetConfig();
        var binding = config.Bindings.FirstOrDefault(b => 
            b.Channel == channel && 
            b.Note == note && 
            b.IsControlChange == isControlChange);

        if (binding == null) return;

        float normalizedVolume = value / 127f;

        switch (binding.Type)
        {
            case BindingType.SystemVolume:
                _audioService.SetMasterVolume(normalizedVolume);
                break;
            case BindingType.ObsVolume:
                // 转换 0-127 到 dB
                // 1. 应用立方锥度 (Cubic Taper) 模拟推子曲线: mul = val^3
                // 2. 转换为 dB: dB = 20 * Log10(mul)
                
                float inputMul = normalizedVolume * normalizedVolume * normalizedVolume;
                float obsDb;

                if (inputMul <= 0.0001f)
                {
                    obsDb = -100.0f; // 视为静音 (-inf)
                }
                else
                {
                    obsDb = (float)(20 * Math.Log10(inputMul));
                }
                
                // 限制最大为 0dB (避免过大增益)
                if (obsDb > 0) obsDb = 0;

                _obsService.SetVolume(binding.Target, obsDb, useMul: false);
                break;
            case BindingType.ObsMute:
                if (value > 0) // 按下按钮时触发
                {
                    _obsService.ToggleMute(binding.Target);
                }
                break;
            case BindingType.ObsSwitchScene:
                if (value > 0)
                {
                    _obsService.SetCurrentScene(binding.Target);
                }
                break;
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
}
