using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RelaxKon_Publisher.Hubs;
using RelaxKon_Publisher.Models;

namespace RelaxKon_Publisher.Services;

/// <summary>Local-only publishing coordinator. It never writes to the selected website checkout.</summary>
public sealed class PublisherService
{
    private static readonly HashSet<string> Runtimes = ["win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-arm64"];
    private static readonly SemaphoreSlim GenerationLock = new(1, 1);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly ConcurrentDictionary<Guid, PublisherJob> jobs = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> jobCancellation = new();
    private readonly PublisherPathsOptions configuredPaths;
    private readonly AndroidImportOptions androidImport;
    private readonly string toolRoot;
    private readonly IHubContext<PublisherHub> publisherHub;
    private readonly ILogger<PublisherService> logger;

    public PublisherService(IOptions<PublisherPathsOptions> configuredPaths, IOptions<AndroidImportOptions> androidImport, IWebHostEnvironment environment, IHubContext<PublisherHub> publisherHub, ILogger<PublisherService> logger)
    {
        this.configuredPaths = configuredPaths.Value;
        this.androidImport = androidImport.Value;
        toolRoot = environment.ContentRootPath;
        this.publisherHub = publisherHub;
        this.logger = logger;
    }

    public PublisherPaths GetDefaultPaths() => new(
        configuredPaths.RelaxKonOSPath,
        configuredPaths.RelaxKonServerPath,
        string.IsNullOrWhiteSpace(configuredPaths.ContentOutputPath)
            ? Path.Combine(toolRoot, "artifacts", "publisher-output", "Content")
            : configuredPaths.ContentOutputPath);

    public PublisherJob? GetJob(Guid id) => jobs.GetValueOrDefault(id);

    /// <summary>Validates request fields synchronously so invalid plans never become background jobs.</summary>
    public void ValidateRequest(PublisherPlanRequest request)
    {
        if (request.Paths is null) throw new InvalidOperationException("必须提供三个本机绝对路径。");
        if (string.IsNullOrWhiteSpace(request.Version) || !System.Text.RegularExpressions.Regex.IsMatch(request.Version, "^[0-9A-Za-z][0-9A-Za-z._-]{0,63}$"))
            throw new InvalidOperationException("版本号不能为空，且只能包含字母、数字、点、下划线和连字符。");
        if (SelectedRuntimes(request).Any(runtime => !Runtimes.Contains(runtime))) throw new InvalidOperationException("运行时必须是预定义的发布预设。");
        if (request.BuildClient && SelectedClientRuntimes(request).Count == 0) throw new InvalidOperationException("至少选择一个客户端运行时。");
        if (request.BuildServer && SelectedServerRuntimes(request).Count == 0) throw new InvalidOperationException("至少选择一个服务端运行时。");
        if (request.BuildServer && SelectedServerRuntimes(request).Any(runtime => runtime.StartsWith("osx-", StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("macOS 目前仅支持客户端包，不能选择服务端目标运行时。");
        if (!request.BuildClient && !request.BuildServer && !request.IncludeInstallers && !request.ImportAndroidApk && !request.ImportAndroidAab)
            throw new InvalidOperationException("至少选择一项要生成的发布内容。");
    }

    public PublisherJob? CancelJob(Guid id)
    {
        if (!jobs.TryGetValue(id, out var job)) return null;
        if (job.State is "succeeded" or "failed" or "cancelled") return job;
        job.CancellationRequested = true;
        SetStep(job, "正在请求取消当前构建…", "warning");
        if (jobCancellation.TryGetValue(id, out var cancellation))
            try { cancellation.Cancel(); }
            catch (ObjectDisposedException) { }
        return job;
    }

    public async Task<PublisherPreview> PreviewAsync(PublisherPlanRequest request, CancellationToken cancellationToken, Func<PublisherLogEntry, Task>? report = null)
    {
        async Task ReportAsync(string level, string message)
        {
            WriteBackendLog(level, $"[预览] {message}");
            if (report is not null) await report(new PublisherLogEntry(DateTimeOffset.UtcNow, level, message));
        }

        await ReportAsync("info", "开始执行构建前检查。");
        var paths = await ValidateAsync(request, cancellationToken);
        await ReportAsync("success", "本机路径与发布计划格式校验通过。");
        var sourceContent = Path.Combine(paths.RelaxKonServerPath, "Content");
        await ReportAsync("info", "正在扫描旧发布包和 downloads.json。");
        var existing = ReadExistingPackages(sourceContent);
        var changes = PlannedChanges(request, paths, existing);
        var checks = CreatePreflightChecks(request, paths);
        if (checks.Any(check => !check.Passed)) throw new InvalidOperationException(string.Join("；", checks.Where(check => !check.Passed).Select(check => check.Detail)));
        await ReportAsync("success", $"发现 {existing.Count} 个现有发布包，计划产生 {changes.Count} 项输出变化。");
        await ReportAsync("info", "正在读取来源仓库 Git 状态。");
        var git = await ReadGitAsync(paths.RelaxKonServerPath, cancellationToken);
        var warnings = new List<string>();
        if (!request.BuildClient && !request.BuildServer) warnings.Add("尚未选择要构建的客户端或服务端包；只会生成选中的辅助文件。" );
        if (changes.Any(change => change.Action == "替换")) warnings.Add("存在同名输出，显式生成时将先备份后替换。" );
        var preview = new PublisherPreview(paths, git.Root, git.Head, git.Status,
            ["客户端：win-x64、win-arm64、linux-x64、linux-arm64、osx-arm64 / Release", "服务端：win-x64、win-arm64、linux-x64、linux-arm64 / Release"], existing, changes, warnings, checks);
        await ReportAsync("success", "预览完成；没有修改任何目录。");
        return preview;
    }

    public void StartPreview(PublisherPlanRequest request, string connectionId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var preview = await PreviewAsync(request, CancellationToken.None,
                    entry => publisherHub.Clients.Client(connectionId).SendAsync("previewLog", entry));
                await publisherHub.Clients.Client(connectionId).SendAsync("previewCompleted", preview);
            }
            catch (Exception exception)
            {
                WriteBackendLog("error", $"[预览] 预览失败：{exception.Message}");
                await publisherHub.Clients.Client(connectionId).SendAsync("previewFailed", exception.Message);
            }
        });
    }

    public PublisherJob StartGeneration(PublisherPlanRequest request)
    {
        ValidateRequest(request);
        var job = new PublisherJob();
        Log(job, "info", "发布任务已创建，等待本机发布锁。");
        if (!jobs.TryAdd(job.Id, job)) throw new InvalidOperationException("无法创建发布任务。");
        var cancellation = new CancellationTokenSource();
        if (!jobCancellation.TryAdd(job.Id, cancellation)) throw new InvalidOperationException("无法创建发布取消令牌。");
        _ = Task.Run(() => GenerateAsync(job, request, cancellation.Token));
        return job;
    }

