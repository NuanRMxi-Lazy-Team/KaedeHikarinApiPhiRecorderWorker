using System.Text.Json;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging.Contracts;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Tests.Messaging;

public class ContractJsonTests
{
    [Fact]
    public void RenderTaskMessage_RoundTrips_WithCamelCaseAndStringEnums()
    {
        var message = new RenderTaskMessage
        {
            JobId = Guid.NewGuid(),
            ChartPresignedUrl = "https://example.com/chart.zip?sig=abc",
            OutputObjectKey = "render/abc/output.mp4",
            Config = new RenderConfigContract
            {
                Width = 1920,
                Height = 1080,
                Fps = 60,
                AudioMixMode = RenderAudioMixModeContract.Culling,
                ChallengeColor = RenderChallengeColorContract.Rainbow,
                CustomEncoder = null,
            },
            SubmittedAtUtc = DateTimeOffset.Parse("2026-09-06T12:00:00Z"),
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(message, ContractJson.Options);
        var text = System.Text.Encoding.UTF8.GetString(json);
        Assert.Contains("\"jobId\"", text);
        Assert.Contains("\"chartPresignedUrl\"", text);
        Assert.Contains("\"audioMixMode\":\"Culling\"", text);
        Assert.Contains("\"challengeColor\":\"Rainbow\"", text);

        var deserialized = JsonSerializer.Deserialize<RenderTaskMessage>(json, ContractJson.Options);
        Assert.NotNull(deserialized);
        Assert.Equal(message.JobId, deserialized.JobId);
        Assert.Equal(message.ChartPresignedUrl, deserialized.ChartPresignedUrl);
        Assert.Equal(message.OutputObjectKey, deserialized.OutputObjectKey);
        Assert.Equal(message.Config.Width, deserialized.Config.Width);
        Assert.Equal(RenderAudioMixModeContract.Culling, deserialized.Config.AudioMixMode);
        Assert.Equal(RenderChallengeColorContract.Rainbow, deserialized.Config.ChallengeColor);
        Assert.Equal(message.SubmittedAtUtc, deserialized.SubmittedAtUtc);
    }

    [Fact]
    public void RenderEventMessage_RoundTrips_WithEnums()
    {
        var message = RenderEventMessage.Failed(
            Guid.NewGuid(),
            RenderFailReason.QueueWaitTimeout,
            "queued too long");
        var json = JsonSerializer.SerializeToUtf8Bytes(message, ContractJson.Options);
        var deserialized = JsonSerializer.Deserialize<RenderEventMessage>(json, ContractJson.Options);
        Assert.NotNull(deserialized);
        Assert.Equal(message.JobId, deserialized.JobId);
        Assert.Equal(RenderEventType.Failed, deserialized.EventType);
        Assert.Equal(RenderFailReason.QueueWaitTimeout, deserialized.Reason);
        Assert.Equal("queued too long", deserialized.Error);
    }

    [Fact]
    public void RenderControlMessage_RoundTrips()
    {
        var message = new RenderControlMessage { JobId = Guid.NewGuid(), Action = "cancel" };
        var json = JsonSerializer.SerializeToUtf8Bytes(message, ContractJson.Options);
        var deserialized = JsonSerializer.Deserialize<RenderControlMessage>(json, ContractJson.Options);
        Assert.NotNull(deserialized);
        Assert.Equal(message.JobId, deserialized.JobId);
        Assert.Equal("cancel", deserialized.Action);
    }

    [Fact]
    public void RenderConfigContract_Defaults_MatchUpstreamBasePreset()
    {
        var config = new RenderConfigContract();

        Assert.Equal(1920u, config.Width);
        Assert.Equal(1080u, config.Height);
        Assert.Equal(60u, config.Fps);
        Assert.Equal(1.0f, config.ChartRatio);
        Assert.True(config.HardwareAccel);
        Assert.True(config.DynamicBitrateControl);
        Assert.Equal("28", config.Bitrate);
        Assert.True(config.AggressiveChart);
        Assert.Equal(RenderChallengeColorContract.Rainbow, config.ChallengeColor);
        Assert.Equal(3u, config.ChallengeRank);
        Assert.Equal(1.0f, config.NoteScale);
        Assert.True(config.Particle);
        Assert.Equal(16.0f, config.PlayerRks);
        Assert.Equal(8u, config.SampleCount);
        Assert.Equal(1.0f, config.Speed);
        Assert.Equal(0.5f, config.VolumeMusic);
        Assert.Equal(0.4f, config.VolumeSfx);
        Assert.True(config.ForceLimit);
        Assert.Equal(0.5f, config.LimitThreshold);
        Assert.Equal(RenderAudioMixModeContract.Culling, config.AudioMixMode);
        Assert.Equal("AUTOPLAY", config.Combo);
        Assert.Equal("%date% %time% %info.name%_%level_prefix%", config.FileNameFormat);
        Assert.True(config.RenderLine);
        Assert.True(config.RenderExtra);
        Assert.Equal(80f, config.BackgroundBlurriness);
        Assert.Equal(5000ul, config.MaxParticles);

        var deserialized = JsonSerializer.Deserialize<RenderConfigContract>("{}", ContractJson.Options);
        Assert.NotNull(deserialized);
        Assert.Equal(1920u, deserialized.Width);
        Assert.Equal(60u, deserialized.Fps);
        Assert.Equal(RenderAudioMixModeContract.Culling, deserialized.AudioMixMode);
    }
}
