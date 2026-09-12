using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RelaxKonServer.Models;
using RelaxKonServer.Options;

namespace RelaxKonServer.Services;

public interface IDownloadService
{
    Task<IReadOnlyList<DownloadInfo>> GetDownloadsAsync(CancellationToken cancellationToken);
}

public sealed class DownloadService : IDownloadService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IMemoryCache _cache;
    private readonly string _descriptorPath;
    private readonly TimeSpan _cacheDuration;

    public DownloadService(IWebHostEnvironment environment, IOptions<ContentOptions> options, IMemoryCache cache)
    {
        _cache = cache;
        var contentRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));
        _descriptorPath = Path.GetFullPath(Path.Combine(contentRoot, options.Value.DownloadsPath));
        _cacheDuration = TimeSpan.FromMinutes(Math.Clamp(options.Value.CacheMinutes, 1, 120));
    }

    public async Task<IReadOnlyList<DownloadInfo>> GetDownloadsAsync(CancellationToken cancellationToken)
    {
        var items = await _cache.GetOrCreateAsync("downloads:all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            if (!File.Exists(_descriptorPath)) return new List<DownloadInfo>();

            await using var stream = File.OpenRead(_descriptorPath);
            return await JsonSerializer.DeserializeAsync<List<DownloadInfo>>(stream, SerializerOptions, cancellationToken) ?? [];
        });

        return items!
            .OrderBy(item => PlatformOrder(item.Platform))
            .ThenBy(item => item.Architecture, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int PlatformOrder(string platform) => platform.ToLowerInvariant() switch
    {
        "windows" => 0,
        "linux" => 1,
        "macos" => 2,
        _ => 9
    };
}
