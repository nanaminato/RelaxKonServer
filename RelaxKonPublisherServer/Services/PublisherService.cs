using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RelaxKon_Publisher.Models;

namespace RelaxKon_Publisher.Services;

/// <summary>Local-only publishing coordinator. It never writes to the selected website checkout.</summary>
public sealed class PublisherService
{
    private static readonly HashSet<string> Runtimes = ["win-x64", "win-arm64", "linux-x64", "linux-arm64"];
    private static readonly SemaphoreSlim GenerationLock = new(1, 1);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly ConcurrentDictionary<Guid, PublisherJob> jobs = new();
    private readonly PublisherPathsOptions configuredPaths;
    private readonly string toolRoot;

    public PublisherService(IOptions<PublisherPathsOptions> configuredPaths, IWebHostEnvironment environment)
    {
        this.configuredPaths = configuredPaths.Value;
        toolRoot = environment.ContentRootPath;
    }

    public PublisherPaths GetDefaultPaths() => new(
        configuredPaths.RelaxKonOSPath,
        configuredPaths.RelaxKonServerPath,
        string.IsNullOrWhiteSpace(configuredPaths.ContentOutputPath)
            ? Path.Combine(toolRoot, "artifacts", "publisher-output", "Content")
            : configuredPaths.ContentOutputPath);

    public PublisherJob? GetJob(Guid id) => jobs.GetValueOrDefault(id);

    public async Task<PublisherPreview> PreviewAsync(PublisherPlanRequest request, CancellationToken cancellationToken)
    {
        var paths = await ValidateAsync(request, cancellationToken);
        var sourceContent = Path.Combine(paths.RelaxKonServerPath, "Content");
        var existing = ReadExistingPackages(sourceContent);
        var changes = PlannedChanges(request, paths, existing);
        var git = await ReadGitAsync(paths.RelaxKonServerPath, cancellationToken);
        var warnings = new List<string>();
        if (!request.BuildClient && !request.BuildServer) warnings.Add("尚未选择要构建的客户端或服务端包；只会生成选中的辅助文件。" );
        if (changes.Any(change => change.Action == "替换")) warnings.Add("存在同名输出，显式生成时将先备份后替换。" );
        return new PublisherPreview(paths, git.Root, git.Head, git.Status,
            ["win-x64 / Release", "win-arm64 / Release", "linux-x64 / Release", "linux-arm64 / Release"], existing, changes, warnings);
    }

    public PublisherJob StartGeneration(PublisherPlanRequest request)
    {
        var job = new PublisherJob();
        if (!jobs.TryAdd(job.Id, job)) throw new InvalidOperationException("无法创建发布任务。");
        _ = Task.Run(() => GenerateAsync(job, request));
        return job;
    }

