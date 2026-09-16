namespace RelaxKon_Publisher.Models;

public sealed record PublisherPaths(string RelaxKonOSPath, string RelaxKonServerPath, string ContentOutputPath);

public sealed class PublisherPlanRequest
{
    public PublisherPaths Paths { get; init; } = new("", "", "");
    public string Version { get; init; } = "";
    public string Runtime { get; init; } = "win-x64";
    public bool BuildClient { get; init; } = true;
    public bool BuildServer { get; init; } = true;
    public bool IncludeChecksums { get; init; } = true;
    public bool IncludeDescriptors { get; init; } = true;
    public bool IncludeInstallers { get; init; }
    public bool OnlyLatestDownloadable { get; init; }
    public List<HistoricalPackageDecision> HistoricalPackages { get; init; } = [];
}

public sealed record HistoricalPackageDecision(string RelativePath, bool CopyToOutput, bool IsDownloadable);
public sealed record ExistingPackage(string RelativePath, string FileName, long Size, string? Version, string? Runtime, string? PackageKind, bool IsDownloadable);
public sealed record OutputChange(string RelativePath, string Action, string Reason);
public sealed record PublisherPreview(PublisherPaths Paths, string? GitRoot, string? Head, string? GitStatus, IReadOnlyList<string> Presets, IReadOnlyList<ExistingPackage> ExistingPackages, IReadOnlyList<OutputChange> Changes, IReadOnlyList<string> Warnings);
public sealed class PublisherJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string State { get; set; } = "queued";
    public string Step { get; set; } = "等待本机发布锁";
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
    public string? Error { get; set; }
    public IReadOnlyList<string> AffectedFiles { get; set; } = [];
}
