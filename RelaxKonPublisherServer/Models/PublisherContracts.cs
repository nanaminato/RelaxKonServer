namespace RelaxKon_Publisher.Models;

public sealed record PublisherPaths(string RelaxKonOSPath, string RelaxKonServerPath, string ContentOutputPath);

public sealed class PublisherPlanRequest
{
    public PublisherPaths Paths { get; init; } = new("", "", "");
    public string Version { get; init; } = "";
    public string Runtime { get; init; } = "win-x64";
    /// <summary>Selected target runtimes. Runtime is retained for older local clients.</summary>
    public List<string> Runtimes { get; init; } = [];
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
public sealed record PublisherPreflightCheck(string Name, bool Passed, string Detail);
public sealed record DownloadChange(string Action, string FileName, string Detail);
public sealed record PublisherLogEntry(DateTimeOffset Timestamp, string Level, string Message);
public sealed record PublisherPreview(PublisherPaths Paths, string? GitRoot, string? Head, string? GitStatus, IReadOnlyList<string> Presets, IReadOnlyList<ExistingPackage> ExistingPackages, IReadOnlyList<OutputChange> Changes, IReadOnlyList<string> Warnings, IReadOnlyList<PublisherPreflightCheck> PreflightChecks);
public sealed class PublisherJob
{
    private const int MaximumLogEntries = 1_000;
    private readonly object logLock = new();
    private readonly List<PublisherLogEntry> logs = [];
    public Guid Id { get; init; } = Guid.NewGuid();
    public string State { get; set; } = "queued";
    public string Step { get; set; } = "等待本机发布锁";
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
    public string? Error { get; set; }
    public IReadOnlyList<string> AffectedFiles { get; set; } = [];
    public int ProgressCurrent { get; set; }
    public int ProgressTotal { get; set; }
    public bool CancellationRequested { get; set; }
    public IReadOnlyList<DownloadChange> DownloadChanges { get; set; } = [];
    public IReadOnlyList<PublisherLogEntry> Logs { get { lock (logLock) return logs.ToArray(); } }

    public void AddLog(string level, string message)
    {
        lock (logLock)
        {
            logs.Add(new PublisherLogEntry(DateTimeOffset.UtcNow, level, message));
            if (logs.Count > MaximumLogEntries) logs.RemoveRange(0, logs.Count - MaximumLogEntries);
        }
    }
}
