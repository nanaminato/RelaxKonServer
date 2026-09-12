using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RelaxKonServer.Markdown;
using RelaxKonServer.Models;
using RelaxKonServer.Options;

namespace RelaxKonServer.Services;

public sealed class DocumentService : IDocumentService
{
    private const string DefaultLanguage = "en-US";

    private static readonly Regex SafeSegment = new("^[A-Za-z0-9][A-Za-z0-9-]*$", RegexOptions.Compiled);
    private static readonly Regex SafeSlug = new("^[A-Za-z0-9][A-Za-z0-9/_-]*$", RegexOptions.Compiled);
    private static readonly Regex HeadingLine = new("^(?<level>#{1,3})\\s+(?<text>.+?)\\s*#*\\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex SlugifyPattern = new("[^a-z0-9\\u4e00-\\u9fff]+", RegexOptions.Compiled);

    // Markdown scrubbing for search snippets: a snippet should read like prose,
    // not like the raw source of the page.
    private static readonly Regex CodeFencePattern = new("```[\\s\\S]*?```", RegexOptions.Compiled);
    private static readonly Regex InlineCodePattern = new("`([^`]*)`", RegexOptions.Compiled);
    private static readonly Regex ImagePattern = new("!\\[([^\\]]*)\\]\\([^)]*\\)", RegexOptions.Compiled);
    private static readonly Regex LinkPattern = new("\\[([^\\]]*)\\]\\([^)]*\\)", RegexOptions.Compiled);
    private static readonly Regex HeadingPattern = new("^[ \\t]{0,3}#{1,6}[ \\t]+", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex BlockQuotePattern = new("^[ \\t]{0,3}>[ \\t]?", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex ListMarkerPattern = new("^[ \\t]{0,3}(?:[-*+]|\\d{1,3}\\.)[ \\t]+", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex TableDividerPattern = new("^[ \\t]*\\|?[ \\t]*:?-{3,}:?[ \\t]*(?:\\|[ \\t]*:?-{3,}:?[ \\t]*)+\\|?[ \\t]*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex EmphasisPattern = new("[*_]{1,3}", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new("\\s+", RegexOptions.Compiled);

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
                ? Directory.GetDirectories(_rootPath)
                    .Select(Path.GetFileName)
                    .Where(IsSafeSegment)
                    .OrderBy(code => LanguageOrder(code!))
                    .ThenBy(code => code, StringComparer.Ordinal)
                    .Select(code => new LanguageInfo(code!, LanguageName(code!)))
                    .ToArray()
                : []);
        })!;

    public Task<IReadOnlyList<string>> GetVersionsAsync(string? language, CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync($"docs:versions:{language ?? "all"}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return Task.FromResult<IReadOnlyList<string>>(GetLanguageDirectories(language)
                .SelectMany(Directory.GetDirectories)
                .Select(directory => Path.GetFileName(directory)!)
                .Where(IsSafeSegment)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(version => version == "latest")
                .ThenByDescending(version => version, StringComparer.OrdinalIgnoreCase)
                .ToArray());
        })!;

    public async Task<IReadOnlyList<NavigationNode>?> GetNavigationAsync(string language, string version, CancellationToken cancellationToken)
    {
        var documents = await GetDocumentsAsync(language, version);
        if (documents is null) return null;

        return documents
            .GroupBy(document => document.Category)
            .OrderBy(group => group.Min(document => document.Order))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new NavigationNode(
                group.Key,
                null,
                group.OrderBy(document => document.Order)
                    .ThenBy(document => document.Title, StringComparer.OrdinalIgnoreCase)
                    .Select(document => new NavigationNode(document.Title, document.Slug, []))
                    .ToArray()))
            .ToArray();
    }

