namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Rendering;

public sealed class ChartDownloader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ChartDownloader> _logger;

    public ChartDownloader(IHttpClientFactory httpClientFactory, ILogger<ChartDownloader> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> DownloadAsync(
        string presignedUrl,
        string workDirectory,
        CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient("chart");
        using var response = await client.GetAsync(
            presignedUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"chart download failed with {(int)response.StatusCode} ({response.ReasonPhrase})");
        }

        var chartPath = Path.Combine(workDirectory, "chart.zip");
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = File.Create(chartPath);
        await source.CopyToAsync(destination, cancellationToken);
        _logger.LogInformation(
            "chart downloaded: {Bytes} bytes to {Path}",
            new FileInfo(chartPath).Length,
            chartPath);
        return chartPath;
    }
}
