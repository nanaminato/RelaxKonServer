using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RelaxKonServer.Markdown;
using RelaxKonServer.Models;
using RelaxKonServer.Options;

namespace RelaxKonServer.Services;

public interface IReleaseService
{
    Task<IReadOnlyList<ReleaseSummary>> GetReleasesAsync(CancellationToken cancellationToken);
    Task<ReleaseDetails?> GetReleaseAsync(string version, CancellationToken cancellationToken);
}

public sealed class ReleaseService : IReleaseService
{
    private static readonly Regex ListItem = new("^\\s*[-*]\\s+(?<text>.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly IMemoryCache _cache;
    private readonly string _rootPath;
    private readonly TimeSpan _cacheDuration;

    public ReleaseService(IWebHostEnvironment environment, IOptions<ContentOptions> options, IMemoryCache cache)
    {
        _cache = cache;
        var contentRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));
        _rootPath = Path.GetFullPath(Path.Combine(contentRoot, options.Value.ReleasesPath));
        _cacheDuration = TimeSpan.FromMinutes(Math.Clamp(options.Value.CacheMinutes, 1, 120));
    }

    public async Task<IReadOnlyList<ReleaseSummary>> GetReleasesAsync(CancellationToken cancellationToken)
    {
        var releases = await _cache.GetOrCreateAsync("releases:all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            var loaded = new List<(ReleaseSummary Summary, string Path)>();
            if (!Directory.Exists(_rootPath)) return loaded;

            var files = Directory.EnumerateFiles(_rootPath, "*.md").OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                var source = await File.ReadAllTextAsync(file, cancellationToken);
                var metadata = FrontMatter.Parse(source);
                loaded.Add((ToSummary(metadata, file), file));
            }

            return loaded.OrderByDescending(item => item.Summary.ReleaseDate).ToList();
        });

        return releases!.Select(item => item.Summary).ToArray();
    }

    public async Task<ReleaseDetails?> GetReleaseAsync(string version, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Length > 64 || version.Contains("..")) return null;

        var releases = await GetReleasesAsync(cancellationToken);
        var summary = releases.FirstOrDefault(item => item.Version.Equals(version, StringComparison.OrdinalIgnoreCase));

        var file = await _cache.GetOrCreateAsync($"releases:file:{version}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            if (!Directory.Exists(_rootPath)) return null;
            return Directory.EnumerateFiles(_rootPath, "*.md")
                .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path).Equals(version, StringComparison.OrdinalIgnoreCase));
        });

        if (file is null || !File.Exists(file)) return null;

        var source = await File.ReadAllTextAsync(file, cancellationToken);
        var metadata = FrontMatter.Parse(source);
        var content = FrontMatter.Strip(source);
        summary ??= ToSummary(metadata, file);

        return new ReleaseDetails(
            summary.Version,
            summary.Title,
            summary.Summary,
            content,
            summary.ReleaseDate,
            summary.IsPrerelease,
            ExtractHighlights(content));
    }

    private static ReleaseSummary ToSummary(IReadOnlyDictionary<string, string> metadata, string path)
    {
        var version = metadata.GetValueOrDefault("version") ?? Path.GetFileNameWithoutExtension(path);
        var title = metadata.GetValueOrDefault("title") ?? version;
        var summary = metadata.GetValueOrDefault("summary") ?? string.Empty;
        var date = DateTimeOffset.TryParse(metadata.GetValueOrDefault("date"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? DateOnly.FromDateTime(parsed.UtcDateTime)
            : DateOnly.FromDateTime(File.GetLastWriteTimeUtc(path));
        var prerelease = bool.TryParse(metadata.GetValueOrDefault("prerelease"), out var isPre) && isPre;

        return new ReleaseSummary(version, title, summary, date, prerelease);
    }

    private static IReadOnlyList<string> ExtractHighlights(string content) =>
        ListItem.Matches(content)
            .Select(match => match.Groups["text"].Value.Trim())
            .Take(6)
            .ToArray();
}
