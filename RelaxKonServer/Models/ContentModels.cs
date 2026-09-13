namespace RelaxKonServer.Models;

public sealed record DownloadInfo(
    string Platform,
    string Architecture,
    string Version,
    string Url,
    string Size,
    string Checksum,
    DateOnly ReleaseDate,
    bool IsAvailable,
    string? FileName,
    string? PackageKind);

public sealed record ReleaseSummary(
    string Version,
    string Title,
    string Summary,
    DateOnly ReleaseDate,
    bool IsPrerelease);

public sealed record ReleaseDetails(
    string Version,
    string Title,
    string Summary,
    string Content,
    DateOnly ReleaseDate,
    bool IsPrerelease,
    IReadOnlyList<string> Highlights);

public sealed record FaqItem(
    string Question,
    string Answer,
    string Category,
    string Language,
    int Order);
