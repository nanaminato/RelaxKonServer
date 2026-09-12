namespace RelaxKonServer.Options;

public sealed class DocumentationOptions
{
    public const string SectionName = "Documentation";
    public string RootPath { get; init; } = "Content/Docs";
    public int CacheMinutes { get; init; } = 30;
}
