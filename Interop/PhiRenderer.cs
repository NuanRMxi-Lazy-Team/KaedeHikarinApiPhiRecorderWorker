namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;

/// <summary>
/// Owns one native <c>phi_context_t</c>. A context allows exactly one active
/// render job at a time; destroying the context waits for all context-owned
/// work in the native library to finish.
/// </summary>
public sealed class PhiRenderer : IDisposable
{
    private readonly object _gate = new();
    private nint _context;
    private bool _disposed;

    /// <summary>ABI version implemented by the native header this wrapper was built against.</summary>
    public static uint ExpectedAbiVersion => PhiNative.AbiVersion;

    /// <summary>ABI version reported by the loaded native library.</summary>
    public uint LoadedAbiVersion { get; }

    public PhiRenderer(PhiRendererOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var tempDirectory = options.ResolveTempDirectory();
        Directory.CreateDirectory(tempDirectory);

        LoadedAbiVersion = PhiNative.phi_abi_version();
        if (LoadedAbiVersion != PhiNative.AbiVersion)
        {
            throw new PhiException(
                PhiStatus.AbiMismatch,
                $"phi_abi_version (loaded {LoadedAbiVersion}, expected {PhiNative.AbiVersion})");
        }

        _context = CreateContext(options, tempDirectory);
    }

    public unsafe PhiChartInfo LoadChartInfo(string chartPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chartPath);
        EnsureNotDisposed();

        byte[] chartBytes = Utf8.Encode(chartPath);
        nint outInfo = 0;
        PhiStatus status;
        fixed (byte* chartPtr = chartBytes)
        {
            status = PhiNative.phi_chart_info_load(
                _context,
                new PhiStringView((nint)chartPtr, (nuint)chartBytes.Length),
                out outInfo);
        }

        status.ThrowIfError("phi_chart_info_load");
        return new PhiChartInfo(this, outInfo);
    }

    public PhiRenderConfig CreateDefaultConfig() => PhiRenderConfig.CreateDefault();

    /// <summary>
    /// Submits a render job to the private renderer host. The context allows one
    /// active job at a time; a second submit fails with <see cref="PhiStatus.Busy"/>.
    /// Events are delivered through <see cref="PhiJob.Events"/> from the native
    /// dispatcher thread; the callback must never call back into this library.
    /// </summary>
    public unsafe PhiJob SubmitRender(PhiRenderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureNotDisposed();

        var config = request.Config
            ?? throw new ArgumentException("a render config is required", nameof(request));
        var chartInfo = request.ChartInfo;

        byte[] chartBytes = Utf8.Encode(request.ChartPath);
        byte[] outputBytes = Utf8.Encode(request.OutputPath);

        PhiJob job;
        lock (_gate)
        {
            EnsureNotDisposed();
            job = new PhiJob(this);
            try
            {
                nint outJob = 0;
                PhiStatus status = config.WithNative(cfgPtr =>
                {
                    fixed (byte* chartPtr = chartBytes, outputPtr = outputBytes)
                    {
                        PhiRenderRequestStruct nativeRequest = new()
                        {
                            StructSize = (uint)sizeof(PhiRenderRequestStruct),
                            AbiVersion = PhiNative.AbiVersion,
                            ChartPath = new PhiStringView((nint)chartPtr, (nuint)chartBytes.Length),
                            OutputPath = new PhiStringView((nint)outputPtr, (nuint)outputBytes.Length),
                            Config = (nint)cfgPtr,
                            ChartInfo = chartInfo?.Handle ?? 0,
                        };
                        return PhiNative.phi_render_submit(
                            _context,
                            &nativeRequest,
                            job.CallbackDelegate,
                            job.UserData,
                            out outJob);
                    }
                });
                status.ThrowIfError("phi_render_submit");
                job.Attach(outJob);
            }
            catch
            {
                job.Dispose();
                throw;
            }
        }

        if (cancellationToken.CanBeCanceled)
        {
            job.RegisterCancellation(cancellationToken);
        }

        return job;
    }

    /// <summary>Clears the native error state of the context.</summary>
    public PhiStatus ClearError()
    {
        EnsureNotDisposed();
        return PhiNative.phi_context_clear_error(_context);
    }

    /// <summary>Returns the last error recorded by the native context, or null when none.</summary>
    public unsafe string? GetLastError()
    {
        EnsureNotDisposed();

        byte* buffer = stackalloc byte[4096];
        PhiStatus status = PhiNative.phi_context_get_last_error(_context, buffer, 4096, out nuint required);
        if (status != PhiStatus.Ok || required == 0)
        {
            return null;
        }

        int length = (int)Math.Min(required, 4096);
        return System.Text.Encoding.UTF8.GetString(new ReadOnlySpan<byte>(buffer, length));
    }

    /// <summary>
    /// Cheap readiness probe: reports the negotiated ABI version and whether a
    /// native context can be created with the configured resource roots.
    /// </summary>
    public PhiProbeInfo Probe()
    {
        EnsureNotDisposed();
        return new PhiProbeInfo(ExpectedAbiVersion, LoadedAbiVersion, GetLastError());
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_context != 0)
            {
                PhiNative.phi_context_destroy(_context);
                _context = 0;
            }
        }
    }

    private static unsafe nint CreateContext(PhiRendererOptions options, string tempDirectory)
    {
        byte[] assetsBytes = Utf8.Encode(options.ResolveAssetsDirectory());
        byte[] fontsBytes = Utf8.Encode(options.ResolveFontsDirectory());
        byte[] resourcePackBytes = Utf8.Encode(options.ResolveResourcePackDirectory());
        byte[] ffmpegBytes = Utf8.Encode(options.FfmpegPath);
        byte[] tempBytes = Utf8.Encode(tempDirectory);
        byte[] hostBytes = Utf8.Encode(options.ResolveRendererHostPath());

        fixed (byte* assetsPtr = assetsBytes,
               fontsPtr = fontsBytes,
               resourcePackPtr = resourcePackBytes,
               ffmpegPtr = ffmpegBytes,
               tempPtr = tempBytes,
               hostPtr = hostBytes)
        {
            PhiContextOptions nativeOptions = new()
            {
                StructSize = (uint)sizeof(PhiContextOptions),
                AbiVersion = PhiNative.AbiVersion,
                AssetsDir = new PhiStringView((nint)assetsPtr, (nuint)assetsBytes.Length),
                FontsDir = new PhiStringView((nint)fontsPtr, (nuint)fontsBytes.Length),
                ResourcePackDir = new PhiStringView((nint)resourcePackPtr, (nuint)resourcePackBytes.Length),
                FfmpegPath = new PhiStringView((nint)ffmpegPtr, (nuint)ffmpegBytes.Length),
                TempDir = new PhiStringView((nint)tempPtr, (nuint)tempBytes.Length),
                RendererHostPath = new PhiStringView((nint)hostPtr, (nuint)hostBytes.Length),
            };

            PhiStatus status = PhiNative.phi_context_create(&nativeOptions, out nint context);
            status.ThrowIfError("phi_context_create");
            return context;
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PhiRenderer));
        }
    }
}

public sealed record PhiProbeInfo(uint ExpectedAbiVersion, uint LoadedAbiVersion, string? LastError);
