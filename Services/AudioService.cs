using NAudio.CoreAudioApi;
using System.Runtime.InteropServices;

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
            // 释放旧设备资源
            /*if (_playbackDevice != null)
            {
                // MMDevice 没有显式的 Dispose 方法公开，但在 COM 中最好小心。
                // NAudio 的 MMDevice 好像没有实现 IDisposable，但 COM 对象通常需要释放。
                // 暂时忽略，依赖 GC 或 NAudio 内部处理。
            }*/
            
            _playbackDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载默认音频设备出错: {ex.Message}");
            _playbackDevice = null;
        }
    }

    /// <summary>
    /// 获取主音量 (0.0 - 1.0)
    /// </summary>
    public float GetMasterVolume()
    {
        try
        {
            return GetVolumeInternal();
        }
        catch (Exception)
        {
            // 如果出错，尝试重新加载设备并重试
            LoadDefaultDevice();
            try
            {
                return GetVolumeInternal();
            }
            catch
            {
                return 0f;
            }
        }
    }

    private float GetVolumeInternal()
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
        try
        {
            SetVolumeInternal(volume);
        }
        catch (Exception)
        {
            // 如果出错，尝试重新加载设备并重试
            LoadDefaultDevice();
            try
            {
                SetVolumeInternal(volume);
            }
            catch
            {
                // 再次失败，忽略
            }
        }
    }

    private void SetVolumeInternal(float volume)
    {
        if (_playbackDevice == null) LoadDefaultDevice();
        if (_playbackDevice != null)
        {
            // 确保音量在 0 到 1 之间
            volume = Math.Clamp(volume, 0f, 1f);
            
            // 检查设备状态是否有效（可选，但直接操作抛异常也是一种检查）
            if (_playbackDevice.State != DeviceState.Active)
            {
                throw new InvalidOperationException("Device is not active");
            }

            _playbackDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
        }
    }
}
