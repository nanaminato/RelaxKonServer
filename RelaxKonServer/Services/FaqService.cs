using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RelaxKonServer.Models;
using RelaxKonServer.Options;

namespace RelaxKonServer.Services;

public interface IFaqService
{
    Task<IReadOnlyList<FaqItem>> GetFaqAsync(string language, CancellationToken cancellationToken);
}

public sealed class FaqService : IFaqService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private const string DefaultLanguage = "en-US";

    private readonly IMemoryCache _cache;
    private readonly string _rootPath;
    private readonly TimeSpan _cacheDuration;

    public FaqService(IWebHostEnvironment environment, IOptions<ContentOptions> options, IMemoryCache cache)
    {
        _cache = cache;
        var contentRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));
        _rootPath = Path.GetFullPath(Path.Combine(contentRoot, options.Value.FaqPath));
        _cacheDuration = TimeSpan.FromMinutes(Math.Clamp(options.Value.CacheMinutes, 1, 120));
    }

    public async Task<IReadOnlyList<FaqItem>> GetFaqAsync(string language, CancellationToken cancellationToken)
    {
        var code = Normalize(language);
        var items = await LoadAsync(code, cancellationToken);
        if (items.Count == 0 && code != DefaultLanguage) items = await LoadAsync(DefaultLanguage, cancellationToken);

        return items
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Question, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private Task<List<FaqItem>> LoadAsync(string language, CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync($"faq:{language}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            var path = Path.Combine(_rootPath, language + ".json");
            if (!File.Exists(path)) return new List<FaqItem>();

            await using var stream = File.OpenRead(path);
            var items = await JsonSerializer.DeserializeAsync<List<FaqItem>>(stream, SerializerOptions, cancellationToken) ?? [];
            return items.Select(item => item with { Language = language, Category = string.IsNullOrWhiteSpace(item.Category) ? "General" : item.Category }).ToList();
        })!;

    private static string Normalize(string? language) =>
        !string.IsNullOrWhiteSpace(language) && language.Length <= 16 && language.All(character => char.IsAsciiLetterOrDigit(character) || character == '-')
            ? language
            : DefaultLanguage;
}
