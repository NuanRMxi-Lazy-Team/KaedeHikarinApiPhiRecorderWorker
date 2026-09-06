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
    public uint Width { get; set; }
    public uint Height { get; set; }
    public double EndingLength { get; set; }
    public bool RenderLoading { get; set; }
    public bool Hires { get; set; }
    public float ChartDebugLine { get; set; }
    public float ChartDebugNote { get; set; }
    public float ChartRatio { get; set; }
    public bool AllGood { get; set; }
    public bool AllBad { get; set; }
    public uint Fps { get; set; }
    public bool HardwareAccel { get; set; }
    public bool Hevc { get; set; }
    public bool Mpeg4 { get; set; }
    public string? CustomEncoder { get; set; }
    public bool DynamicBitrateControl { get; set; }
    public string? Bitrate { get; set; }
    public bool AggressiveChart { get; set; }
    public bool AggressiveNote { get; set; }
    public bool AggressiveParticle { get; set; }
    public RenderChallengeColorContract ChallengeColor { get; set; }
    public uint ChallengeRank { get; set; }
    public float NoteScale { get; set; }
    public bool Particle { get; set; }
    public string? PlayerAvatar { get; set; }
    public string? PlayerName { get; set; }
    public float PlayerRks { get; set; }
    public uint SampleCount { get; set; }
    public bool Fxaa { get; set; }
    public string? ResourcePackPath { get; set; }
    public float Speed { get; set; }
    public float VolumeMusic { get; set; }
    public float VolumeSfx { get; set; }
    public bool ForceLimit { get; set; }
    public float LimitThreshold { get; set; }
    public bool LoudnessEqualization { get; set; }
    public RenderAudioMixModeContract AudioMixMode { get; set; }
    public string? Watermark { get; set; }
    public bool Roman { get; set; }
    public bool Chinese { get; set; }
    public string? Combo { get; set; }
    public string? Difficulty { get; set; }
    public double JudgeOffset { get; set; }
    public string? FileNameFormat { get; set; }
    public bool RenderLine { get; set; }
    public bool RenderLineExtra { get; set; }
    public bool RenderNote { get; set; }
    public bool RenderDoubleHint { get; set; }
    public bool RenderUiPause { get; set; }
    public bool RenderUiName { get; set; }
    public bool RenderUiLevel { get; set; }
    public bool RenderUiScore { get; set; }
    public bool RenderUiCombo { get; set; }
    public bool RenderUiBar { get; set; }
    public bool RenderBg { get; set; }
    public bool RenderBgDim { get; set; }
    public bool PreserveFramebuffer { get; set; }
    public bool RenderExtra { get; set; }
    public float BackgroundBlurriness { get; set; }
    public ulong MaxParticles { get; set; }
    public double PlayStartTime { get; set; }
    public double PlayEndTime { get; set; }
    public bool HasPlayEndTime { get; set; }
    public float Fade { get; set; }
    public bool AlphaTint { get; set; }
}
