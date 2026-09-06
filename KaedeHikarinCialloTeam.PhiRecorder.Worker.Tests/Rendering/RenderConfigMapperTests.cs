using KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging.Contracts;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Rendering;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Tests.Rendering;

public class RenderConfigMapperTests
{
    [Fact]
    public void ToPhiRenderConfig_MapsAllFields()
    {
        var contract = new RenderConfigContract
        {
            Width = 1920,
            Height = 1080,
            EndingLength = 3.5,
            RenderLoading = true,
            Hires = true,
            ChartDebugLine = 1.5f,
            ChartDebugNote = 2.5f,
            ChartRatio = 1.0f,
            AllGood = true,
            AllBad = false,
            Fps = 60,
            HardwareAccel = true,
            Hevc = false,
            Mpeg4 = true,
            CustomEncoder = "libx264",
            DynamicBitrateControl = true,
            Bitrate = "8M",
            AggressiveChart = true,
            AggressiveNote = false,
            AggressiveParticle = true,
            ChallengeColor = RenderChallengeColorContract.Golden,
            ChallengeRank = 16,
            NoteScale = 1.1f,
            Particle = true,
            PlayerAvatar = "avatar.png",
            PlayerName = "nuanr",
            PlayerRks = 15.5f,
            SampleCount = 8,
            Fxaa = true,
            ResourcePackPath = null,
            Speed = 1.2f,
            VolumeMusic = 0.8f,
            VolumeSfx = 0.6f,
            ForceLimit = true,
            LimitThreshold = 0.5f,
            LoudnessEqualization = true,
            AudioMixMode = RenderAudioMixModeContract.Fft,
            Watermark = "wm",
            Roman = true,
            Chinese = false,
            Combo = "AUTOPLAY",
            Difficulty = "AT",
            JudgeOffset = 0.02,
            FileNameFormat = null,
            RenderLine = true,
            RenderLineExtra = true,
            RenderNote = true,
            RenderDoubleHint = true,
            RenderUiPause = false,
            RenderUiName = true,
            RenderUiLevel = true,
            RenderUiScore = true,
            RenderUiCombo = true,
            RenderUiBar = true,
            RenderBg = true,
            RenderBgDim = true,
            PreserveFramebuffer = true,
            RenderExtra = true,
            BackgroundBlurriness = 80f,
            MaxParticles = 5000,
            PlayStartTime = 1.5,
            PlayEndTime = 60.0,
            HasPlayEndTime = true,
            Fade = 0.5f,
            AlphaTint = true,
        };

        var config = contract.ToPhiRenderConfig();

        Assert.Equal(1920u, config.Width);
        Assert.Equal(1080u, config.Height);
        Assert.Equal(3.5, config.EndingLength);
        Assert.True(config.RenderLoading);
        Assert.True(config.Hires);
        Assert.Equal(1.5f, config.ChartDebugLine);
        Assert.Equal(60u, config.Fps);
        Assert.True(config.HardwareAccel);
        Assert.False(config.Hevc);
        Assert.True(config.Mpeg4);
        Assert.Equal("libx264", config.CustomEncoder);
        Assert.Equal(PhiChallengeColor.Golden, config.ChallengeColor);
        Assert.Equal(16u, config.ChallengeRank);
        Assert.Equal(PhiAudioMixMode.Fft, config.AudioMixMode);
        Assert.Equal("AUTOPLAY", config.Combo);
        Assert.Equal(0.02, config.JudgeOffset);
        Assert.Equal(5000ul, config.MaxParticles);
        Assert.Equal(60.0, config.PlayEndTime);
        Assert.True(config.HasPlayEndTime);
        Assert.True(config.AlphaTint);
        Assert.True(config.RenderBgDim);
    }
}
