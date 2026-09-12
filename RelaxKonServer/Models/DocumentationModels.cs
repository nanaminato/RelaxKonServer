namespace RelaxKonServer.Models;

public sealed record DocumentLink(string Slug, string Title);
public sealed record DocumentSummary(string Id, string Slug, string Title, string Description, string Category, int Order, string Language, string Version, DateTimeOffset LastUpdated);
public sealed record DocumentResponse(string Id, string Slug, string Title, string Description, string Category, int Order, string Content, string Language, string Version, DateTimeOffset LastUpdated, DocumentLink? Previous, DocumentLink? Next);
public sealed record NavigationNode(string Title, string? Slug, IReadOnlyList<NavigationNode> Children);
public sealed record SearchResult(string Slug, string Title, string Description, string Category, string Language, string Version, string Snippet);
public sealed record LanguageInfo(string Code, string Name);
public sealed record DownloadInfo(string Platform, string Architecture, string Version, string Url, string Size, string Checksum, DateOnly ReleaseDate, bool IsAvailable);
public sealed record ReleaseSummary(string Version, string Title, string Summary, DateOnly ReleaseDate);
public sealed record ReleaseDetails(string Version, string Title, string Content, DateOnly ReleaseDate);
public sealed record FaqItem(string Question, string Answer, int Order);