    public async Task<DocumentResponse?> GetDocumentAsync(string language, string version, string slug, CancellationToken cancellationToken)
    {
        if (!IsSafeSlug(slug)) return null;

        var documents = await GetDocumentsAsync(language, version);
        if (documents is null) return null;

        var document = documents.FirstOrDefault(item => item.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        var path = GetSafePath(Path.Combine(document?.Language ?? language, version, slug + ".md"));
        if (document is null || path is null || !File.Exists(path)) return null;

        var ordered = documents.OrderBy(item => item.Order).ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ToArray();
        var index = Array.FindIndex(ordered, item => item.Slug.Equals(document.Slug, StringComparison.OrdinalIgnoreCase));
        var content = FrontMatter.Strip(await File.ReadAllTextAsync(path, cancellationToken));

        return new DocumentResponse(
            document.Id,
            document.Slug,
            document.Title,
            document.Description,
            document.Category,
            document.Order,
            content,
            language,
            version,
            document.LastUpdated,
            index > 0 ? new DocumentLink(ordered[index - 1].Slug, ordered[index - 1].Title) : null,
            index >= 0 && index < ordered.Length - 1 ? new DocumentLink(ordered[index + 1].Slug, ordered[index + 1].Title) : null,
            ExtractHeadings(content),
            !document.Language.Equals(language, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, string language, string version, CancellationToken cancellationToken)
    {
        var trimmed = query.Trim();
        if (trimmed.Length < 2) return [];

        var documents = await GetDocumentsAsync(language, version);
        if (documents is null) return [];

        var results = new List<SearchResult>();
        foreach (var document in documents)
        {
            var path = GetSafePath(Path.Combine(document.Language, version, document.Slug + ".md"));
            if (path is null || !File.Exists(path)) continue;

            var content = FrontMatter.Strip(await File.ReadAllTextAsync(path, cancellationToken));
            var index = content.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase);
            var titleMatch = document.Title.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
            var descriptionMatch = document.Description.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
            if (index < 0 && !titleMatch && !descriptionMatch) continue;

            var snippet = content.Length == 0
                ? document.Description
                : BuildSnippet(content, trimmed);

            results.Add(new SearchResult(document.Slug, document.Title, document.Description, document.Category, document.Language, document.Version, snippet));
        }

        return results
            .OrderByDescending(result => result.Title.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .ThenBy(result => result.Title, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();
    }

    public async Task<bool> SlugExistsAsync(string language, string version, string slug, CancellationToken cancellationToken)
    {
        if (!IsSafeSlug(slug)) return false;
        var documents = await GetDocumentsAsync(language, version);
        return documents?.Any(item => item.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)) == true;
    }

    public async Task<IReadOnlyList<string>> GetSlugsAsync(string language, string version, CancellationToken cancellationToken) =>
        (await GetDocumentsAsync(language, version))?.Select(item => item.Slug).ToArray() ?? [];

    private async Task<IReadOnlyList<DocumentSummary>?> GetDocumentsAsync(string language, string version)
    {
        var own = await LoadDocumentsAsync(language, version);
        if (string.Equals(language, DefaultLanguage, StringComparison.OrdinalIgnoreCase)) return own;

        // Merge untranslated slugs from the default language so a partially localized
        // documentation set still presents the complete navigation tree.
        var fallback = await LoadDocumentsAsync(DefaultLanguage, version);
        if (own is null) return fallback;
        if (fallback is null) return own;

        var known = own.Select(document => document.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var merged = own
            .Concat(fallback
                .Where(document => !known.Contains(document.Slug))
                // Fallback entries keep the requested language's category labels so the
                // navigation tree stays grouped under localised headings.
                .Select(document => document with { Category = LocalizedCategory(document.Slug, language) }))
            .ToArray();
        return merged;
    }

    private Task<IReadOnlyList<DocumentSummary>?> LoadDocumentsAsync(string language, string version)
    {
        if (!IsSafeSegment(language) || !IsSafeSegment(version)) return Task.FromResult<IReadOnlyList<DocumentSummary>?>(null);
        return _cache.GetOrCreateAsync($"docs:index:{language}:{version}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            var versionPath = GetSafePath(Path.Combine(language, version));
            if (versionPath is null || !Directory.Exists(versionPath)) return Task.FromResult<IReadOnlyList<DocumentSummary>?>(null);

            IReadOnlyList<DocumentSummary> documents = Directory.EnumerateFiles(versionPath, "*.md", SearchOption.AllDirectories)
                .Select(path => ToSummary(path, language, version, versionPath))
                .OrderBy(document => document.Order)
                .ThenBy(document => document.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Task.FromResult<IReadOnlyList<DocumentSummary>?>(documents);
        })!;
    }

    private static DocumentSummary ToSummary(string path, string language, string version, string versionPath)
    {
        var metadata = FrontMatter.Parse(File.ReadAllText(path));
        var slug = Path.GetRelativePath(versionPath, path).Replace('\\', '/')[..^3];
        var fallbackTitle = TitleFromSlug(slug);

        return new DocumentSummary(
            slug,
            slug,
            metadata.GetValueOrDefault("title", fallbackTitle),
            metadata.GetValueOrDefault("description", "RelaxKonOS documentation."),
            metadata.GetValueOrDefault("category", CategoryFromSlug(slug)),
            int.TryParse(metadata.GetValueOrDefault("order"), out var order) ? order : 999,
            language,
            version,
            new DateTimeOffset(File.GetLastWriteTimeUtc(path)));
    }

    private static IReadOnlyList<DocumentHeading> ExtractHeadings(string content) =>
        HeadingLine.Matches(content)
            .Select(match => new DocumentHeading(
                Slugify(match.Groups["text"].Value),
                match.Groups["text"].Value.Trim(),
                match.Groups["level"].Value.Length))
            .ToArray();

    /// <summary>Builds a readable preview around the matched term, free of Markdown syntax.</summary>
    private static string BuildSnippet(string content, string term, int maxLength = 200)
    {
        var plain = ToPlainText(content);
        if (plain.Length == 0) return string.Empty;
        if (plain.Length <= maxLength) return plain;

        var index = plain.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        var start = index <= 60 ? 0 : index - 60;
        if (start + maxLength > plain.Length) start = plain.Length - maxLength;
        if (start < 0) start = 0;

        var slice = plain.Substring(start, Math.Min(maxLength, plain.Length - start)).Trim();
        var prefix = start > 0 ? "…" : string.Empty;
        var suffix = start + maxLength < plain.Length ? "…" : string.Empty;
        return prefix + slice + suffix;
    }

    /// <summary>Reduces Markdown to plain prose so previews never leak raw syntax.</summary>
    private static string ToPlainText(string markdown)
    {
        var text = CodeFencePattern.Replace(markdown, " ");
        text = ImagePattern.Replace(text, "$1");
        text = LinkPattern.Replace(text, "$1");
        text = InlineCodePattern.Replace(text, "$1");
        text = TableDividerPattern.Replace(text, " ");
        text = HeadingPattern.Replace(text, string.Empty);
        text = BlockQuotePattern.Replace(text, string.Empty);
        text = ListMarkerPattern.Replace(text, string.Empty);
        text = EmphasisPattern.Replace(text, string.Empty);
        text = text.Replace('|', ' ');
        return WhitespacePattern.Replace(text, " ").Trim();
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
    private static bool IsSafeSlug(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && SafeSlug.IsMatch(value);

    private static string TitleFromSlug(string slug) => string.Concat(
        slug.Split('/').Last()
            .Replace('-', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));

    private static string Slugify(string text) => SlugifyPattern.Replace(text.ToLowerInvariant(), "-").Trim('-');

    private static string CategoryFromSlug(string slug) => slug.Split('/').FirstOrDefault() switch
    {
        "apps" => "Applications",
        "concepts" => "Concepts",
        _ => "Getting Started"
    };

    private static string LocalizedCategory(string slug, string language) => (slug.Split('/').FirstOrDefault(), language) switch
    {
        ("apps", "zh-CN") => "应用程序",
        ("concepts", "zh-CN") => "概念",
        (_, "zh-CN") => "开始使用",
        ("apps", "ja-JP") => "アプリケーション",
        ("concepts", "ja-JP") => "概念",
        (_, "ja-JP") => "はじめに",
        ("apps", _) => "Applications",
        ("concepts", _) => "Concepts",
        _ => "Getting Started"
    };

    private static int LanguageOrder(string code) => code switch { "en-US" => 0, "zh-CN" => 1, "ja-JP" => 2, _ => 99 };

    private static string LanguageName(string code) => code switch
    {
        "zh-CN" => "简体中文",
        "ja-JP" => "日本語",
        _ => "English"
    };
}