    private async Task GenerateAsync(PublisherJob job, PublisherPlanRequest request, CancellationToken cancellationToken)
    {
        string? stagingRoot = null;
        var lockAcquired = false;
        try
        {
            await GenerationLock.WaitAsync(cancellationToken);
            lockAcquired = true;
            job.State = "running";
            SetStep(job, "验证本机路径和发布计划");
            var paths = await ValidateAsync(request, cancellationToken);
            var checks = CreatePreflightChecks(request, paths);
            if (checks.Any(check => !check.Passed)) throw new InvalidOperationException(string.Join("；", checks.Where(check => !check.Passed).Select(check => check.Detail)));
            var clientRuntimes = request.BuildClient ? SelectedClientRuntimes(request) : [];
            var serverRuntimes = request.BuildServer ? SelectedServerRuntimes(request) : [];
            var runtimes = clientRuntimes.Concat(serverRuntimes).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Log(job, "info", $"构建前检查通过；客户端：{string.Join("、", clientRuntimes.DefaultIfEmpty("未选择"))}；服务端：{string.Join("、", serverRuntimes.DefaultIfEmpty("未选择"))}。");
            job.ProgressTotal = 3 + clientRuntimes.Count + serverRuntimes.Count + (request.ImportAndroidApk ? 1 : 0) + (request.ImportAndroidAab ? 1 : 0);
            job.ProgressCurrent = 1;
            var sourceContent = Path.Combine(paths.RelaxKonServerPath, "Content");
            var stagingBase = Path.Combine(toolRoot, "artifacts", "staging", job.Id.ToString("N"));
            stagingRoot = Path.Combine(stagingBase, "Content");
            var stageDownloads = Path.Combine(stagingRoot, "Downloads");
            var stageDelivery = Path.Combine(stagingRoot, "ReleaseDelivery");
            Directory.CreateDirectory(stageDownloads);
            Directory.CreateDirectory(stageDelivery);

            SetStep(job, "复制当前独立输出到暂存区");
            CopyManagedOutput(paths.ContentOutputPath, stagingRoot);
            CopySelectedHistory(request, sourceContent, stageDelivery);

            var entries = ReadDownloadEntries(paths.ContentOutputPath);
            if (entries.Count == 0) entries = ReadDownloadEntries(sourceContent);
            var entriesBefore = entries.ToList();
            entries.RemoveAll(entry => !File.Exists(DeliveryPathForUrl(stageDelivery, entry.Url)));
            foreach (var decision in request.HistoricalPackages)
            {
                var fileName = Path.GetFileName(decision.RelativePath);
                for (var index = 0; index < entries.Count; index++)
                    if (string.Equals(entries[index].FileName, fileName, StringComparison.OrdinalIgnoreCase))
                        entries[index] = entries[index] with { IsAvailable = decision.CopyToOutput && decision.IsDownloadable };
            }
            if (request.OnlyLatestDownloadable)
                entries.RemoveAll(entry => entry.PackageKind is "client" or "server");

            var affected = new List<string>();
            if (request.IncludeInstallers)
            {
                SetStep(job, "复制发布安装器");
                CopyDirectory(Path.Combine(paths.RelaxKonOSPath, "deployment", "bootstrap"), Path.Combine(stageDelivery, "relaxkonos", "stable", "latest", "bootstrap"));
                affected.Add("ReleaseDelivery/relaxkonos/stable/latest/bootstrap");
            }
            foreach (var runtime in runtimes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (clientRuntimes.Contains(runtime, StringComparer.OrdinalIgnoreCase))
                {
                    SetStep(job, $"构建客户端包（{runtime}）");
                    if (RemoveStagedPackage(stageDelivery, request.Version, runtime, "client"))
                        Log(job, "info", $"已删除暂存区中的现有客户端包，将重新生成：{runtime}。");
                    entries.RemoveAll(entry => entry.Runtime == runtime && entry.PackageKind == "client");
                    var package = await BuildPackageAsync(job, paths.RelaxKonOSPath, stagingBase, stageDelivery, request, runtime, "client", cancellationToken);
                    entries.Add(package.Entry);
                    affected.AddRange(package.Files);
                    job.ProgressCurrent++;
                    PushUpdate(job);
                }
                if (serverRuntimes.Contains(runtime, StringComparer.OrdinalIgnoreCase))
                {
                    SetStep(job, $"构建服务端包（{runtime}）");
                    if (RemoveStagedPackage(stageDelivery, request.Version, runtime, "server"))
                        Log(job, "info", $"已删除暂存区中的现有服务端包，将重新生成：{runtime}。");
                    entries.RemoveAll(entry => entry.Runtime == runtime && entry.PackageKind == "server");
                    var package = await BuildPackageAsync(job, paths.RelaxKonOSPath, stagingBase, stageDelivery, request, runtime, "server", cancellationToken);
                    var latestDescriptor = await WriteLatestServerDescriptorAsync(stageDelivery, package.Entry, cancellationToken);
                    entries.Add(package.Entry);
                    affected.AddRange(package.Files);
                    affected.Add(latestDescriptor);
                    job.ProgressCurrent++;
                    PushUpdate(job);
                }
            }

            if (request.ImportAndroidApk || request.ImportAndroidAab)
            {
                var import = GetAndroidImportSettings(request.ImportAndroidApk, request.ImportAndroidAab);
                var androidReleaseManifest = await ReadAndVerifyAndroidReleaseManifestAsync(import, request.Version, cancellationToken);
                if (request.ImportAndroidApk)
                {
                    SetStep(job, "导入并验证已签名 Android APK");
                    entries.RemoveAll(entry => entry.Runtime == "android-universal" && entry.PackageKind == "client");
                    var imported = await ImportAndroidArtifactAsync(job, import, androidReleaseManifest, stageDelivery, request, ".apk", cancellationToken);
                    entries.Add(imported.Entry!);
                    affected.AddRange(imported.Files);
                    job.ProgressCurrent++;
                    PushUpdate(job);
                }
                if (request.ImportAndroidAab)
                {
                    SetStep(job, "归档并验证已签名 Android App Bundle");
                    var imported = await ImportAndroidArtifactAsync(job, import, androidReleaseManifest, stageDelivery, request, ".aab", cancellationToken);
                    affected.AddRange(imported.Files);
                    job.ProgressCurrent++;
                    PushUpdate(job);
                }
            }

            SetStep(job, "生成下载清单并校验");
            var downloadsPath = Path.Combine(stageDownloads, "downloads.json");
            await File.WriteAllTextAsync(downloadsPath, JsonSerializer.Serialize(entries.OrderBy(entry => entry.Version).ThenBy(entry => entry.Runtime).ThenBy(entry => entry.PackageKind), Json));
            affected.Add("Downloads/downloads.json");
            await VerifyAsync(stagingRoot, entries, cancellationToken);
            job.ProgressCurrent++;
            job.DownloadChanges = CompareDownloadEntries(entriesBefore, entries);
            var manifest = new
            {
                generatedAt = DateTimeOffset.UtcNow,
                status = "succeeded",
                paths,
                request.Version,
                runtimes,
                clientRuntimes,
                serverRuntimes,
                request.BuildClient,
                request.BuildServer,
                affectedFiles = affected,
                validation = "ZIP, SHA-256, relative paths and downloads.json verified"
            };
            await File.WriteAllTextAsync(Path.Combine(stagingRoot, "publisher-manifest.json"), JsonSerializer.Serialize(manifest, Json));
            affected.Add("publisher-manifest.json");

            SetStep(job, "以可恢复方式替换独立输出");
            CommitManagedOutput(stagingRoot, paths.ContentOutputPath, job.Id);
            job.ProgressCurrent = job.ProgressTotal;
            job.AffectedFiles = affected;
            job.State = "succeeded";
            job.Step = "已生成；请检查独立输出后手动复制到官网 Content";
            Log(job, "success", "发布成功，已将经校验的暂存输出提交到独立输出目录。");
        }
        catch (OperationCanceledException)
        {
            job.State = "cancelled";
            job.Step = "任务已取消；独立输出未被提交";
            Log(job, "warning", "任务已取消，暂存输出将被清理，独立输出未被提交。");
        }
        catch (Exception exception)
        {
            job.State = "failed";
            job.Error = exception.Message;
            job.Step = "任务失败；独立输出未被提交";
            Log(job, "error", $"任务失败：{exception.Message}");
        }
        finally
        {
            job.FinishedAt = DateTimeOffset.UtcNow;
            if (lockAcquired) GenerationLock.Release();
            if (jobCancellation.TryRemove(job.Id, out var cancellation)) cancellation.Dispose();
            if (stagingRoot is not null)
            {
                var stagingBase = Directory.GetParent(stagingRoot)?.FullName;
                if (stagingBase is not null && Directory.Exists(stagingBase)) Directory.Delete(stagingBase, recursive: true);
            }
            PushUpdate(job);
        }
    }

