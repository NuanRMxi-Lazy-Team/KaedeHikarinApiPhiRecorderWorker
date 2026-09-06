using System.Text;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;

internal static class Utf8
{
    public static byte[] Encode(string? value) =>
        string.IsNullOrEmpty(value) ? [] : Encoding.UTF8.GetBytes(value);

    public static unsafe string Decode(nint data, nuint length)
    {
        if (length == 0)
        {
            return string.Empty;
        }

        if (data == 0 || length > int.MaxValue)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(new ReadOnlySpan<byte>((void*)data, (int)length));
    }
}

public sealed class PhiException : Exception
{
    public PhiStatus Status { get; }

    public PhiException(PhiStatus status, string context)
        : base($"native call '{context}' failed with status {status}")
    {
        Status = status;
    }
}

public static class PhiStatusExtensions
{
    public static void ThrowIfError(this PhiStatus status, string context)
    {
        if (status != PhiStatus.Ok)
        {
            throw new PhiException(status, context);
        }
    }
}
