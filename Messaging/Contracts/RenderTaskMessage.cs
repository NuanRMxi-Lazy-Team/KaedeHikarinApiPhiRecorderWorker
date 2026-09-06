using System.Text.Json;
using System.Text.Json.Serialization;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging.Contracts;

public static class ContractJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}

public sealed class RenderTaskMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();

    public Guid JobId { get; set; }

    public string ChartPresignedUrl { get; set; } = string.Empty;

    public string OutputObjectKey { get; set; } = string.Empty;

    public RenderConfigContract Config { get; set; } = new();

    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public enum RenderChallengeColorContract
{
    White = 0,
    Green = 1,
    Blue = 2,
    Red = 3,
    Golden = 4,
    Rainbow = 5,
}

public enum RenderAudioMixModeContract
{
    Traditional = 0,
    Culling = 1,
    Fft = 2,
}

public sealed class RenderConfigContract
{
    /// <summary>渲染分辨率，默认与上游 Phi-Recorder 基础预设一致：1920×1080。</summary>
    public uint Width { get; set; } = 1920;
    public uint Height { get; set; } = 1080;
    public double EndingLength { get; set; }
    public bool RenderLoading { get; set; }
    public bool Hires { get; set; }
    public float ChartDebugLine { get; set; }
    public float ChartDebugNote { get; set; }
    public float ChartRatio { get; set; } = 1.0f;
    public bool AllGood { get; set; }
    public bool AllBad { get; set; }
    public uint Fps { get; set; } = 60;
    public bool HardwareAccel { get; set; } = true;
    public bool Hevc { get; set; }
    public bool Mpeg4 { get; set; }
    public string? CustomEncoder { get; set; }
    public bool DynamicBitrateControl { get; set; } = true;
    public string? Bitrate { get; set; } = "28";
    public bool AggressiveChart { get; set; } = true;
    public bool AggressiveNote { get; set; }
    public bool AggressiveParticle { get; set; }
    public RenderChallengeColorContract ChallengeColor { get; set; } = RenderChallengeColorContract.Rainbow;
    public uint ChallengeRank { get; set; } = 3;
    public float NoteScale { get; set; } = 1.0f;
    public bool Particle { get; set; } = true;
    public string? PlayerAvatar { get; set; }
    public string? PlayerName { get; set; } = "";
    public float PlayerRks { get; set; } = 16.0f;
    public uint SampleCount { get; set; } = 8;
    public bool Fxaa { get; set; }
    public string? ResourcePackPath { get; set; }
    public float Speed { get; set; } = 1.0f;
    public float VolumeMusic { get; set; } = 0.5f;
    public float VolumeSfx { get; set; } = 0.4f;
    public bool ForceLimit { get; set; } = true;
    public float LimitThreshold { get; set; } = 0.5f;
    public bool LoudnessEqualization { get; set; }
    public RenderAudioMixModeContract AudioMixMode { get; set; } = RenderAudioMixModeContract.Culling;
    public string? Watermark { get; set; } = "";
    public bool Roman { get; set; }
    public bool Chinese { get; set; }
    public string? Combo { get; set; } = "AUTOPLAY";
    public string? Difficulty { get; set; } = "";
    public double JudgeOffset { get; set; }
    public string? FileNameFormat { get; set; } = "%date% %time% %info.name%_%level_prefix%";
    public bool RenderLine { get; set; } = true;
    public bool RenderLineExtra { get; set; } = true;
    public bool RenderNote { get; set; } = true;
    public bool RenderDoubleHint { get; set; } = true;
    public bool RenderUiPause { get; set; } = true;
    public bool RenderUiName { get; set; } = true;
    public bool RenderUiLevel { get; set; } = true;
    public bool RenderUiScore { get; set; } = true;
    public bool RenderUiCombo { get; set; } = true;
    public bool RenderUiBar { get; set; } = true;
    public bool RenderBg { get; set; } = true;
    public bool RenderBgDim { get; set; } = true;
    public bool PreserveFramebuffer { get; set; }
    public bool RenderExtra { get; set; } = true;
    public float BackgroundBlurriness { get; set; } = 80f;
    public ulong MaxParticles { get; set; } = 5000;
    public double PlayStartTime { get; set; }
    public double PlayEndTime { get; set; }
    public bool HasPlayEndTime { get; set; }
    public float Fade { get; set; }
    public bool AlphaTint { get; set; }
}
