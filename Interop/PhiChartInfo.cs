namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;

/// <summary>Managed snapshot of a native <c>phi_chart_info_view_t</c>; all strings are owned copies.</summary>
public sealed record PhiChartInfoData
{
    public int? Id { get; init; }
    public string? Guid { get; init; }
    public int? Uploader { get; init; }

    public string Name { get; init; } = "";
    public float Difficulty { get; init; }
    public string Level { get; init; } = "";
    public string Charter { get; init; } = "";
    public string Composer { get; init; } = "";
    public string Illustrator { get; init; } = "";
    public string Chart { get; init; } = "";
    public PhiChartFormat? Format { get; init; }
    public string Music { get; init; } = "";
    public string Illustration { get; init; } = "";
    public string? UnlockVideo { get; init; }

    public double PreviewStart { get; init; }
    public double? PreviewEnd { get; init; }
    public float AspectRatio { get; init; }
    public bool ForceAspectRatio { get; init; }
    public float BackgroundDim { get; init; }
    public float LineLength { get; init; }
    public double Offset { get; init; }
    public string? Tip { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];

    public string Intro { get; init; } = "";
    public bool HoldPartialCover { get; init; }
    public bool NegativeLengthHold { get; init; }
    public bool NoteUniformScale { get; init; }
    public uint ScoreTotal { get; init; }
    public float HoldParticleIntervalRatio { get; init; }
    public bool FoldAnimation { get; init; }

    public string? Created { get; init; }
    public string? Updated { get; init; }
    public string? ChartUpdated { get; init; }
}

/// <summary>
/// Owns one native <c>phi_chart_info_t</c> handle. The handle must not outlive
/// the <see cref="PhiRenderer"/> that loaded it.
/// </summary>
public sealed class PhiChartInfo : IDisposable
{
    private readonly PhiRenderer _renderer;
    private nint _handle;
    private bool _disposed;

    internal nint Handle => _handle;

    internal PhiChartInfo(PhiRenderer renderer, nint handle)
    {
        _renderer = renderer;
        _handle = handle;
    }

    /// <summary>Returns a fully owned copy of the current native view.</summary>
    public unsafe PhiChartInfoData GetView()
    {
        EnsureNotDisposed();

        PhiChartInfoView native = default;
        PhiNative.phi_chart_info_get_view(_handle, &native).ThrowIfError("phi_chart_info_get_view");

        var tags = new List<string>((int)Math.Min(native.TagCount, 4096));
        if (native.Tags != 0 && native.TagCount > 0)
        {
            PhiStringView* tagPtr = (PhiStringView*)native.Tags;
            for (nuint i = 0; i < native.TagCount; i++)
            {
                tags.Add(Utf8.Decode(tagPtr[i].Data, tagPtr[i].Length));
            }
        }

        return new PhiChartInfoData
        {
            Id = native.HasId != 0 ? native.Id : null,
            Guid = native.HasGuid != 0 ? Utf8.Decode(native.Guid.Data, native.Guid.Length) : null,
            Uploader = native.HasUploader != 0 ? native.Uploader : null,
            Name = Utf8.Decode(native.Name.Data, native.Name.Length),
            Difficulty = native.Difficulty,
            Level = Utf8.Decode(native.Level.Data, native.Level.Length),
            Charter = Utf8.Decode(native.Charter.Data, native.Charter.Length),
            Composer = Utf8.Decode(native.Composer.Data, native.Composer.Length),
            Illustrator = Utf8.Decode(native.Illustrator.Data, native.Illustrator.Length),
            Chart = Utf8.Decode(native.Chart.Data, native.Chart.Length),
            Format = native.HasFormat != 0 ? (PhiChartFormat)native.Format : null,
            Music = Utf8.Decode(native.Music.Data, native.Music.Length),
            Illustration = Utf8.Decode(native.Illustration.Data, native.Illustration.Length),
            UnlockVideo = native.HasUnlockVideo != 0
                ? Utf8.Decode(native.UnlockVideo.Data, native.UnlockVideo.Length)
                : null,
            PreviewStart = native.PreviewStart,
            PreviewEnd = native.HasPreviewEnd != 0 ? native.PreviewEnd : null,
            AspectRatio = native.AspectRatio,
            ForceAspectRatio = native.ForceAspectRatio != 0,
            BackgroundDim = native.BackgroundDim,
            LineLength = native.LineLength,
            Offset = native.Offset,
            Tip = native.HasTip != 0 ? Utf8.Decode(native.Tip.Data, native.Tip.Length) : null,
            Tags = tags,
            Intro = Utf8.Decode(native.Intro.Data, native.Intro.Length),
            HoldPartialCover = native.HoldPartialCover != 0,
            NegativeLengthHold = native.NegativeLengthHold != 0,
            NoteUniformScale = native.NoteUniformScale != 0,
            ScoreTotal = native.ScoreTotal,
            HoldParticleIntervalRatio = native.HoldParticleIntervalRatio,
            FoldAnimation = native.FoldAnimation != 0,
            Created = native.HasCreated != 0 ? Utf8.Decode(native.Created.Data, native.Created.Length) : null,
            Updated = native.HasUpdated != 0 ? Utf8.Decode(native.Updated.Data, native.Updated.Length) : null,
            ChartUpdated = native.HasChartUpdated != 0
                ? Utf8.Decode(native.ChartUpdated.Data, native.ChartUpdated.Length)
                : null,
        };
    }

