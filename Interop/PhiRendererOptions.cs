namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;

public sealed class PhiRendererOptions
{
    public string AssetsDirectory { get; set; } = "native/assets";

    public string FontsDirectory { get; set; } = "native/assets";

    public string ResourcePackDirectory { get; set; } = "native/assets/respack";

    public string FfmpegPath { get; set; } = "ffmpeg";

    public string TempDirectory { get; set; } = "";

    public string RendererHostPath { get; set; } = "native/phi-renderer-host.exe";

    internal string ResolveAssetsDirectory() => ResolvePath(AssetsDirectory, "native/assets");

    internal string ResolveFontsDirectory() => ResolvePath(FontsDirectory, ResolveAssetsDirectory());

    internal string ResolveResourcePackDirectory() => ResolvePath(ResourcePackDirectory, Path.Combine(ResolveAssetsDirectory(), "respack"));

    internal string ResolveRendererHostPath() => ResolvePath(RendererHostPath, Path.Combine("native", HostFileName));

    internal string ResolveTempDirectory()
    {
        var value = string.IsNullOrWhiteSpace(TempDirectory)
            ? Path.Combine(Path.GetTempPath(), "phi-recorder-native")
            : TempDirectory;
        return Path.IsPathRooted(value)
            ? value
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, value));
    }

    internal static string HostFileName =>
        OperatingSystem.IsWindows() ? "phi-renderer-host.exe" : "phi-renderer-host";

    private static string ResolvePath(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            value = fallback;
        }

        return Path.IsPathRooted(value)
            ? value
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, value));
    }
}
