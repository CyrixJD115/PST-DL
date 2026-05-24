namespace PSTManager.Core.Services;

public class DownloadService
{
    private readonly HttpClient _http;
    private readonly LogService _log;

    public DownloadService(HttpClient http, LogService log)
    {
        _http = http;
        _log = log;
    }

    public async Task<string> DownloadAsync(string url, string destinationDir, IProgress<double>? progress = null, IProgress<string>? statusProgress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destinationDir);

        var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
        if (string.IsNullOrEmpty(fileName)) fileName = "download.zip";
        var outputPath = Path.Combine(destinationDir, fileName);

        _log.Info($"DownloadAsync: url={url}");
        _log.Info($"DownloadAsync: output={outputPath}");

        try
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            _log.Info($"DownloadAsync: status={response.StatusCode}");
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            _log.Info($"DownloadAsync: content length={totalBytes}");

            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var pct = (double)totalRead / totalBytes * 100;
                    progress?.Report(pct);
                    statusProgress?.Report($"Downloading... {pct:F0}%");
                }
                else
                {
                    var mb = totalRead / 1024.0 / 1024.0;
                    statusProgress?.Report($"Downloading... {mb:F1} MB");
                }
            }

            _log.Info($"DownloadAsync: complete, {totalRead} bytes written");
            return outputPath;
        }
        catch (Exception ex)
        {
            _log.Error($"DownloadAsync: failed for {url}", ex);
            throw;
        }
    }
}