    /// <summary>
    /// Replaces the whole native view; the native side deep-copies all strings,
    /// timestamps and tags before replacing the handle.
    /// </summary>
    public unsafe void SetView(PhiChartInfoData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        EnsureNotDisposed();

        byte[] guid = Utf8.Encode(data.Guid);
        byte[] name = Utf8.Encode(data.Name);
        byte[] level = Utf8.Encode(data.Level);
        byte[] charter = Utf8.Encode(data.Charter);
        byte[] composer = Utf8.Encode(data.Composer);
        byte[] illustrator = Utf8.Encode(data.Illustrator);
        byte[] chart = Utf8.Encode(data.Chart);
        byte[] music = Utf8.Encode(data.Music);
        byte[] illustration = Utf8.Encode(data.Illustration);
        byte[] unlockVideo = Utf8.Encode(data.UnlockVideo);
        byte[] tip = Utf8.Encode(data.Tip);
        byte[] intro = Utf8.Encode(data.Intro);
        byte[] created = Utf8.Encode(data.Created);
        byte[] updated = Utf8.Encode(data.Updated);
        byte[] chartUpdated = Utf8.Encode(data.ChartUpdated);
        byte[][] tagBytes = data.Tags.Select(Utf8.Encode).ToArray();
        PhiStringView[] tagViews = new PhiStringView[tagBytes.Length];

        fixed (byte* guidPtr = guid,
               namePtr = name,
               levelPtr = level,
               charterPtr = charter,
               composerPtr = composer,
               illustratorPtr = illustrator,
               chartPtr = chart,
               musicPtr = music,
               illustrationPtr = illustration,
               unlockVideoPtr = unlockVideo,
               tipPtr = tip,
               introPtr = intro,
               createdPtr = created,
               updatedPtr = updated,
               chartUpdatedPtr = chartUpdated)
        fixed (PhiStringView* tagViewsPtr = tagViews)
        {
            for (int i = 0; i < tagBytes.Length; i++)
            {
                fixed (byte* tagPtr = tagBytes[i])
                {
                    tagViews[i] = new PhiStringView((nint)tagPtr, (nuint)tagBytes[i].Length);
                }
            }

            PhiChartInfoView native = new()
            {
                StructSize = (uint)sizeof(PhiChartInfoView),
                AbiVersion = PhiNative.AbiVersion,
                Id = data.Id ?? 0,
                HasId = data.Id.HasValue ? (byte)1 : (byte)0,
                Guid = new PhiStringView((nint)guidPtr, (nuint)guid.Length),
                HasGuid = data.Guid is null ? (byte)0 : (byte)1,
                Uploader = data.Uploader ?? 0,
                HasUploader = data.Uploader.HasValue ? (byte)1 : (byte)0,
                Name = new PhiStringView((nint)namePtr, (nuint)name.Length),
                Difficulty = data.Difficulty,
                Level = new PhiStringView((nint)levelPtr, (nuint)level.Length),
                Charter = new PhiStringView((nint)charterPtr, (nuint)charter.Length),
                Composer = new PhiStringView((nint)composerPtr, (nuint)composer.Length),
                Illustrator = new PhiStringView((nint)illustratorPtr, (nuint)illustrator.Length),
                Chart = new PhiStringView((nint)chartPtr, (nuint)chart.Length),
                Format = (int)(data.Format ?? PhiChartFormat.Rpe),
                HasFormat = data.Format.HasValue ? (byte)1 : (byte)0,
                Music = new PhiStringView((nint)musicPtr, (nuint)music.Length),
                Illustration = new PhiStringView((nint)illustrationPtr, (nuint)illustration.Length),
                UnlockVideo = new PhiStringView((nint)unlockVideoPtr, (nuint)unlockVideo.Length),
                HasUnlockVideo = data.UnlockVideo is null ? (byte)0 : (byte)1,
                PreviewStart = data.PreviewStart,
                PreviewEnd = data.PreviewEnd ?? 0,
                HasPreviewEnd = data.PreviewEnd.HasValue ? (byte)1 : (byte)0,
                AspectRatio = data.AspectRatio,
                ForceAspectRatio = data.ForceAspectRatio ? (byte)1 : (byte)0,
                BackgroundDim = data.BackgroundDim,
                LineLength = data.LineLength,
                Offset = data.Offset,
                Tip = new PhiStringView((nint)tipPtr, (nuint)tip.Length),
                HasTip = data.Tip is null ? (byte)0 : (byte)1,
                Tags = (nint)tagViewsPtr,
                TagCount = (nuint)tagViews.Length,
                Intro = new PhiStringView((nint)introPtr, (nuint)intro.Length),
                HoldPartialCover = data.HoldPartialCover ? (byte)1 : (byte)0,
                NegativeLengthHold = data.NegativeLengthHold ? (byte)1 : (byte)0,
                NoteUniformScale = data.NoteUniformScale ? (byte)1 : (byte)0,
                ScoreTotal = data.ScoreTotal,
                HoldParticleIntervalRatio = data.HoldParticleIntervalRatio,
                FoldAnimation = data.FoldAnimation ? (byte)1 : (byte)0,
                Created = new PhiStringView((nint)createdPtr, (nuint)created.Length),
                HasCreated = data.Created is null ? (byte)0 : (byte)1,
                Updated = new PhiStringView((nint)updatedPtr, (nuint)updated.Length),
                HasUpdated = data.Updated is null ? (byte)0 : (byte)1,
                ChartUpdated = new PhiStringView((nint)chartUpdatedPtr, (nuint)chartUpdated.Length),
                HasChartUpdated = data.ChartUpdated is null ? (byte)0 : (byte)1,
            };

            PhiNative.phi_chart_info_set_view(_handle, &native).ThrowIfError("phi_chart_info_set_view");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_handle != 0)
        {
            PhiNative.phi_chart_info_destroy(_handle);
            _handle = 0;
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PhiChartInfo));
        }
    }
}
