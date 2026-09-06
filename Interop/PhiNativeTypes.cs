using System.Runtime.InteropServices;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;

public enum PhiStatus
{
    Ok = 0,
    InvalidArgument = 1,
    AbiMismatch = 2,
    BufferTooSmall = 3,
    InvalidUtf8 = 4,
    OutOfMemory = 5,
    NotImplemented = 6,
    Busy = 7,
    GraphicsUnavailable = 8,
    FfmpegUnavailable = 9,
    InvalidState = 10,
    InternalError = 11,
    Panic = 12,
    Canceled = 13,
    InvalidConfig = 14,
}

public enum PhiJobState
{
    Pending = 0,
    Loading = 1,
    Mixing = 2,
    Rendering = 3,
    Paused = 4,
    Done = 5,
    Canceled = 6,
    Failed = 7,
}

public enum PhiAudioMixMode
{
    Traditional = 0,
    Culling = 1,
    Fft = 2,
}

public enum PhiChallengeColor
{
    White = 0,
    Green = 1,
    Blue = 2,
    Red = 3,
    Golden = 4,
    Rainbow = 5,
}

public enum PhiChartFormat
{
    Rpe = 0,
    Pec = 1,
    Pgr = 2,
    Pbc = 3,
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct PhiStringView
{
    public readonly nint Data;
    public readonly nuint Length;

    public PhiStringView(nint data, nuint length)
    {
        Data = data;
        Length = length;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct PhiResolution
{
    public uint Width;
    public uint Height;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PhiContextOptions
{
    public uint StructSize;
    public uint AbiVersion;
    public PhiStringView AssetsDir;
    public PhiStringView FontsDir;
    public PhiStringView ResourcePackDir;
    public PhiStringView FfmpegPath;
    public PhiStringView TempDir;
    public PhiStringView RendererHostPath;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PhiRenderConfigStruct
{
    public uint StructSize;
    public uint AbiVersion;

    public PhiResolution Resolution;
    public double EndingLength;
    public byte RenderLoading;
    public byte Hires;
    public float ChartDebugLine;
    public float ChartDebugNote;
    public float ChartRatio;
    public byte AllGood;
    public byte AllBad;
    public uint Fps;
    public byte HardwareAccel;
    public byte Hevc;
    public byte Mpeg4;
    public PhiStringView CustomEncoder;
    public byte DynamicBitrateControl;
    public PhiStringView Bitrate;

    public byte AggressiveChart;
    public byte AggressiveNote;
    public byte AggressiveParticle;
    public int ChallengeColor;
    public uint ChallengeRank;
    public float NoteScale;
    public byte Particle;
    public PhiStringView PlayerAvatar;
    public PhiStringView PlayerName;
    public float PlayerRks;
    public uint SampleCount;
    public byte Fxaa;
    public PhiStringView ResourcePackPath;
    public float Speed;
    public float VolumeMusic;
    public float VolumeSfx;
    public byte ForceLimit;
    public float LimitThreshold;
    public byte LoudnessEqualization;
    public int AudioMixMode;
    public PhiStringView Watermark;
    public byte Roman;
    public byte Chinese;
    public PhiStringView Combo;
    public PhiStringView Difficulty;
    public double JudgeOffset;
    public PhiStringView FileNameFormat;

    public byte RenderLine;
    public byte RenderLineExtra;
    public byte RenderNote;
    public byte RenderDoubleHint;
    public byte RenderUiPause;
    public byte RenderUiName;
    public byte RenderUiLevel;
    public byte RenderUiScore;
    public byte RenderUiCombo;
    public byte RenderUiBar;
    public byte RenderBg;
    public byte RenderBgDim;
    public byte PreserveFramebuffer;
    public byte RenderExtra;
    public float BackgroundBlurriness;

    public ulong MaxParticles;
    public double PlayStartTime;
    public double PlayEndTime;
    public byte HasPlayEndTime;
    public float Fade;
    public byte AlphaTint;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PhiChartInfoView
{
    public uint StructSize;
    public uint AbiVersion;

    public int Id;
    public byte HasId;
    public PhiStringView Guid;
    public byte HasGuid;
    public int Uploader;
    public byte HasUploader;

    public PhiStringView Name;
    public float Difficulty;
    public PhiStringView Level;
    public PhiStringView Charter;
    public PhiStringView Composer;
    public PhiStringView Illustrator;
    public PhiStringView Chart;
    public int Format;
    public byte HasFormat;
    public PhiStringView Music;
    public PhiStringView Illustration;
    public PhiStringView UnlockVideo;
    public byte HasUnlockVideo;

    public double PreviewStart;
    public double PreviewEnd;
    public byte HasPreviewEnd;
    public float AspectRatio;
    public byte ForceAspectRatio;
    public float BackgroundDim;
    public float LineLength;
    public double Offset;
    public PhiStringView Tip;
    public byte HasTip;
    public nint Tags;
    public nuint TagCount;

    public PhiStringView Intro;
    public byte HoldPartialCover;
    public byte NegativeLengthHold;
    public byte NoteUniformScale;
    public uint ScoreTotal;
    public float HoldParticleIntervalRatio;
    public byte FoldAnimation;

    public PhiStringView Created;
    public byte HasCreated;
    public PhiStringView Updated;
    public byte HasUpdated;
    public PhiStringView ChartUpdated;
    public byte HasChartUpdated;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PhiRenderRequestStruct
{
    public uint StructSize;
    public uint AbiVersion;
    public PhiStringView ChartPath;
    public PhiStringView OutputPath;
    public nint Config;
    public nint ChartInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PhiJobSnapshotStruct
{
    public uint StructSize;
    public uint AbiVersion;
    public ulong JobId;
    public PhiJobState State;
    public double Progress;
    public double Fps;
    public double EstimatedSeconds;
    public double DurationSeconds;
    public ulong Frame;
    public ulong TotalFrames;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PhiJobEventStruct
{
    public uint StructSize;
    public uint AbiVersion;
    public ulong JobId;
    public PhiJobState State;
    public double Progress;
    public double Fps;
    public double EstimatedSeconds;
    public double DurationSeconds;
    public PhiStringView Message;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void PhiJobCallbackFn(PhiJobEventStruct* evt, void* userData);
