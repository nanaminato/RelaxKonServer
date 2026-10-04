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
    Task<IReadOnlyList<ReleaseSummary>> GetReleasesAsync(string language, CancellationToken cancellationToken);
    Task<ReleaseDetails?> GetReleaseAsync(string version, string language, CancellationToken cancellationToken);
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

    public async Task<IReadOnlyList<ReleaseSummary>> GetReleasesAsync(string language, CancellationToken cancellationToken)
    {
        language = NormalizeLanguage(language);
        var releases = await _cache.GetOrCreateAsync($"releases:all:{language}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            var loaded = new List<(ReleaseSummary Summary, string Path)>();
            if (!Directory.Exists(_rootPath)) return loaded;

            var files = Directory.EnumerateFiles(_rootPath, "*.md").OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                var source = await File.ReadAllTextAsync(file, cancellationToken);
                var metadata = FrontMatter.Parse(source);
                var localized = await ReadTranslationAsync(file, language, cancellationToken);
                loaded.Add((ToSummary(metadata, localized.Metadata, file, localized.Language, localized.Language != language), file));
            }

            return loaded.OrderByDescending(item => item.Summary.ReleaseDate).ToList();
        });

        return releases!.Select(item => item.Summary).ToArray();
    }

    public async Task<ReleaseDetails?> GetReleaseAsync(string version, string language, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Length > 64 || version.Contains("..")) return null;

        language = NormalizeLanguage(language);
        var releases = await GetReleasesAsync(language, cancellationToken);
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
        var localized = await ReadTranslationAsync(file, language, cancellationToken);
        var shared = FrontMatter.Strip(source);
        foreach (var key in new[] { "artifactsTitle", "packageLabel", "downloadsLabel" })
            shared = shared.Replace("{" + key + "}", localized.Metadata.GetValueOrDefault(key) ?? key, StringComparison.Ordinal);
        var content = localized.Content + "\n\n" + shared;
        summary ??= ToSummary(metadata, localized.Metadata, file, localized.Language, localized.Language != language);

        return new ReleaseDetails(
            summary.Version,
            summary.Title,
            summary.Summary,
            content,
            summary.ReleaseDate,
            summary.IsPrerelease,
            ExtractHighlights(localized.Content),
            summary.Language,
            summary.IsFallback);
    }

    private static ReleaseSummary ToSummary(IReadOnlyDictionary<string, string> metadata, IReadOnlyDictionary<string, string> translation, string path, string language, bool isFallback)
    {
        var version = metadata.GetValueOrDefault("version") ?? Path.GetFileNameWithoutExtension(path);
        var title = translation.GetValueOrDefault("title") ?? version;
        var summary = translation.GetValueOrDefault("summary") ?? string.Empty;
        var date = DateTimeOffset.TryParse(metadata.GetValueOrDefault("date"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? DateOnly.FromDateTime(parsed.UtcDateTime)
            : DateOnly.FromDateTime(File.GetLastWriteTimeUtc(path));
        var prerelease = bool.TryParse(metadata.GetValueOrDefault("prerelease"), out var isPre) && isPre;

        return new ReleaseSummary(version, title, summary, date, prerelease, language, isFallback);
    }

    private static string NormalizeLanguage(string language) => language switch
    {
        "zh-CN" => "zh-CN",
        "ja-JP" => "ja-JP",
        _ => "en-US"
    };

    private async Task<(IReadOnlyDictionary<string, string> Metadata, string Content, string Language)> ReadTranslationAsync(
        string sharedFile, string language, CancellationToken cancellationToken)
    {
        var name = Path.GetFileName(sharedFile);
        var file = Path.Combine(_rootPath, language, name);
        if (!File.Exists(file))
        {
            language = "en-US";
            file = Path.Combine(_rootPath, language, name);
        }
        if (!File.Exists(file))
            throw new InvalidDataException($"Release {name} requires an English translation.");
        var source = await File.ReadAllTextAsync(file, cancellationToken);
        return (FrontMatter.Parse(source), FrontMatter.Strip(source), language);
    }

    private static IReadOnlyList<string> ExtractHighlights(string content) =>
        ListItem.Matches(content)
            .Select(match => match.Groups["text"].Value.Trim())
            .Take(6)
            .ToArray();
}
