using OBSWebsocketDotNet;
using GrassMidi.Models;

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
    /// <param name="useMul">是否使用倍数 (Multiplier) 模式, false 为 dB 模式</param>
    public void SetVolume(string sourceName, float volume, bool useMul = true)
    {
        if (!_isConnected) return;
        try
        {
             _obs.SetInputVolume(sourceName, volume, useMul);
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
}
