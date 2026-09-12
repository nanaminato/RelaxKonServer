using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RelaxKonServer.Models;
using RelaxKonServer.Options;

namespace RelaxKonServer.Services;

public sealed class DocumentService : IDocumentService
{
    private static readonly Regex SafeSegment = new("^[A-Za-z0-9][A-Za-z0-9-]*$", RegexOptions.Compiled);
    private static readonly Regex SafeSlug = new("^[A-Za-z0-9][A-Za-z0-9/_-]*$", RegexOptions.Compiled);
    private readonly IMemoryCache _cache;
    private readonly string _rootPath;
    private readonly TimeSpan _cacheDuration;

    public DocumentService(IWebHostEnvironment environment, IOptions<DocumentationOptions> options, IMemoryCache cache)
    {
        _cache = cache;
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));
        _cacheDuration = TimeSpan.FromMinutes(Math.Clamp(options.Value.CacheMinutes, 1, 120));
    }

    public Task<IReadOnlyList<LanguageInfo>> GetLanguagesAsync(CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync("docs:languages", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return Task.FromResult<IReadOnlyList<LanguageInfo>>(Directory.Exists(_rootPath)
                ? Directory.GetDirectories(_rootPath).Select(Path.GetFileName).Where(IsSafeSegment).Order().Select(code => new LanguageInfo(code!, LanguageName(code!))).ToArray() : []);
        })!;

    public Task<IReadOnlyList<string>> GetVersionsAsync(string? language, CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync($"docs:versions:{language ?? "all"}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return Task.FromResult<IReadOnlyList<string>>(GetLanguageDirectories(language).SelectMany(Directory.GetDirectories)
                .Select(directory => Path.GetFileName(directory)!).Where(IsSafeSegment).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(version => version == "latest").ThenByDescending(version => version).ToArray());
        })!;

    public async Task<IReadOnlyList<NavigationNode>?> GetNavigationAsync(string language, string version, CancellationToken cancellationToken)
    {
        var documents = await GetDocumentsAsync(language, version);
        return documents is null ? null : documents.GroupBy(document => document.Category).OrderBy(group => group.Min(document => document.Order))
            .Select(group => new NavigationNode(group.Key, null, group.OrderBy(document => document.Order).ThenBy(document => document.Title)
                .Select(document => new NavigationNode(document.Title, document.Slug, [])).ToArray())).ToArray();
    }

    public async Task<DocumentResponse?> GetDocumentAsync(string language, string version, string slug, CancellationToken cancellationToken)
    {
        if (!IsSafeSegment(language) || !IsSafeSegment(version) || !SafeSlug.IsMatch(slug)) return null;
        var documents = await GetDocumentsAsync(language, version);
        var document = documents?.FirstOrDefault(item => item.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        var path = GetSafePath(Path.Combine(language, version, slug + ".md"));
        if (document is null || path is null || !File.Exists(path)) return null;
        var ordered = documents!.OrderBy(item => item.Order).ThenBy(item => item.Title).ToArray();
        var index = Array.FindIndex(ordered, item => item.Slug.Equals(document.Slug, StringComparison.OrdinalIgnoreCase));
        return new DocumentResponse(document.Id, document.Slug, document.Title, document.Description, document.Category, document.Order,
            StripFrontMatter(await File.ReadAllTextAsync(path, cancellationToken)), document.Language, document.Version, document.LastUpdated,
            index > 0 ? new DocumentLink(ordered[index - 1].Slug, ordered[index - 1].Title) : null,
            index >= 0 && index < ordered.Length - 1 ? new DocumentLink(ordered[index + 1].Slug, ordered[index + 1].Title) : null);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, string language, string version, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return [];
        var documents = await GetDocumentsAsync(language, version);
        if (documents is null) return [];
        var needle = query.Trim(); var results = new List<SearchResult>();
        foreach (var document in documents)
        {
            var path = GetSafePath(Path.Combine(language, version, document.Slug + ".md"));
            if (path is null || !File.Exists(path)) continue;
            var content = StripFrontMatter(await File.ReadAllTextAsync(path, cancellationToken));
            var index = content.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (index < 0 && !document.Title.Contains(needle, StringComparison.OrdinalIgnoreCase) && !document.Description.Contains(needle, StringComparison.OrdinalIgnoreCase)) continue;
            var start = Math.Max(0, index < 0 ? 0 : index - 72);
            var snippet = content.Length == 0 ? document.Description : content.Substring(start, Math.Min(180, content.Length - start)).Replace('\n', ' ').Trim();
            results.Add(new SearchResult(document.Slug, document.Title, document.Description, document.Category, language, version, snippet));
        }
        return results.Take(20).ToArray();
    }

    private Task<IReadOnlyList<DocumentSummary>?> GetDocumentsAsync(string language, string version)
    {
        if (!IsSafeSegment(language) || !IsSafeSegment(version)) return Task.FromResult<IReadOnlyList<DocumentSummary>?>(null);
        return _cache.GetOrCreateAsync($"docs:index:{language}:{version}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            var versionPath = GetSafePath(Path.Combine(language, version));
            if (versionPath is null || !Directory.Exists(versionPath)) return Task.FromResult<IReadOnlyList<DocumentSummary>?>(null);
            IReadOnlyList<DocumentSummary> documents = Directory.EnumerateFiles(versionPath, "*.md", SearchOption.AllDirectories)
                .Select(path => ToSummary(path, language, version, versionPath)).OrderBy(document => document.Order).ToArray();
            return Task.FromResult<IReadOnlyList<DocumentSummary>?>(documents);
        })!;
    }

    private DocumentSummary ToSummary(string path, string language, string version, string versionPath)
    {
        var metadata = ParseFrontMatter(File.ReadAllText(path));
        var slug = Path.GetRelativePath(versionPath, path).Replace('\\', '/')[..^3];
        var fallbackTitle = slug.Split('/').Last().Replace('-', ' ');
        return new DocumentSummary(slug, slug, metadata.GetValueOrDefault("title", fallbackTitle), metadata.GetValueOrDefault("description", "RelaxKonOS documentation."),
            metadata.GetValueOrDefault("category", CategoryFromSlug(slug)), int.TryParse(metadata.GetValueOrDefault("order"), out var order) ? order : 999,
            language, version, new DateTimeOffset(File.GetLastWriteTimeUtc(path)));
    }

    private string? GetSafePath(string relative)
    {
        var candidate = Path.GetFullPath(Path.Combine(_rootPath, relative));
        var prefix = _rootPath.EndsWith(Path.DirectorySeparatorChar) ? _rootPath : _rootPath + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }
    private IEnumerable<string> GetLanguageDirectories(string? language) => string.IsNullOrWhiteSpace(language)
        ? Directory.Exists(_rootPath) ? Directory.GetDirectories(_rootPath) : []
        : IsSafeSegment(language) && GetSafePath(language) is { } path && Directory.Exists(path) ? [path] : [];
    private static bool IsSafeSegment(string? value) => value is not null && SafeSegment.IsMatch(value);
    private static string CategoryFromSlug(string slug) => slug.Split('/').FirstOrDefault() switch { "apps" => "Applications", "concepts" => "Concepts", _ => "Getting Started" };
    private static string LanguageName(string code) => code switch { "zh-CN" => "简体中文", "ja-JP" => "日本語", _ => "English" };
    private static Dictionary<string, string> ParseFrontMatter(string source)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!source.StartsWith("---", StringComparison.Ordinal)) return result;
        var end = source.IndexOf("\n---", 3, StringComparison.Ordinal); if (end < 0) return result;
        foreach (var line in source[3..end].Split('\n')) { var separator = line.IndexOf(':'); if (separator > 0) result[line[..separator].Trim()] = line[(separator + 1)..].Trim().Trim('"'); }
        return result;
    }
    private static string StripFrontMatter(string source)
    {
        if (!source.StartsWith("---", StringComparison.Ordinal)) return source;
        var end = source.IndexOf("\n---", 3, StringComparison.Ordinal); return end < 0 ? source : source[(end + 4)..].TrimStart('\r', '\n');
    }
}
