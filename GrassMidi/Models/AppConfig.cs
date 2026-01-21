using System.Text.Json.Serialization;

namespace GrassMidi.Models;

/// <summary>
/// 应用程序配置模型
/// </summary>
public class AppConfig
{
    /// <summary>
    /// OBS WebSocket 地址
    /// </summary>
    public string ObsUrl { get; set; } = "ws://localhost:4455";

    /// <summary>
    /// OBS WebSocket 密码
    /// </summary>
    public string ObsPassword { get; set; } = "";

    /// <summary>
    /// 选中的 MIDI 设备名称
    /// </summary>
    public string SelectedMidiDevice { get; set; } = "";

    /// <summary>
    /// MIDI 绑定列表
    /// </summary>
    public List<MidiBinding> Bindings { get; set; } = new();
}

/// <summary>
/// MIDI 绑定定义
/// </summary>
public class MidiBinding
{
    /// <summary>
    /// MIDI 通道
    /// </summary>
    public int Channel { get; set; }

    /// <summary>
    /// 音符或控制器编号
    /// </summary>
    public int Note { get; set; }

    /// <summary>
    /// 是否为控制变更 (旋钮/推子)
    /// </summary>
    public bool IsControlChange { get; set; }

    /// <summary>
    /// 绑定类型
    /// </summary>
    public BindingType Type { get; set; }

    /// <summary>
    /// 目标名称 (例如 OBS 源名称或场景名称)
    /// </summary>
    public string Target { get; set; } = "";
}

/// <summary>
/// 绑定操作类型枚举
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BindingType
{
    SystemVolume,    // 系统音量
    ObsVolume,       // OBS 源音量
    ObsMute,         // OBS 源静音切换
    ObsSwitchScene,  // OBS 切换场景
    MediaPlayPause,  // 媒体播放/暂停
    MediaNext,       // 媒体下一曲
    MediaPrev,       // 媒体上一曲
    MediaStop        // 媒体停止
}
