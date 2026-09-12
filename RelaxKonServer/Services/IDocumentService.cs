using RelaxKonServer.Models;

namespace RelaxKonServer.Services;

public interface IDocumentService
{
    Task<IReadOnlyList<LanguageInfo>> GetLanguagesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetVersionsAsync(string? language, CancellationToken cancellationToken);

    Task<IReadOnlyList<NavigationNode>?> GetNavigationAsync(string language, string version, CancellationToken cancellationToken);

    Task<DocumentResponse?> GetDocumentAsync(string language, string version, string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, string language, string version, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string language, string version, string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetSlugsAsync(string language, string version, CancellationToken cancellationToken);
}