    private async Task GenerateAsync(PublisherJob job, PublisherPlanRequest request)
    {
        string? stagingRoot = null;
        try
        {
            await GenerationLock.WaitAsync();
            job.State = "running";
            job.Step = "验证本机路径和发布计划";
            var paths = await ValidateAsync(request, CancellationToken.None);
            var sourceContent = Path.Combine(paths.RelaxKonServerPath, "Content");
            var stagingBase = Path.Combine(toolRoot, "artifacts", "staging", job.Id.ToString("N"));
            stagingRoot = Path.Combine(stagingBase, "Content");
            var stageDownloads = Path.Combine(stagingRoot, "Downloads");
            var stageDelivery = Path.Combine(stagingRoot, "ReleaseDelivery");
            Directory.CreateDirectory(stageDownloads);
            Directory.CreateDirectory(stageDelivery);

            job.Step = "复制当前独立输出到暂存区";
            CopyManagedOutput(paths.ContentOutputPath, stagingRoot);
            CopySelectedHistory(request, sourceContent, stageDelivery);

            var entries = ReadDownloadEntries(paths.ContentOutputPath);
            if (entries.Count == 0) entries = ReadDownloadEntries(sourceContent);
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
                job.Step = "复制发布安装器";
                CopyDirectory(Path.Combine(paths.RelaxKonOSPath, "deployment", "bootstrap"), Path.Combine(stageDelivery, "relaxkonos", "stable", "latest", "bootstrap"));
                affected.Add("ReleaseDelivery/relaxkonos/stable/latest/bootstrap");
            }
            if (request.BuildClient)
            {
                job.Step = "构建客户端包";
                var package = await BuildPackageAsync(paths.RelaxKonOSPath, stagingBase, stageDelivery, request, "client", CancellationToken.None);
                entries.RemoveAll(entry => entry.Runtime == request.Runtime && entry.PackageKind == "client");
                entries.Add(package.Entry);
                affected.AddRange(package.Files);
            }
            if (request.BuildServer)
            {
                job.Step = "构建服务端包";
                var package = await BuildPackageAsync(paths.RelaxKonOSPath, stagingBase, stageDelivery, request, "server", CancellationToken.None);
                entries.RemoveAll(entry => entry.Runtime == request.Runtime && entry.PackageKind == "server");
                entries.Add(package.Entry);
                affected.AddRange(package.Files);
            }

            job.Step = "生成下载清单并校验";
            var downloadsPath = Path.Combine(stageDownloads, "downloads.json");
            await File.WriteAllTextAsync(downloadsPath, JsonSerializer.Serialize(entries.OrderBy(entry => entry.Version).ThenBy(entry => entry.Runtime).ThenBy(entry => entry.PackageKind), Json));
            affected.Add("Downloads/downloads.json");
            await VerifyAsync(stagingRoot, entries, CancellationToken.None);
            var manifest = new
            {
                generatedAt = DateTimeOffset.UtcNow,
                status = "succeeded",
                paths,
                request.Version,
                request.Runtime,
                request.BuildClient,
                request.BuildServer,
                affectedFiles = affected,
                validation = "ZIP, SHA-256, relative paths and downloads.json verified"
            };
            await File.WriteAllTextAsync(Path.Combine(stagingRoot, "publisher-manifest.json"), JsonSerializer.Serialize(manifest, Json));
            affected.Add("publisher-manifest.json");

            job.Step = "以可恢复方式替换独立输出";
            CommitManagedOutput(stagingRoot, paths.ContentOutputPath, job.Id);
            job.AffectedFiles = affected;
            job.State = "succeeded";
            job.Step = "已生成；请检查独立输出后手动复制到官网 Content";
        }
        catch (Exception exception)
        {
            job.State = "failed";
            job.Error = exception.Message;
            job.Step = "任务失败；独立输出未被提交";
        }
        finally
        {
            job.FinishedAt = DateTimeOffset.UtcNow;
            if (GenerationLock.CurrentCount == 0) GenerationLock.Release();
            if (stagingRoot is not null)
            {
                var stagingBase = Directory.GetParent(stagingRoot)?.FullName;
                if (stagingBase is not null && Directory.Exists(stagingBase)) Directory.Delete(stagingBase, recursive: true);
            }
        }
    }

    private async Task<PublisherPaths> ValidateAsync(PublisherPlanRequest request, CancellationToken cancellationToken)
    {
        if (request.Paths is null) throw new InvalidOperationException("必须提供三个本机绝对路径。");
        if (string.IsNullOrWhiteSpace(request.Version) || !System.Text.RegularExpressions.Regex.IsMatch(request.Version, "^[0-9A-Za-z][0-9A-Za-z._-]{0,63}$"))
            throw new InvalidOperationException("版本号只能包含字母、数字、点、下划线和连字符。");
        if (!Runtimes.Contains(request.Runtime)) throw new InvalidOperationException("运行时必须是预定义的发布预设。");
        if (!request.BuildClient && !request.BuildServer && !request.IncludeInstallers) throw new InvalidOperationException("至少选择一项要生成的发布内容。");

        var paths = new PublisherPaths(NormalizeExistingDirectory(request.Paths.RelaxKonOSPath, "RelaxKonOS"), NormalizeExistingDirectory(request.Paths.RelaxKonServerPath, "RelaxKonServer"), NormalizeOutputDirectory(request.Paths.ContentOutputPath));
        if (!File.Exists(Path.Combine(paths.RelaxKonOSPath, "RelaxKonOS.sln"))) throw new InvalidOperationException("RelaxKonOS 路径必须包含 RelaxKonOS.sln。");
        var content = Path.Combine(paths.RelaxKonServerPath, "Content");
        if (!Directory.Exists(content)) throw new InvalidOperationException("RelaxKonServer 路径必须包含 Content 目录。");
        if (IsSameOrChild(paths.ContentOutputPath, content)) throw new InvalidOperationException("输出目录必须独立于所选 RelaxKonServer 的 Content，不能写入其内部。");
        var git = await ReadGitAsync(paths.RelaxKonServerPath, cancellationToken);
        if (git.Root is null) throw new InvalidOperationException("RelaxKonServer 的父目录必须可识别为 Git 工作树。");
        return paths;
    }

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
            foreach (var zip in Directory.EnumerateFiles(delivery, "*.zip", SearchOption.AllDirectories))
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
        foreach (var kind in new[] { request.BuildClient ? "client" : null, request.BuildServer ? "server" : null }.Where(kind => kind is not null))
        {
            var filename = $"RelaxKonOS-{request.Version}-{request.Runtime}-{kind}.zip";
            var relative = $"ReleaseDelivery/relaxkonos/stable/{request.Version}/{request.Runtime}/{kind}/{filename}";
            var output = Path.Combine(paths.ContentOutputPath, relative.Replace('/', Path.DirectorySeparatorChar));
            changes.Add(new OutputChange(relative, File.Exists(output) ? "替换" : "新增", $"构建最新 {kind} 包"));
        }
        changes.Add(new OutputChange("Downloads/downloads.json", File.Exists(Path.Combine(paths.ContentOutputPath, "Downloads", "downloads.json")) ? "替换" : "新增", "根据选择的可下载包重建"));
        foreach (var history in request.HistoricalPackages.Where(item => item.CopyToOutput))
            changes.Add(new OutputChange($"ReleaseDelivery/{history.RelativePath}", "复制", history.IsDownloadable ? "保留为公开历史包" : "仅复制，不在下载清单公开"));
        if (request.OnlyLatestDownloadable && existing.Any()) changes.Add(new OutputChange("Downloads/downloads.json", "隐藏", "新下载清单排除旧客户端和服务端"));
        return changes;
    }

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

    private async Task<BuiltPackage> BuildPackageAsync(string osRoot, string stagingBase, string deliveryRoot, PublisherPlanRequest request, string kind, CancellationToken cancellationToken)
    {
        var platform = request.Runtime.StartsWith("win-", StringComparison.Ordinal) ? "windows" : "linux";
        var extension = platform == "windows" ? ".exe" : "";
        var packageName = $"RelaxKonOS-{request.Version}-{request.Runtime}-{kind}";
        var buildRoot = Path.Combine(stagingBase, "build", packageName);
        var packageRoot = Path.Combine(buildRoot, packageName);
        Directory.CreateDirectory(packageRoot);
        if (kind == "client")
        {
            var target = Path.Combine(packageRoot, "payload", platform, "client");
            await DotnetPublishAsync(Path.Combine(osRoot, "Client", "RelaxKonOS.Client.Desktop", "RelaxKonOS.Client.Desktop.csproj"), target, request.Runtime, cancellationToken);
            EnsureExecutable(target, $"RelaxKonOS{extension}");
            await WriteManifestAsync(packageRoot, kind, request, platform, new Dictionary<string, string> { ["client"] = $"payload/{platform}/client/RelaxKonOS{extension}" });
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
                await DotnetPublishAsync(Path.Combine(osRoot, component.Item1.Replace('/', Path.DirectorySeparatorChar)), target, request.Runtime, cancellationToken);
                EnsureExecutable(target, component.Item4);
                payload[component.Item3] = $"payload/{platform}/{component.Item2}/{component.Item4}";
            }
            CopyDirectory(Path.Combine(osRoot, "deployment", "bootstrap"), Path.Combine(packageRoot, "deployment", "bootstrap"));
            CopyDirectory(Path.Combine(osRoot, "deployment", platform), Path.Combine(packageRoot, "deployment", platform));
            await WriteManifestAsync(packageRoot, kind, request, platform, payload);
        }

        var targetDirectory = Path.Combine(deliveryRoot, "relaxkonos", "stable", request.Version, request.Runtime, kind);
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
        var url = $"/relaxkonos/stable/{request.Version}/{request.Runtime}/{kind}/{Path.GetFileName(archive)}";
        if (request.IncludeDescriptors)
        {
            var descriptor = new ReleaseDescriptor(1, kind, request.Version, request.Runtime, $"https://downloads.relaxkon.com{url}", hash);
            var descriptorPath = archive + ".json";
            await File.WriteAllTextAsync(descriptorPath, JsonSerializer.Serialize(descriptor, Json), cancellationToken);
            files.Add(Path.GetRelativePath(deliveryRoot, descriptorPath).Replace('\\', '/'));
        }
        return new BuiltPackage(new DownloadEntry(platform, request.Runtime.Split('-')[1], request.Version, url, FormatSize(new FileInfo(archive).Length), hash, DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"), true, Path.GetFileName(archive), kind), files);
    }

    private static async Task DotnetPublishAsync(string project, string output, string runtime, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "publish", project, "--configuration", "Release", "--runtime", runtime, "--self-contained", "true", "--output", output }) info.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = info };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0) throw new InvalidOperationException($"dotnet publish 失败：{(await errorTask)[..Math.Min((await errorTask).Length, 2000)]}");
        _ = await outputTask;
    }

    private static void EnsureExecutable(string directory, string executable)
    {
        if (!File.Exists(Path.Combine(directory, executable))) throw new InvalidOperationException($"发布输出未包含 {executable}。");
    }

    private static Task WriteManifestAsync(string packageRoot, string kind, PublisherPlanRequest request, string platform, Dictionary<string, string> payload) =>
        File.WriteAllTextAsync(Path.Combine(packageRoot, "manifest.json"), JsonSerializer.Serialize(new { schemaVersion = 1, packageKind = kind, version = request.Version, runtime = request.Runtime, supportedSystems = platform == "windows" ? new[] { "windows" } : new[] { "debian-12", "ubuntu-22.04", "ubuntu-24.04", "ubuntu-26.04" }, payload = new Dictionary<string, object> { [platform] = payload } }, Json));

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
        return Path.Combine(delivery, relative[prefix.Length..]);
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
        public string Runtime => (Platform == "windows" ? "win" : Platform) + "-" + Architecture;
    }
    private sealed record ReleaseDescriptor(int SchemaVersion, string PackageKind, string Version, string Runtime, string Url, string Sha256);
    private sealed record BuiltPackage(DownloadEntry Entry, IReadOnlyList<string> Files);
}
