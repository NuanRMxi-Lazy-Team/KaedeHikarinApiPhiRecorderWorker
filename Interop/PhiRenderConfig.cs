namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;

internal unsafe delegate T PhiRenderConfigAction<out T>(PhiRenderConfigStruct* config);

/// <summary>
/// Managed mirror of the native <c>phi_render_config_t</c>. The native library
/// deep-copies every string only for the duration of a call, so managed strings
/// are stored here and pinned while a native struct is in use.
/// </summary>
public sealed class PhiRenderConfig
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
    public PhiChallengeColor ChallengeColor { get; set; }
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
    public PhiAudioMixMode AudioMixMode { get; set; }
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

    /// <summary>Builds a config from the code-defined native defaults.</summary>
    public static unsafe PhiRenderConfig CreateDefault()
    {
        PhiRenderConfigStruct native = default;
        PhiNative.phi_render_config_init_default(&native).ThrowIfError("phi_render_config_init_default");
        return FromNative(&native);
    }

    /// <summary>Validates the config through the native ABI.</summary>
    public unsafe void Validate()
    {
        WithNative(cfgPtr => PhiNative.phi_render_config_validate(cfgPtr)).ThrowIfError("phi_render_config_validate");
    }

    internal unsafe T WithNative<T>(PhiRenderConfigAction<T> action)
    {
        byte[] customEncoder = Utf8.Encode(CustomEncoder);
        byte[] bitrate = Utf8.Encode(Bitrate);
        byte[] playerAvatar = Utf8.Encode(PlayerAvatar);
        byte[] playerName = Utf8.Encode(PlayerName);
        byte[] resourcePackPath = Utf8.Encode(ResourcePackPath);
        byte[] watermark = Utf8.Encode(Watermark);
        byte[] combo = Utf8.Encode(Combo);
        byte[] difficulty = Utf8.Encode(Difficulty);
        byte[] fileNameFormat = Utf8.Encode(FileNameFormat);

        fixed (byte* customEncoderPtr = customEncoder,
               bitratePtr = bitrate,
               playerAvatarPtr = playerAvatar,
               playerNamePtr = playerName,
               resourcePackPathPtr = resourcePackPath,
               watermarkPtr = watermark,
               comboPtr = combo,
               difficultyPtr = difficulty,
               fileNameFormatPtr = fileNameFormat)
        {
            PhiRenderConfigStruct native = new()
            {
                StructSize = (uint)sizeof(PhiRenderConfigStruct),
                AbiVersion = PhiNative.AbiVersion,
                Resolution = new PhiResolution { Width = Width, Height = Height },
                EndingLength = EndingLength,
                RenderLoading = RenderLoading ? (byte)1 : (byte)0,
                Hires = Hires ? (byte)1 : (byte)0,
                ChartDebugLine = ChartDebugLine,
                ChartDebugNote = ChartDebugNote,
                ChartRatio = ChartRatio,
                AllGood = AllGood ? (byte)1 : (byte)0,
                AllBad = AllBad ? (byte)1 : (byte)0,
                Fps = Fps,
                HardwareAccel = HardwareAccel ? (byte)1 : (byte)0,
                Hevc = Hevc ? (byte)1 : (byte)0,
                Mpeg4 = Mpeg4 ? (byte)1 : (byte)0,
                CustomEncoder = new PhiStringView((nint)customEncoderPtr, (nuint)customEncoder.Length),
                DynamicBitrateControl = DynamicBitrateControl ? (byte)1 : (byte)0,
                Bitrate = new PhiStringView((nint)bitratePtr, (nuint)bitrate.Length),
                AggressiveChart = AggressiveChart ? (byte)1 : (byte)0,
                AggressiveNote = AggressiveNote ? (byte)1 : (byte)0,
                AggressiveParticle = AggressiveParticle ? (byte)1 : (byte)0,
                ChallengeColor = (int)ChallengeColor,
                ChallengeRank = ChallengeRank,
                NoteScale = NoteScale,
                Particle = Particle ? (byte)1 : (byte)0,
                PlayerAvatar = new PhiStringView((nint)playerAvatarPtr, (nuint)playerAvatar.Length),
                PlayerName = new PhiStringView((nint)playerNamePtr, (nuint)playerName.Length),
                PlayerRks = PlayerRks,
                SampleCount = SampleCount,
                Fxaa = Fxaa ? (byte)1 : (byte)0,
                ResourcePackPath = new PhiStringView((nint)resourcePackPathPtr, (nuint)resourcePackPath.Length),
                Speed = Speed,
                VolumeMusic = VolumeMusic,
                VolumeSfx = VolumeSfx,
                ForceLimit = ForceLimit ? (byte)1 : (byte)0,
                LimitThreshold = LimitThreshold,
                LoudnessEqualization = LoudnessEqualization ? (byte)1 : (byte)0,
                AudioMixMode = (int)AudioMixMode,
                Watermark = new PhiStringView((nint)watermarkPtr, (nuint)watermark.Length),
                Roman = Roman ? (byte)1 : (byte)0,
                Chinese = Chinese ? (byte)1 : (byte)0,
                Combo = new PhiStringView((nint)comboPtr, (nuint)combo.Length),
                Difficulty = new PhiStringView((nint)difficultyPtr, (nuint)difficulty.Length),
                JudgeOffset = JudgeOffset,
                FileNameFormat = new PhiStringView((nint)fileNameFormatPtr, (nuint)fileNameFormat.Length),
                RenderLine = RenderLine ? (byte)1 : (byte)0,
                RenderLineExtra = RenderLineExtra ? (byte)1 : (byte)0,
                RenderNote = RenderNote ? (byte)1 : (byte)0,
                RenderDoubleHint = RenderDoubleHint ? (byte)1 : (byte)0,
                RenderUiPause = RenderUiPause ? (byte)1 : (byte)0,
                RenderUiName = RenderUiName ? (byte)1 : (byte)0,
                RenderUiLevel = RenderUiLevel ? (byte)1 : (byte)0,
                RenderUiScore = RenderUiScore ? (byte)1 : (byte)0,
                RenderUiCombo = RenderUiCombo ? (byte)1 : (byte)0,
                RenderUiBar = RenderUiBar ? (byte)1 : (byte)0,
                RenderBg = RenderBg ? (byte)1 : (byte)0,
                RenderBgDim = RenderBgDim ? (byte)1 : (byte)0,
                PreserveFramebuffer = PreserveFramebuffer ? (byte)1 : (byte)0,
                RenderExtra = RenderExtra ? (byte)1 : (byte)0,
                BackgroundBlurriness = BackgroundBlurriness,
                MaxParticles = MaxParticles,
                PlayStartTime = PlayStartTime,
                PlayEndTime = PlayEndTime,
                HasPlayEndTime = HasPlayEndTime ? (byte)1 : (byte)0,
                Fade = Fade,
                AlphaTint = AlphaTint ? (byte)1 : (byte)0,
            };
            return action(&native);
        }
    }

    private static unsafe PhiRenderConfig FromNative(PhiRenderConfigStruct* native)
    {
        return new PhiRenderConfig
        {
            Width = native->Resolution.Width,
            Height = native->Resolution.Height,
            EndingLength = native->EndingLength,
            RenderLoading = native->RenderLoading != 0,
            Hires = native->Hires != 0,
            ChartDebugLine = native->ChartDebugLine,
            ChartDebugNote = native->ChartDebugNote,
            ChartRatio = native->ChartRatio,
            AllGood = native->AllGood != 0,
            AllBad = native->AllBad != 0,
            Fps = native->Fps,
            HardwareAccel = native->HardwareAccel != 0,
            Hevc = native->Hevc != 0,
            Mpeg4 = native->Mpeg4 != 0,
            CustomEncoder = Utf8.Decode(native->CustomEncoder.Data, native->CustomEncoder.Length),
            DynamicBitrateControl = native->DynamicBitrateControl != 0,
            Bitrate = Utf8.Decode(native->Bitrate.Data, native->Bitrate.Length),
            AggressiveChart = native->AggressiveChart != 0,
            AggressiveNote = native->AggressiveNote != 0,
            AggressiveParticle = native->AggressiveParticle != 0,
            ChallengeColor = (PhiChallengeColor)native->ChallengeColor,
            ChallengeRank = native->ChallengeRank,
            NoteScale = native->NoteScale,
            Particle = native->Particle != 0,
            PlayerAvatar = Utf8.Decode(native->PlayerAvatar.Data, native->PlayerAvatar.Length),
            PlayerName = Utf8.Decode(native->PlayerName.Data, native->PlayerName.Length),
            PlayerRks = native->PlayerRks,
            SampleCount = native->SampleCount,
            Fxaa = native->Fxaa != 0,
            ResourcePackPath = Utf8.Decode(native->ResourcePackPath.Data, native->ResourcePackPath.Length),
            Speed = native->Speed,
            VolumeMusic = native->VolumeMusic,
            VolumeSfx = native->VolumeSfx,
            ForceLimit = native->ForceLimit != 0,
            LimitThreshold = native->LimitThreshold,
            LoudnessEqualization = native->LoudnessEqualization != 0,
            AudioMixMode = (PhiAudioMixMode)native->AudioMixMode,
            Watermark = Utf8.Decode(native->Watermark.Data, native->Watermark.Length),
            Roman = native->Roman != 0,
            Chinese = native->Chinese != 0,
            Combo = Utf8.Decode(native->Combo.Data, native->Combo.Length),
            Difficulty = Utf8.Decode(native->Difficulty.Data, native->Difficulty.Length),
            JudgeOffset = native->JudgeOffset,
            FileNameFormat = Utf8.Decode(native->FileNameFormat.Data, native->FileNameFormat.Length),
            RenderLine = native->RenderLine != 0,
            RenderLineExtra = native->RenderLineExtra != 0,
            RenderNote = native->RenderNote != 0,
            RenderDoubleHint = native->RenderDoubleHint != 0,
            RenderUiPause = native->RenderUiPause != 0,
            RenderUiName = native->RenderUiName != 0,
            RenderUiLevel = native->RenderUiLevel != 0,
            RenderUiScore = native->RenderUiScore != 0,
            RenderUiCombo = native->RenderUiCombo != 0,
            RenderUiBar = native->RenderUiBar != 0,
            RenderBg = native->RenderBg != 0,
            RenderBgDim = native->RenderBgDim != 0,
            PreserveFramebuffer = native->PreserveFramebuffer != 0,
            RenderExtra = native->RenderExtra != 0,
            BackgroundBlurriness = native->BackgroundBlurriness,
            MaxParticles = native->MaxParticles,
            PlayStartTime = native->PlayStartTime,
            PlayEndTime = native->PlayEndTime,
            HasPlayEndTime = native->HasPlayEndTime != 0,
            Fade = native->Fade,
            AlphaTint = native->AlphaTint != 0,
        };
    }
}
