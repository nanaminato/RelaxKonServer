namespace RelaxKonServer.Models;

public sealed record DocumentLink(string Slug, string Title);

public sealed record DocumentSummary(
    string Id,
    string Slug,
    string Title,
    string Description,
    string Category,
    int Order,
    string Language,
    string Version,
    DateTimeOffset LastUpdated);

public sealed record DocumentResponse(
    string Id,
    string Slug,
    string Title,
    string Description,
    string Category,
    int Order,
    string Content,
    string Language,
    string Version,
    DateTimeOffset LastUpdated,
    DocumentLink? Previous,
    DocumentLink? Next,
    IReadOnlyList<DocumentHeading> Headings,
    bool IsFallback);

public sealed record DocumentHeading(string Id, string Text, int Level);

public sealed record NavigationNode(string Title, string? Slug, IReadOnlyList<NavigationNode> Children);

public sealed record SearchResult(
    string Slug,
    string Title,
    string Description,
    string Category,
    string Language,
    string Version,
    string Snippet);

public sealed record LanguageInfo(string Code, string Name);
