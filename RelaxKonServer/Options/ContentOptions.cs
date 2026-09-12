namespace RelaxKonServer.Options;

/// <summary>
/// Strongly typed options for the content the website API exposes beyond the
/// documentation tree (releases, FAQ and downloads).
/// </summary>
public sealed class ContentOptions
{
    public const string SectionName = "Content";

    /// <summary>Root folder, relative to the content root, that holds all content sets.</summary>
    public string RootPath { get; init; } = "Content";

    /// <summary>Release notes folder, relative to <see cref="RootPath"/>.</summary>
    public string ReleasesPath { get; init; } = "Releases";

    /// <summary>FAQ folder, relative to <see cref="RootPath"/>.</summary>
    public string FaqPath { get; init; } = "Faq";

    /// <summary>Downloads descriptor file, relative to <see cref="RootPath"/>.</summary>
    public string DownloadsPath { get; init; } = "Downloads/downloads.json";

    /// <summary>Cache lifetime in minutes for content endpoints.</summary>
    public int CacheMinutes { get; init; } = 30;
}
