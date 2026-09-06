namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging.Contracts;

public sealed class RenderControlMessage
{
    public Guid JobId { get; set; }

    public string Action { get; set; } = "cancel";
}