    private async Task<PublisherPaths> ValidateAsync(PublisherPlanRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var paths = new PublisherPaths(NormalizeExistingDirectory(request.Paths.RelaxKonOSPath, "RelaxKonOS"), NormalizeExistingDirectory(request.Paths.RelaxKonServerPath, "RelaxKonServer"), NormalizeOutputDirectory(request.Paths.ContentOutputPath));
        if (!File.Exists(Path.Combine(paths.RelaxKonOSPath, "RelaxKonOS.sln"))) throw new InvalidOperationException("RelaxKonOS 路径必须包含 RelaxKonOS.sln。");
        var content = Path.Combine(paths.RelaxKonServerPath, "Content");
        if (!Directory.Exists(content)) throw new InvalidOperationException("RelaxKonServer 路径必须包含 Content 目录。");
        if (IsSameOrChild(paths.ContentOutputPath, content)) throw new InvalidOperationException("输出目录必须独立于所选 RelaxKonServer 的 Content，不能写入其内部。");
        var git = await ReadGitAsync(paths.RelaxKonServerPath, cancellationToken);
        if (git.Root is null) throw new InvalidOperationException("RelaxKonServer 的父目录必须可识别为 Git 工作树。");
        return paths;
    }

    private static IReadOnlyList<string> SelectedRuntimes(PublisherPlanRequest request)
    {
        var selected = new List<string>();
        if (request.BuildClient) selected.AddRange(SelectedClientRuntimes(request));
        if (request.BuildServer) selected.AddRange(SelectedServerRuntimes(request));
        if (selected.Count == 0) selected.AddRange(LegacyRuntimes(request));
        selected = selected.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (selected.Count == 0) throw new InvalidOperationException("至少选择一个运行时。");
        return selected;
    }

