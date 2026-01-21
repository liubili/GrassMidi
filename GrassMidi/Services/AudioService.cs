using NAudio.CoreAudioApi;

namespace GrassMidi.Services;

/// <summary>
/// 音频服务 - 用于控制 Windows 系统音量
/// </summary>
public class AudioService
{
    private readonly MMDeviceEnumerator _enumerator;
    private MMDevice? _playbackDevice;

    public AudioService()
    {
        _enumerator = new MMDeviceEnumerator();
        LoadDefaultDevice();
    }

    /// <summary>
    /// 加载默认音频设备
    /// </summary>
    private void LoadDefaultDevice()
    {
        try
        {
            _playbackDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载默认音频设备出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取主音量 (0.0 - 1.0)
    /// </summary>
    public float GetMasterVolume()
    {
        if (_playbackDevice == null) LoadDefaultDevice();
        return _playbackDevice?.AudioEndpointVolume.MasterVolumeLevelScalar ?? 0f;
    }

    /// <summary>
    /// 设置主音量
    /// </summary>
    /// <param name="volume">音量值 0.0 - 1.0</param>
    public void SetMasterVolume(float volume)
    {
        if (_playbackDevice == null) LoadDefaultDevice();
        if (_playbackDevice != null)
        {
            // 确保音量在 0 到 1 之间
            volume = Math.Clamp(volume, 0f, 1f);
            _playbackDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
        }
    }
}
