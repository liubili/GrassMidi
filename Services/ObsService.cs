using OBSWebsocketDotNet;
using GrassMidi.Models;
using Newtonsoft.Json.Linq;

namespace GrassMidi.Services;

/// <summary>
/// OBS 服务 - 用于与 OBS Studio 进行 WebSocket 通信
/// </summary>
public class ObsService
{
    private readonly OBSWebsocket _obs;
    private readonly ConfigService _configService;
    private bool _isConnected = false;

    public event EventHandler<bool>? ConnectionChanged;

    public ObsService(ConfigService configService)
    {
        _configService = configService;
        _obs = new OBSWebsocket();
        _obs.Connected += (s, e) => { _isConnected = true; ConnectionChanged?.Invoke(this, true); };
        _obs.Disconnected += (s, e) => { _isConnected = false; ConnectionChanged?.Invoke(this, false); };
        
        // 启动时自动连接
        Task.Run(async () => {
            await Task.Delay(1000);
            Connect();
        });
    }

    public bool IsConnected => _isConnected;

    /// <summary>
    /// 连接到 OBS
    /// </summary>
    public void Connect()
    {
        if (_isConnected) return;
        var config = _configService.GetConfig();
        try
        {
            _obs.ConnectAsync(config.ObsUrl, config.ObsPassword);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"连接 OBS 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 设置源音量
    /// </summary>
    /// <param name="volume">音量值 (倍数或 dB)</param>
    /// <param name="useDb">是否使用分贝 (dB) 模式</param>
    public void SetVolume(string sourceName, float volume, bool useDb = false)
    {
        if (!_isConnected) return;
        try
        {
             _obs.SetInputVolume(sourceName, volume, useDb);
        }
        catch(Exception ex)
        {
             Console.WriteLine($"OBS 设置音量错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换源静音状态
    /// </summary>
    public void ToggleMute(string sourceName)
    {
        if (!_isConnected) return;
        try
        {
            _obs.ToggleInputMute(sourceName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OBS 切换静音错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换到指定场景
    /// </summary>
    public void SetCurrentScene(string sceneName)
    {
        if (!_isConnected) return;
        try
        {
            _obs.SetCurrentProgramScene(sceneName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OBS 切换场景错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 设置源的目标窗口 (针对 窗口采集 或 音频采集)
    /// </summary>
    public void SetInputTarget(string sourceName, string windowInfo)
    {
        if (!_isConnected) return;
        try
        {
            var settings = new JObject();
            settings["window"] = windowInfo;
            // overlay = true: 保持其他设置不变
             _obs.SetInputSettings(sourceName, settings, true);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"OBS 设置目标错误: {ex.Message}");
        }
    }

    public void StartStreaming() { SafeRun(() => _obs.StartStream()); }
    public void StopStreaming() { SafeRun(() => _obs.StopStream()); }
    public void StartRecording() { SafeRun(() => _obs.StartRecord()); }
    public void StopRecording() { SafeRun(() => _obs.StopRecord()); }
    public void SaveReplayBuffer() { SafeRun(() => _obs.SaveReplayBuffer()); }

    private void SafeRun(Action action)
    {
        if (!_isConnected) return;
        try { action(); } catch (Exception ex) { Console.WriteLine($"OBS 操作错误: {ex.Message}"); }
    }
}