    private static IReadOnlyList<string> SelectedClientRuntimes(PublisherPlanRequest request) =>
        HasSeparateRuntimeSelections(request)
            ? request.ClientRuntimes.Where(runtime => !string.IsNullOrWhiteSpace(runtime)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : LegacyRuntimes(request);

    private static IReadOnlyList<string> SelectedServerRuntimes(PublisherPlanRequest request) =>
        HasSeparateRuntimeSelections(request)
            ? request.ServerRuntimes.Where(runtime => !string.IsNullOrWhiteSpace(runtime)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : LegacyRuntimes(request);

    private static bool HasSeparateRuntimeSelections(PublisherPlanRequest request) => request.ClientRuntimes.Count > 0 || request.ServerRuntimes.Count > 0;

    private static IReadOnlyList<string> LegacyRuntimes(PublisherPlanRequest request)
    {
        var selected = request.Runtimes.Where(runtime => !string.IsNullOrWhiteSpace(runtime)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (selected.Count == 0 && !string.IsNullOrWhiteSpace(request.Runtime)) selected.Add(request.Runtime);
        return selected;
    }

    private IReadOnlyList<PublisherPreflightCheck> CreatePreflightChecks(PublisherPlanRequest request, PublisherPaths paths)
    {
        var checks = new List<PublisherPreflightCheck>
        {
            new("RelaxKonOS 解决方案", File.Exists(Path.Combine(paths.RelaxKonOSPath, "RelaxKonOS.sln")), "RelaxKonOS.sln 可用于构建"),
            new("独立输出目录", !IsSameOrChild(paths.ContentOutputPath, Path.Combine(paths.RelaxKonServerPath, "Content")), "输出目录与官网 Content 保持隔离"),
            new("客户端目标运行时", !request.BuildClient || SelectedClientRuntimes(request).All(Runtimes.Contains), request.BuildClient ? $"已选择：{string.Join("、", SelectedClientRuntimes(request))}" : "未构建客户端"),
            new("服务端目标运行时", !request.BuildServer || SelectedServerRuntimes(request).All(runtime => Runtimes.Contains(runtime) && !runtime.StartsWith("osx-", StringComparison.Ordinal)), request.BuildServer ? $"已选择：{string.Join("、", SelectedServerRuntimes(request))}" : "未构建服务端")
        };
        if (request.BuildClient)
        {
            var project = Path.Combine(paths.RelaxKonOSPath, "Client", "RelaxKonOS.Client.Desktop", "RelaxKonOS.Client.Desktop.csproj");
            checks.Add(new PublisherPreflightCheck("客户端项目", File.Exists(project), File.Exists(project) ? "客户端项目文件已找到" : $"找不到客户端项目：{project}"));
        }
        if (request.BuildServer)
        {
            foreach (var relative in new[] { "RelaxKonOS.Server/RelaxKonOS.Server.csproj", "RelaxKonOS.Guardian.Agent/RelaxKonOS.Guardian.Agent.csproj", "RelaxKonOS.PrivilegedHelper/RelaxKonOS.PrivilegedHelper.csproj", "deployment/bootstrap" })
            {
                var path = Path.Combine(paths.RelaxKonOSPath, relative.Replace('/', Path.DirectorySeparatorChar));
                var exists = relative.EndsWith(".csproj", StringComparison.Ordinal) ? File.Exists(path) : Directory.Exists(path);
                checks.Add(new PublisherPreflightCheck($"服务端输入：{relative}", exists, exists ? "构建输入已找到" : $"找不到构建输入：{path}"));
            }
            foreach (var platform in SelectedServerRuntimes(request).Select(RuntimePlatform).Distinct())
            {
                var path = Path.Combine(paths.RelaxKonOSPath, "deployment", platform);
                checks.Add(new PublisherPreflightCheck($"部署文件：{platform}", Directory.Exists(path), Directory.Exists(path) ? "部署文件已找到" : $"找不到部署文件：{path}"));
            }
        }
        if (request.ImportAndroidApk || request.ImportAndroidAab)
        {
            var importDirectory = NormalizeConfiguredDirectory(androidImport.ArtifactDirectory);
            var certificate = NormalizeCertificateFingerprint(androidImport.ExpectedCertificateSha256);
            checks.Add(new PublisherPreflightCheck("Android 导入目录", importDirectory is not null && Directory.Exists(importDirectory), importDirectory is null ? "必须在 appsettings.Local.json 配置绝对 ArtifactDirectory" : "已配置本机受控导入目录"));
            checks.Add(new PublisherPreflightCheck("Android 应用 ID", !string.IsNullOrWhiteSpace(androidImport.ExpectedPackageName), string.IsNullOrWhiteSpace(androidImport.ExpectedPackageName) ? "必须配置 ExpectedPackageName" : androidImport.ExpectedPackageName));
            checks.Add(new PublisherPreflightCheck("Android 发布证书", certificate is not null, certificate is null ? "ExpectedCertificateSha256 必须是 64 位 SHA-256 十六进制指纹" : "已配置受信任证书指纹"));
            if (request.ImportAndroidApk)
            {
                checks.Add(new PublisherPreflightCheck("APK 验签工具", IsExistingFile(androidImport.ApkSignerPath), "apksigner 必须在本机可用"));
                checks.Add(new PublisherPreflightCheck("APK 元数据工具", IsExistingFile(androidImport.Aapt2Path), "aapt2 必须在本机可用"));
            }
            if (request.ImportAndroidAab)
            {
                checks.Add(new PublisherPreflightCheck("AAB 验签工具", IsExistingFile(androidImport.JarSignerPath), "jarsigner 必须在本机可用"));
                checks.Add(new PublisherPreflightCheck("AAB 证书工具", IsExistingFile(androidImport.KeytoolPath), "keytool 必须在本机可用"));
            }
            if (importDirectory is not null)
            {
                var releaseManifest = AndroidManifestPath(importDirectory, request.Version);
                checks.Add(new PublisherPreflightCheck("Android 发布清单", File.Exists(releaseManifest), File.Exists(releaseManifest) ? "已找到由签名构建生成的发布清单" : $"缺少 {Path.GetFileName(releaseManifest)}"));
                foreach (var extension in new[] { (request.ImportAndroidApk, ".apk"), (request.ImportAndroidAab, ".aab") }.Where(item => item.Item1).Select(item => item.Item2))
                {
                    var artifact = AndroidArtifactPath(importDirectory, request.Version, extension);
                    checks.Add(new PublisherPreflightCheck($"Android {extension.TrimStart('.').ToUpperInvariant()}", File.Exists(artifact), File.Exists(artifact) ? "已找到待导入的签名产物" : $"缺少 {Path.GetFileName(artifact)}"));
                }
            }
        }
        return checks;
    }

    private static string? NormalizeConfiguredDirectory(string? value) =>
        string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value) ? null : Path.TrimEndingDirectorySeparator(Path.GetFullPath(value));

    private static bool IsExistingFile(string? value) => !string.IsNullOrWhiteSpace(value) && Path.IsPathFullyQualified(value) && File.Exists(value);

    private static string NormalizeExistingDirectory(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value)) throw new InvalidOperationException($"{label} 必须是本机绝对路径。");
        var path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(value));
        if (!Directory.Exists(path)) throw new InvalidOperationException($"{label} 路径不存在：{path}");
        return path;
    }

    private static string NormalizeOutputDirectory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value)) throw new InvalidOperationException("ContentOutputPath 必须是本机绝对路径。");
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(value));
    }

    private static bool IsSameOrChild(string candidate, string parent) =>
        string.Equals(candidate, parent, StringComparison.OrdinalIgnoreCase) || candidate.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static async Task<(string? Root, string? Head, string? Status)> ReadGitAsync(string serverPath, CancellationToken cancellationToken)
    {
        var directory = Directory.GetParent(serverPath)?.FullName ?? serverPath;
        var root = await RunGitAsync(directory, ["rev-parse", "--show-toplevel"], cancellationToken);
        if (root is null) return (null, null, null);
        return (root, await RunGitAsync(root, ["rev-parse", "--short", "HEAD"], cancellationToken), await RunGitAsync(root, ["status", "--short"], cancellationToken));
    }

    private static async Task<string?> RunGitAsync(string directory, IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo("git") { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0 ? output.Trim() : null;
    }

    private IReadOnlyList<ExistingPackage> ReadExistingPackages(string content)
    {
        var entries = ReadDownloadEntries(content);
        var result = entries.Select(entry => new ExistingPackage(
            FindReleaseRelativePath(content, entry.FileName) ?? $"Downloads/{entry.FileName}", entry.FileName, 0, entry.Version, entry.Runtime, entry.PackageKind, entry.IsAvailable)).ToList();
        var known = result.Select(item => item.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var delivery = Path.Combine(content, "ReleaseDelivery");
        if (Directory.Exists(delivery))
            foreach (var zip in Directory.EnumerateFiles(delivery, "*.*", SearchOption.AllDirectories).Where(path => Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".apk", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".aab", StringComparison.OrdinalIgnoreCase)))
            {
                var name = Path.GetFileName(zip);
                if (known.Add(name)) result.Add(new ExistingPackage(Path.GetRelativePath(delivery, zip).Replace('\\', '/'), name, new FileInfo(zip).Length, null, null, null, false));
            }
        return result;
    }

    private static List<DownloadEntry> ReadDownloadEntries(string content)
    {
        var path = Path.Combine(content, "Downloads", "downloads.json");
        if (!File.Exists(path)) return [];
        try { return JsonSerializer.Deserialize<List<DownloadEntry>>(File.ReadAllText(path), Json) ?? []; }
        catch (JsonException exception) { throw new InvalidOperationException($"无法读取 downloads.json：{exception.Message}"); }
    }

    private static string? FindReleaseRelativePath(string content, string fileName)
    {
        var delivery = Path.Combine(content, "ReleaseDelivery");
        if (!Directory.Exists(delivery)) return null;
        var match = Directory.EnumerateFiles(delivery, fileName, SearchOption.AllDirectories).FirstOrDefault();
        return match is null ? null : Path.GetRelativePath(delivery, match).Replace('\\', '/');
    }

    private static IReadOnlyList<OutputChange> PlannedChanges(PublisherPlanRequest request, PublisherPaths paths, IReadOnlyList<ExistingPackage> existing)
    {
        var changes = new List<OutputChange>();
        foreach (var runtime in request.BuildClient ? SelectedClientRuntimes(request) : [])
        {
            AddPlannedPackageChange(changes, paths, request.Version, runtime, "client");
        }
        foreach (var runtime in request.BuildServer ? SelectedServerRuntimes(request) : []) AddPlannedPackageChange(changes, paths, request.Version, runtime, "server");
        if (request.ImportAndroidApk) AddPlannedAndroidArtifactChange(changes, paths, request.Version, ".apk", "导入经本机工具验证的签名 APK，并加入公开下载清单");
        if (request.ImportAndroidAab) AddPlannedAndroidArtifactChange(changes, paths, request.Version, ".aab", "归档经本机工具验证的签名 AAB；AAB 不加入公开下载清单");
        changes.Add(new OutputChange("Downloads/downloads.json", File.Exists(Path.Combine(paths.ContentOutputPath, "Downloads", "downloads.json")) ? "替换" : "新增", "根据选择的可下载包重建"));
        foreach (var history in request.HistoricalPackages.Where(item => item.CopyToOutput))
            changes.Add(new OutputChange($"ReleaseDelivery/{history.RelativePath}", "复制", history.IsDownloadable ? "保留为公开历史包" : "仅复制，不在下载清单公开"));
        if (request.OnlyLatestDownloadable && existing.Any()) changes.Add(new OutputChange("Downloads/downloads.json", "隐藏", "新下载清单排除旧客户端和服务端"));
        return changes;
    }

    private static void AddPlannedPackageChange(List<OutputChange> changes, PublisherPaths paths, string version, string runtime, string kind)
    {
        var filename = $"RelaxKonOS-{version}-{runtime}-{kind}.zip";
        var relative = $"ReleaseDelivery/relaxkonos/stable/{version}/{runtime}/{kind}/{filename}";
        var output = Path.Combine(paths.ContentOutputPath, relative.Replace('/', Path.DirectorySeparatorChar));
        changes.Add(new OutputChange(relative, File.Exists(output) ? "替换" : "新增", $"构建最新 {kind} 包（{runtime}）"));
    }

    private static void AddPlannedAndroidArtifactChange(List<OutputChange> changes, PublisherPaths paths, string version, string extension, string reason)
    {
        var filename = AndroidArtifactFileName(version, extension);
        var relative = $"ReleaseDelivery/relaxkonos/stable/{version}/android-universal/client/{filename}";
        var output = Path.Combine(paths.ContentOutputPath, relative.Replace('/', Path.DirectorySeparatorChar));
        changes.Add(new OutputChange(relative, File.Exists(output) ? "替换" : "新增", reason));
    }

    private static string RuntimePlatform(string runtime) => runtime switch
    {
        var value when value.StartsWith("win-", StringComparison.OrdinalIgnoreCase) => "windows",
        var value when value.StartsWith("linux-", StringComparison.OrdinalIgnoreCase) => "linux",
        var value when value.StartsWith("osx-", StringComparison.OrdinalIgnoreCase) => "macos",
        _ => throw new InvalidOperationException($"不支持的运行时：{runtime}")
    };

    private static string[] SupportedSystems(string platform) => platform switch
    {
        "windows" => ["windows"],
        "macos" => ["macos"],
        "linux" => ["debian-12", "ubuntu-22.04", "ubuntu-24.04", "ubuntu-26.04"],
        _ => throw new InvalidOperationException($"不支持的平台：{platform}")
    };

    private static void CopyManagedOutput(string output, string staging)
    {
        foreach (var name in new[] { "Downloads", "ReleaseDelivery" })
        {
            var source = Path.Combine(output, name);
            if (Directory.Exists(source)) CopyDirectory(source, Path.Combine(staging, name));
        }
    }

    private static void CopySelectedHistory(PublisherPlanRequest request, string sourceContent, string stageDelivery)
    {
        var sourceDelivery = Path.Combine(sourceContent, "ReleaseDelivery");
        foreach (var item in request.HistoricalPackages.Where(item => item.CopyToOutput))
        {
            var relative = item.RelativePath.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(relative) || relative.Split(Path.DirectorySeparatorChar).Any(part => part is "" or "." or "..")) throw new InvalidOperationException("历史包路径无效。");
            var source = Path.GetFullPath(Path.Combine(sourceDelivery, relative));
            if (!IsSameOrChild(source, sourceDelivery) || !File.Exists(source)) throw new InvalidOperationException($"历史包不存在：{item.RelativePath}");
            var target = Path.Combine(stageDelivery, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target, overwrite: true);
        }
    }

    /// <summary>
    /// Removes only an already-staged package that this task is about to rebuild.
    /// The committed output and selected website checkout remain untouched unless
    /// the whole replacement succeeds.
    /// </summary>
    private static bool RemoveStagedPackage(string deliveryRoot, string version, string runtime, string kind)
    {
        var packageDirectory = Path.Combine(deliveryRoot, "relaxkonos", "stable", version, runtime, kind);
        if (!Directory.Exists(packageDirectory)) return false;
        Directory.Delete(packageDirectory, recursive: true);
        return true;
    }

    private async Task<BuiltPackage> BuildPackageAsync(PublisherJob job, string osRoot, string stagingBase, string deliveryRoot, PublisherPlanRequest request, string runtime, string kind, CancellationToken cancellationToken)
    {
        var platform = RuntimePlatform(runtime);
        var extension = platform == "windows" ? ".exe" : "";
        var packageName = $"RelaxKonOS-{request.Version}-{runtime}-{kind}";
        var buildRoot = Path.Combine(stagingBase, "build", packageName);
        var packageRoot = Path.Combine(buildRoot, packageName);
        Directory.CreateDirectory(packageRoot);
        if (kind == "client")
        {
            var target = Path.Combine(packageRoot, "payload", platform, "client");
            await DotnetPublishAsync(job, Path.Combine(osRoot, "Client", "RelaxKonOS.Client.Desktop", "RelaxKonOS.Client.Desktop.csproj"), target, runtime, cancellationToken);
            EnsureExecutable(target, $"RelaxKonOS{extension}");
            await WriteManifestAsync(packageRoot, kind, request.Version, runtime, platform, new Dictionary<string, string> { ["client"] = $"payload/{platform}/client/RelaxKonOS{extension}" });
            if (platform == "macos") await WriteMacOsReadmeAsync(packageRoot, cancellationToken);
        }
        else
        {
            var components = new[]
            {
                ("RelaxKonOS.Server/RelaxKonOS.Server.csproj", "server", "server", $"RelaxKonOS.Server{extension}"),
                ("RelaxKonOS.Guardian.Agent/RelaxKonOS.Guardian.Agent.csproj", "guardian", "guardian", $"RelaxKonOS.Guardian.Agent{extension}"),
                ("RelaxKonOS.PrivilegedHelper/RelaxKonOS.PrivilegedHelper.csproj", "privileged-helper", "privilegedHelper", $"RelaxKonOS.PrivilegedHelper{extension}")
            };
            var payload = new Dictionary<string, string>();
            foreach (var component in components)
            {
                var target = Path.Combine(packageRoot, "payload", platform, component.Item2);
                await DotnetPublishAsync(job, Path.Combine(osRoot, component.Item1.Replace('/', Path.DirectorySeparatorChar)), target, runtime, cancellationToken);
                EnsureExecutable(target, component.Item4);
                payload[component.Item3] = $"payload/{platform}/{component.Item2}/{component.Item4}";
            }
            CopyDirectory(Path.Combine(osRoot, "deployment", "bootstrap"), Path.Combine(packageRoot, "deployment", "bootstrap"));
            CopyDirectory(Path.Combine(osRoot, "deployment", platform), Path.Combine(packageRoot, "deployment", platform));
            await WriteManifestAsync(packageRoot, kind, request.Version, runtime, platform, payload);
        }

        var targetDirectory = Path.Combine(deliveryRoot, "relaxkonos", "stable", request.Version, runtime, kind);
        Directory.CreateDirectory(targetDirectory);
        var archive = Path.Combine(targetDirectory, packageName + ".zip");
        ZipFile.CreateFromDirectory(packageRoot, archive, CompressionLevel.Optimal, includeBaseDirectory: false);
        using (var zip = ZipFile.OpenRead(archive)) if (zip.Entries.Count == 0) throw new InvalidOperationException("生成的 ZIP 为空。");
        var hash = await ComputeHashAsync(archive, cancellationToken);
        var files = new List<string> { Path.GetRelativePath(deliveryRoot, archive).Replace('\\', '/') };
        if (request.IncludeChecksums)
        {
            var checksum = archive + ".sha256";
            await File.WriteAllTextAsync(checksum, $"{hash}  {Path.GetFileName(archive)}\n", cancellationToken);
            files.Add(Path.GetRelativePath(deliveryRoot, checksum).Replace('\\', '/'));
        }
        var url = $"/relaxkonos/stable/{request.Version}/{runtime}/{kind}/{Path.GetFileName(archive)}";
        if (request.IncludeDescriptors)
        {
            var descriptor = new ReleaseDescriptor(1, kind, request.Version, runtime, $"https://downloads.relaxkon.com{url}", hash);
            var descriptorPath = archive + ".json";
            await File.WriteAllTextAsync(descriptorPath, JsonSerializer.Serialize(descriptor, Json), cancellationToken);
            files.Add(Path.GetRelativePath(deliveryRoot, descriptorPath).Replace('\\', '/'));
        }
        return new BuiltPackage(new DownloadEntry(platform, runtime.Split('-')[1], request.Version, url, FormatSize(new FileInfo(archive).Length), hash, DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"), true, Path.GetFileName(archive), kind), files);
    }

    private AndroidImportSettings GetAndroidImportSettings(bool requireApk, bool requireAab)
    {
        var directory = NormalizeConfiguredDirectory(androidImport.ArtifactDirectory);
        var certificate = NormalizeCertificateFingerprint(androidImport.ExpectedCertificateSha256);
        if (directory is null || !Directory.Exists(directory)) throw new InvalidOperationException("Android ArtifactDirectory 必须是存在的本机绝对路径。");
        if (string.IsNullOrWhiteSpace(androidImport.ExpectedPackageName)) throw new InvalidOperationException("必须配置 Android ExpectedPackageName。");
        if (certificate is null) throw new InvalidOperationException("Android ExpectedCertificateSha256 必须是 64 位 SHA-256 十六进制指纹。");
        if (requireApk && (!IsExistingFile(androidImport.ApkSignerPath) || !IsExistingFile(androidImport.Aapt2Path)))
            throw new InvalidOperationException("导入 APK 需要将 apksigner 和 aapt2 配置为本机绝对文件路径。");
        if (requireAab && (!IsExistingFile(androidImport.KeytoolPath) || !IsExistingFile(androidImport.JarSignerPath)))
            throw new InvalidOperationException("导入 AAB 需要将 keytool 和 jarsigner 配置为本机绝对文件路径。");
        return new AndroidImportSettings(directory, androidImport.ExpectedPackageName, certificate, androidImport.ApkSignerPath, androidImport.Aapt2Path, androidImport.KeytoolPath, androidImport.JarSignerPath);
    }

    private async Task<AndroidReleaseManifest> ReadAndVerifyAndroidReleaseManifestAsync(AndroidImportSettings import, string version, CancellationToken cancellationToken)
    {
        var path = AndroidManifestPath(import.ArtifactDirectory, version);
        if (!File.Exists(path)) throw new InvalidOperationException($"缺少 Android 发布清单：{Path.GetFileName(path)}");
        AndroidReleaseManifest? manifest;
        try
        {
            await using var stream = File.OpenRead(path);
            manifest = await JsonSerializer.DeserializeAsync<AndroidReleaseManifest>(stream, Json, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Android 发布清单不是有效 JSON：{exception.Message}");
        }
        if (manifest is null || manifest.SchemaVersion != 1 || manifest.Artifacts.Count == 0)
            throw new InvalidOperationException("Android 发布清单格式无效。");
        if (!string.Equals(manifest.PackageName, import.ExpectedPackageName, StringComparison.Ordinal))
            throw new InvalidOperationException($"Android 应用 ID 不匹配：期望 {import.ExpectedPackageName}，实际 {manifest.PackageName}。");
        if (!string.Equals(manifest.VersionName, version, StringComparison.Ordinal))
            throw new InvalidOperationException($"Android versionName 不匹配：发布计划为 {version}，发布清单为 {manifest.VersionName}。");
        if (!string.Equals(NormalizeCertificateFingerprint(manifest.CertificateSha256), import.ExpectedCertificateSha256, StringComparison.Ordinal))
            throw new InvalidOperationException("Android 发布清单的签名证书与本机受信任指纹不匹配。");
        return manifest;
    }

    private async Task<ImportedAndroidArtifact> ImportAndroidArtifactAsync(PublisherJob job, AndroidImportSettings import, AndroidReleaseManifest manifest, string deliveryRoot, PublisherPlanRequest request, string extension, CancellationToken cancellationToken)
    {
        var source = AndroidArtifactPath(import.ArtifactDirectory, request.Version, extension);
        if (!File.Exists(source)) throw new InvalidOperationException($"缺少 Android {extension}：{Path.GetFileName(source)}");
        EnsureReadableArchive(source);
        var hash = await ComputeHashAsync(source, cancellationToken);
        var declared = manifest.Artifacts.SingleOrDefault(item => string.Equals(item.FileName, Path.GetFileName(source), StringComparison.OrdinalIgnoreCase));
        if (declared is null || !string.Equals(NormalizeCertificateFingerprint(declared.Sha256), hash, StringComparison.Ordinal))
            throw new InvalidOperationException($"Android 发布清单中的 SHA-256 与 {Path.GetFileName(source)} 不匹配。");

        if (extension.Equals(".apk", StringComparison.OrdinalIgnoreCase))
        {
            var signature = await RunToolAsync(import.ApkSignerPath, ["verify", "--verbose", "--print-certs", source], cancellationToken);
            EnsureExpectedCertificate(signature, import.ExpectedCertificateSha256, "APK");
            var badging = await RunToolAsync(import.Aapt2Path, ["dump", "badging", source], cancellationToken);
            EnsureApkMetadata(badging, import.ExpectedPackageName, request.Version);
        }
        else
        {
            var verification = await RunToolAsync(import.JarSignerPath, ["-verify", source], cancellationToken);
            if (!verification.Contains("jar verified", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("AAB 签名验证未确认 jar verified。");
            var certificate = await RunToolAsync(import.KeytoolPath, ["-printcert", "-jarfile", source], cancellationToken);
            EnsureExpectedCertificate(certificate, import.ExpectedCertificateSha256, "AAB");
        }

        var targetDirectory = Path.Combine(deliveryRoot, "relaxkonos", "stable", request.Version, "android-universal", "client");
        Directory.CreateDirectory(targetDirectory);
        var target = Path.Combine(targetDirectory, Path.GetFileName(source));
        foreach (var path in new[] { target, target + ".sha256", target + ".json" })
            if (File.Exists(path)) File.Delete(path);
        File.Copy(source, target, overwrite: true);
        var files = new List<string> { Path.GetRelativePath(deliveryRoot, target).Replace('\\', '/') };
        var manifestSource = AndroidManifestPath(import.ArtifactDirectory, request.Version);
        var manifestTarget = Path.Combine(targetDirectory, Path.GetFileName(manifestSource));
        File.Copy(manifestSource, manifestTarget, overwrite: true);
        files.Add(Path.GetRelativePath(deliveryRoot, manifestTarget).Replace('\\', '/'));
        if (request.IncludeChecksums)
        {
            var checksum = target + ".sha256";
            await File.WriteAllTextAsync(checksum, $"{hash}  {Path.GetFileName(target)}\n", cancellationToken);
            files.Add(Path.GetRelativePath(deliveryRoot, checksum).Replace('\\', '/'));
        }
        var url = $"/relaxkonos/stable/{request.Version}/android-universal/client/{Path.GetFileName(target)}";
        if (request.IncludeDescriptors)
        {
            var descriptor = target + ".json";
            await File.WriteAllTextAsync(descriptor, JsonSerializer.Serialize(new ReleaseDescriptor(1, "client", request.Version, "android-universal", $"https://downloads.relaxkon.com{url}", hash), Json), cancellationToken);
            files.Add(Path.GetRelativePath(deliveryRoot, descriptor).Replace('\\', '/'));
        }
        Log(job, "success", $"已验证并导入 Android {extension.TrimStart('.').ToUpperInvariant()}：{Path.GetFileName(source)}。");
        var entry = extension.Equals(".apk", StringComparison.OrdinalIgnoreCase)
            ? new DownloadEntry("android", "universal", request.Version, url, FormatSize(new FileInfo(target).Length), hash, DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"), true, Path.GetFileName(target), "client")
            : null;
        return new ImportedAndroidArtifact(entry, files);
    }

    private static void EnsureReadableArchive(string path)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            if (archive.Entries.Count == 0) throw new InvalidOperationException($"Android 产物为空：{Path.GetFileName(path)}");
        }
        catch (InvalidDataException exception)
        {
            throw new InvalidOperationException($"Android 产物不是可读取的 APK/AAB ZIP：{Path.GetFileName(path)}（{exception.Message}）");
        }
    }

    private static void EnsureApkMetadata(string output, string expectedPackageName, string expectedVersion)
    {
        var match = Regex.Match(output, "package:\\s+name='(?<package>[^']+)'\\s+versionCode='[^']*'\\s+versionName='(?<version>[^']+)'", RegexOptions.CultureInvariant);
        if (!match.Success) throw new InvalidOperationException("无法从 aapt2 输出读取 APK 应用 ID 和版本。");
        if (!string.Equals(match.Groups["package"].Value, expectedPackageName, StringComparison.Ordinal) || !string.Equals(match.Groups["version"].Value, expectedVersion, StringComparison.Ordinal))
            throw new InvalidOperationException($"APK 元数据不匹配：期望 {expectedPackageName} {expectedVersion}。");
    }

    private static void EnsureExpectedCertificate(string output, string expectedFingerprint, string artifactKind)
    {
        var fingerprints = Regex.Matches(output, "SHA-?256(?:\\s+digest)?\\s*:\\s*(?<value>[0-9A-Fa-f:]{64,95})", RegexOptions.CultureInvariant)
            .Select(match => NormalizeCertificateFingerprint(match.Groups["value"].Value))
            .Where(value => value is not null)
            .Cast<string>();
        if (!fingerprints.Contains(expectedFingerprint, StringComparer.Ordinal))
            throw new InvalidOperationException($"{artifactKind} 签名证书与本机受信任指纹不匹配。");
    }

    private static string? NormalizeCertificateFingerprint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Replace(":", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();
        return Regex.IsMatch(normalized, "^[0-9a-f]{64}$", RegexOptions.CultureInvariant) ? normalized : null;
    }

    private static string AndroidArtifactFileName(string version, string extension) => $"RelaxKonOS-{version}-android-universal{extension}";
    private static string AndroidArtifactPath(string directory, string version, string extension) => Path.Combine(directory, AndroidArtifactFileName(version, extension));
    private static string AndroidManifestPath(string directory, string version) => Path.Combine(directory, $"RelaxKonOS-{version}-android-universal.release.json");

    private static async Task<string> RunToolAsync(string executable, IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = info };
        process.Start();
        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = (await standardOutput) + "\n" + (await standardError);
        if (process.ExitCode != 0) throw new InvalidOperationException($"Android 验证工具 {Path.GetFileName(executable)} 失败（退出代码 {process.ExitCode}）：{output.Trim()}");
        return output;
    }

    /// <summary>Updates the catalog consumed by the online server installers.</summary>
    private static async Task<string> WriteLatestServerDescriptorAsync(string deliveryRoot, DownloadEntry package, CancellationToken cancellationToken)
    {
        if (!string.Equals(package.PackageKind, "server", StringComparison.Ordinal))
            throw new ArgumentException("最新安装器描述只能引用服务端包。", nameof(package));

        var latest = Path.Combine(deliveryRoot, "relaxkonos", "stable", "latest");
        Directory.CreateDirectory(latest);
        var descriptorPath = Path.Combine(latest, package.Runtime + ".json");
        var descriptor = new ReleaseDescriptor(1, "server", package.Version, package.Runtime, $"https://downloads.relaxkon.com{package.Url}", package.Checksum);
        await File.WriteAllTextAsync(descriptorPath, JsonSerializer.Serialize(descriptor, Json), cancellationToken);
        return Path.GetRelativePath(deliveryRoot, descriptorPath).Replace('\\', '/');
    }

    private async Task DotnetPublishAsync(PublisherJob job, string project, string output, string runtime, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in new[] { "publish", project, "--configuration", "Release", "--runtime", runtime, "--self-contained", "true", "--output", output }) info.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = info };
        Log(job, "info", $"执行：dotnet publish {Path.GetFileName(project)} · Release · {runtime} · self-contained");
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data)) Log(job, "info", eventArgs.Data);
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data)) Log(job, "error", eventArgs.Data);
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        });
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0) throw new InvalidOperationException($"dotnet publish 失败，退出代码：{process.ExitCode}。请查看执行日志。");
        Log(job, "success", $"dotnet publish 完成：{Path.GetFileName(project)}（{runtime}）。");
    }

    private void SetStep(PublisherJob job, string step, string level = "info")
    {
        job.Step = step;
        Log(job, level, step);
    }

    private void Log(PublisherJob job, string level, string message)
    {
        job.AddLog(level, message);
        WriteBackendLog(level, $"[任务 {job.Id:N}] {message}");
        PushUpdate(job);
    }

    private void WriteBackendLog(string level, string message)
    {
        switch (level)
        {
            case "error": logger.LogError("{Message}", message); break;
            case "warning": logger.LogWarning("{Message}", message); break;
            case "success": logger.LogInformation("✓ {Message}", message); break;
            default: logger.LogInformation("{Message}", message); break;
        }
    }

    private void PushUpdate(PublisherJob job) =>
        _ = publisherHub.Clients.Group(PublisherHub.JobGroup(job.Id)).SendAsync("jobUpdated", job);

    private static IReadOnlyList<DownloadChange> CompareDownloadEntries(IEnumerable<DownloadEntry> before, IEnumerable<DownloadEntry> after)
    {
        var oldEntries = before.ToDictionary(entry => entry.FileName, StringComparer.OrdinalIgnoreCase);
        var newEntries = after.ToDictionary(entry => entry.FileName, StringComparer.OrdinalIgnoreCase);
        var changes = new List<DownloadChange>();
        foreach (var (fileName, entry) in newEntries.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!oldEntries.TryGetValue(fileName, out var oldEntry))
                changes.Add(new DownloadChange("新增", fileName, $"{entry.Version} · {entry.Runtime} · {(entry.IsAvailable ? "公开可下载" : "已隐藏")}"));
            else if (oldEntry != entry)
            {
                var availability = oldEntry.IsAvailable == entry.IsAvailable ? "元数据已更新" : entry.IsAvailable ? "改为公开可下载" : "改为隐藏";
                changes.Add(new DownloadChange("更新", fileName, availability));
            }
        }
        foreach (var (fileName, entry) in oldEntries.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            if (!newEntries.ContainsKey(fileName)) changes.Add(new DownloadChange("移除", fileName, $"从下载清单移除（原版本 {entry.Version}）"));
        return changes;
    }

    private static void EnsureExecutable(string directory, string executable)
    {
        if (!File.Exists(Path.Combine(directory, executable))) throw new InvalidOperationException($"发布输出未包含 {executable}。");
    }

    private static Task WriteManifestAsync(string packageRoot, string kind, string version, string runtime, string platform, Dictionary<string, string> payload) =>
        File.WriteAllTextAsync(Path.Combine(packageRoot, "manifest.json"), JsonSerializer.Serialize(new { schemaVersion = 1, packageKind = kind, version, runtime, supportedSystems = SupportedSystems(platform), payload = new Dictionary<string, object> { [platform] = payload } }, Json));

    private static Task WriteMacOsReadmeAsync(string packageRoot, CancellationToken cancellationToken) =>
        File.WriteAllTextAsync(Path.Combine(packageRoot, "README-macOS.txt"), """
            RelaxKonOS macOS portable client

            1. Extract this ZIP in Finder or with `unzip`.
            2. In Terminal, run:
               chmod +x payload/macos/client/RelaxKonOS
               ./payload/macos/client/RelaxKonOS

            This portable build is not code-signed or notarized by the local publisher.
            For distribution outside a controlled environment, sign and notarize it in the Apple release workflow.
            """, Encoding.UTF8, cancellationToken);

    private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
    }

    private static string FormatSize(long bytes) => $"{bytes / 1024d / 1024d:0.0} MB";

    private static async Task VerifyAsync(string stagingRoot, IReadOnlyList<DownloadEntry> entries, CancellationToken cancellationToken)
    {
        var delivery = Path.Combine(stagingRoot, "ReleaseDelivery");
        foreach (var entry in entries.Where(entry => entry.IsAvailable))
        {
            var file = DeliveryPathForUrl(delivery, entry.Url);
            if (!File.Exists(file)) throw new InvalidOperationException($"downloads.json 引用了不存在的文件：{entry.FileName}");
            if (!string.Equals(Path.GetFileName(file), entry.FileName, StringComparison.Ordinal)) throw new InvalidOperationException("downloads.json 文件名不匹配。");
            if (Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using var zip = ZipFile.OpenRead(file);
                if (zip.Entries.Count == 0) throw new InvalidOperationException($"ZIP 不可读取或为空：{entry.FileName}");
            }
            if (!string.Equals(await ComputeHashAsync(file, cancellationToken), entry.Checksum, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"SHA-256 不匹配：{entry.FileName}");
            if (!string.Equals(FormatSize(new FileInfo(file).Length), entry.Size, StringComparison.Ordinal))
                throw new InvalidOperationException($"文件大小不匹配：{entry.FileName}");
        }
    }

    private static string DeliveryPathForUrl(string delivery, string url)
    {
        var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var prefix = $"relaxkonos{Path.DirectorySeparatorChar}stable{Path.DirectorySeparatorChar}";
        if (!relative.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return Path.Combine(delivery, "__invalid_url__");
        // Release packages are staged below ReleaseDelivery/relaxkonos/stable/….
        // Keep that full URL-relative path; stripping the prefix makes validation
        // look under ReleaseDelivery/{version}/… and reject packages that exist.
        return Path.Combine(delivery, relative);
    }

    private void CommitManagedOutput(string stagingRoot, string output, Guid jobId)
    {
        var backup = Path.Combine(toolRoot, "artifacts", "recovery", $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{jobId:N}");
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(backup);
        var moved = new List<string>();
        try
        {
            foreach (var name in new[] { "Downloads", "ReleaseDelivery" })
            {
                var destination = Path.Combine(output, name);
                if (Directory.Exists(destination)) { Directory.Move(destination, Path.Combine(backup, name)); moved.Add(name); }
                Directory.Move(Path.Combine(stagingRoot, name), destination);
            }
            var manifest = Path.Combine(output, "publisher-manifest.json");
            if (File.Exists(manifest)) File.Move(manifest, Path.Combine(backup, "publisher-manifest.json"), overwrite: true);
            File.Move(Path.Combine(stagingRoot, "publisher-manifest.json"), manifest, overwrite: true);
        }
        catch
        {
            foreach (var name in new[] { "Downloads", "ReleaseDelivery" })
            {
                var destination = Path.Combine(output, name);
                if (Directory.Exists(destination)) Directory.Delete(destination, recursive: true);
                if (moved.Contains(name) && Directory.Exists(Path.Combine(backup, name))) Directory.Move(Path.Combine(backup, name), destination);
            }
            throw;
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source)) throw new InvalidOperationException($"缺少构建所需目录：{source}");
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private sealed record DownloadEntry(string Platform, string Architecture, string Version, string Url, string Size, string Checksum, string ReleaseDate, bool IsAvailable, string FileName, string PackageKind)
    {
        public string Runtime => (Platform == "windows" ? "win" : Platform == "macos" ? "osx" : Platform) + "-" + Architecture;
    }
    private sealed record ReleaseDescriptor(int SchemaVersion, string PackageKind, string Version, string Runtime, string Url, string Sha256);
    private sealed record BuiltPackage(DownloadEntry Entry, IReadOnlyList<string> Files);
    private sealed record AndroidImportSettings(string ArtifactDirectory, string ExpectedPackageName, string ExpectedCertificateSha256, string ApkSignerPath, string Aapt2Path, string KeytoolPath, string JarSignerPath);
    private sealed record AndroidReleaseManifest(int SchemaVersion, string PackageName, string VersionName, long VersionCode, string CertificateSha256, List<AndroidReleaseManifestArtifact> Artifacts);
    private sealed record AndroidReleaseManifestArtifact(string FileName, string Sha256);
    private sealed record ImportedAndroidArtifact(DownloadEntry? Entry, IReadOnlyList<string> Files);
}
