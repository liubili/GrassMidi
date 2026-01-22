using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace GrassMidi.Services;

/// <summary>
/// 音频服务 - 用于控制 Windows 系统音量
/// 实现了 IMMNotificationClient 以监听设备插拔和默认设备变更
/// </summary>
public class AudioService : IDisposable, IMMNotificationClient
{
    private readonly MMDeviceEnumerator _enumerator;
    private MMDevice? _playbackDevice;
    private readonly object _lock = new object();

    public AudioService()
    {
        _enumerator = new MMDeviceEnumerator();
        LoadDefaultDevice();

        try
        {
            _enumerator.RegisterEndpointNotificationCallback(this);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RegisterAudioNotificationCallback Error: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载默认音频设备
    /// </summary>
    private void LoadDefaultDevice()
    {
        lock (_lock)
        {
            try
            {
                var newDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                
                // 只有当 ID 变化时才更新，或者是初次加载
                if (_playbackDevice == null || _playbackDevice.ID != newDevice.ID)
                {
                    _playbackDevice = newDevice;
                    Console.WriteLine($"Audio Device Loaded: {_playbackDevice.FriendlyName} ({_playbackDevice.ID})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载默认音频设备出错: {ex.Message}");
                // 这里如果不置空，可能保留旧的引用，视情况而定
                // 在监听器模式下，出错可能意味着没有设备
            }
        }
    }

    /// <summary>
    /// 获取主音量 (0.0 - 1.0)
    /// </summary>
    public float GetMasterVolume()
    {
        lock (_lock)
        {
            try
            {
                if (_playbackDevice == null) LoadDefaultDevice();
                return _playbackDevice?.AudioEndpointVolume.MasterVolumeLevelScalar ?? 0f;
            }
            catch (Exception)
            {
                return 0f;
            }
        }
    }

    /// <summary>
    /// 设置主音量
    /// </summary>
    /// <param name="volume">音量值 0.0 - 1.0</param>
    public void SetMasterVolume(float volume)
    {
        lock (_lock)
        {
            try
            {
                // 再次检查空值
                if (_playbackDevice == null) LoadDefaultDevice();

                if (_playbackDevice != null)
                {
                    volume = Math.Clamp(volume, 0f, 1f);
                    _playbackDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                }
            }
            catch (Exception)
            {
                // 即使失败也不要在 Update 循环里疯狂重试 log，静默失败
            }
        }
    }

    #region IMMNotificationClient Implementation

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        // 我们只关心播放(Render)设备，且通常是 Multimedia 或 Console 角色
        if (flow == DataFlow.Render && role == Role.Multimedia)
        {
            Console.WriteLine("Default Audio Device Changed. Reloading...");
            LoadDefaultDevice();
        }
    }

    public void OnDeviceAdded(string pwstrDeviceId) { }

    public void OnDeviceRemoved(string deviceId) { }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        // 如果当前持有的设备被禁用或拔出，重新加载默认
        lock (_lock)
        {
            if (_playbackDevice != null && _playbackDevice.ID == deviceId && newState != DeviceState.Active)
            {
                 Console.WriteLine("Current Audio Device State Changed (Inactive). Reloading...");
                 LoadDefaultDevice();
            }
        }
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    #endregion

    public void Dispose()
    {
        try
        {
            _enumerator.UnregisterEndpointNotificationCallback(this);
        }
        catch { }
        
        _enumerator.Dispose();
        GC.SuppressFinalize(this);
    }
}
