using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using RelaxKonOS.ModulePackaging;
using RelaxKon_Publisher.Models;

namespace RelaxKon_Publisher.Services;

internal sealed record ModuleReleaseConfig(string ModuleId, string Name, string Project, string EntryAssembly,
    string EntryType, string Runtime, string[] Rids, string HostApiMinimum, string HostApiMaximumExclusive,
    string[]? Capabilities = null, ModulePackageDependency[]? Dependencies = null,
    ModulePackageCapability[]? Privileges = null, ModulePackageUi? Ui = null);

internal sealed record ModuleBuildTarget(ModuleReleaseConfig Module, string Rid)
{
    public string ArchiveName(string version) => $"{Module.ModuleId}-{version}-{Rid}.rkmodule";
}

public sealed class ModulePublishingOptions
{
    public string SourceId { get; init; } = "";
    public string PublisherId { get; init; } = "";
    public string Purpose { get; init; } = "official";
    public string ArtifactOrigin { get; init; } = "";
    public string KeyId { get; init; } = "";
    public string PrivateKeyPath { get; init; } = "";
    public int ValiditySeconds { get; init; } = 604800;
    public long MinimumSequence { get; init; } = 1;
}

internal sealed class ModulePublishing(ModulePublishingOptions options)
{
    internal const string OutputDirectory = "ModuleDelivery";
    internal const string CatalogName = "catalog.json";
    private static readonly string[] SupportedRids = ["win-x64", "win-arm64", "linux-x64", "linux-arm64"];

    internal static bool IsIdentifier(string? value) => value is not null &&
        Regex.IsMatch(value, @"^[a-z][a-z0-9.-]{0,95}\z", RegexOptions.CultureInvariant);

    internal static IReadOnlyList<ModuleReleaseConfig> Discover(string root)
    {
        var modulesRoot = Path.Combine(root, "ServerModules");
        if (!Directory.Exists(modulesRoot)) return [];
        var modules = new Dictionary<string, ModuleReleaseConfig>(StringComparer.Ordinal);
        foreach (var directory in new DirectoryInfo(modulesRoot).EnumerateDirectories().OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) continue;
            var path = Path.Combine(directory.FullName, "module-release.json");
            if (!File.Exists(path)) continue;
            var config = ModulePackageJson.Read<ModuleReleaseConfig>(File.ReadAllBytes(path));
            if (!IsIdentifier(config.ModuleId) || string.IsNullOrWhiteSpace(config.Name)
                || string.IsNullOrWhiteSpace(config.Project) || Path.GetFileName(config.Project) != config.Project
                || !config.Project.EndsWith(".csproj", StringComparison.Ordinal)
                || !File.Exists(Path.Combine(directory.FullName, config.Project))
                || string.IsNullOrWhiteSpace(config.EntryAssembly) || string.IsNullOrWhiteSpace(config.EntryType)
                || config.Runtime is not ("module-host" or "in-process")
                || config.Rids is not { Length: > 0 } || config.Rids.Any(rid => !SupportedRids.Contains(rid))
                || config.Rids.Distinct(StringComparer.Ordinal).Count() != config.Rids.Length
                || !modules.TryAdd(config.ModuleId, config with { Project = Path.Combine(directory.FullName, config.Project) }))
                throw new InvalidOperationException($"模块发布配置无效或模块 ID 重复：{path}");
        }
        return modules.Values.OrderBy(module => module.ModuleId, StringComparer.Ordinal).ToArray();
    }

    internal IReadOnlyList<ModuleBuildTarget> Resolve(PublisherPlanRequest request, string root)
    {
        if (request.Modules.Count == 0) return [];
        if (!Version.TryParse(request.Version, out var version) || version.Build < 0 || version.Revision != -1
            || version.ToString(3) != request.Version)
            throw new InvalidOperationException("模块版本必须为三段数字（例如 1.2.3）。");
        var modules = Discover(root).ToDictionary(module => module.ModuleId, StringComparer.Ordinal);
        var targets = new List<ModuleBuildTarget>();
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var selection in request.Modules)
        {
            if (selection is null || !IsIdentifier(selection.ModuleId) || !selected.Add(selection.ModuleId)
                || !modules.TryGetValue(selection.ModuleId, out var module)
                || selection.Rids is not { Count: > 0 } || selection.Rids.Any(rid => !module.Rids.Contains(rid))
                || selection.Rids.Distinct(StringComparer.Ordinal).Count() != selection.Rids.Count)
                throw new InvalidOperationException($"模块或目标平台无效或重复：{selection?.ModuleId}");
            targets.AddRange(selection.Rids.Select(rid => new ModuleBuildTarget(module, rid)));
        }
        return targets;
    }

    internal string ValidateSettings()
    {
        if (!IsIdentifier(options.SourceId) || !IsIdentifier(options.PublisherId) || !IsIdentifier(options.KeyId)
            || options.Purpose is not ("official" or "third-party")
            || !Uri.TryCreate(options.ArtifactOrigin, UriKind.Absolute, out var origin) || origin.Scheme != "https"
            || origin.AbsoluteUri != origin.GetLeftPart(UriPartial.Authority) + "/" || origin.UserInfo.Length != 0
            || origin.AbsoluteUri != options.ArtifactOrigin || options.MinimumSequence <= 0
            || options.ValiditySeconds is <= 0 or > 2678400
            || !Path.IsPathFullyQualified(options.PrivateKeyPath) || !File.Exists(options.PrivateKeyPath))
            throw new InvalidOperationException("请在本机 ModulePublishing 配置中填写来源、HTTPS 根地址、有效期和签名密钥。");
        using var key = ECDsa.Create();
        try
        {
            key.ImportFromPem(File.ReadAllText(options.PrivateKeyPath));
            var parameters = key.ExportParameters(true);
            CryptographicOperations.ZeroMemory(parameters.D!);
            if (parameters.Curve.Oid.Value != "1.2.840.10045.3.1.7")
                throw new CryptographicException();
            return Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
        }
        catch (Exception error) when (error is ArgumentException or CryptographicException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("无法读取模块签名私钥，请检查本机 ECDSA P-256 私钥配置。");
        }
    }

    internal async Task<IReadOnlyList<string>> BuildAsync(IReadOnlyList<ModuleBuildTarget> targets,
        string version, string commit, string stage, string output, string work,
        Func<ModuleBuildTarget, string, Task> publish, Action<ModuleBuildTarget> completed, CancellationToken cancellationToken)
    {
        var publicKey = ValidateSettings();
        var oldPath = Path.Combine(output, CatalogName);
        var old = File.Exists(oldPath) ? await ReadPreviousAsync(oldPath, publicKey, cancellationToken) : null;
        Directory.CreateDirectory(stage);
        var releases = old?.Releases.ToList() ?? [];
        CheckNewTargets(targets, version, output, old);

        var affected = new List<string>();
        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var module = target.Module;
            var publishDir = Path.Combine(work, module.ModuleId, target.Rid);
            await publish(target, publishDir);
            var identity = $"{module.ModuleId}/{version}/{target.Rid}";
            var releaseId = "release-" + Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)));
            var release = await ModulePackageBuilder.BuildArchiveAsync(new ModulePackageBuildOptions(
                publishDir, stage, module.ModuleId, version, releaseId, options.PublisherId, options.Purpose,
                target.Rid, module.EntryAssembly, module.EntryType, "git-" + commit, commit,
                options.SourceId, 1, options.ArtifactOrigin, options.PrivateKeyPath, options.KeyId, options.ValiditySeconds,
                module.HostApiMinimum, module.HostApiMaximumExclusive, module.Capabilities, module.Dependencies,
                module.Runtime, module.Privileges, module.Ui ?? new(module.Name, "", "", null, "UNLICENSED")), cancellationToken);
            releases.Add(release);
            affected.Add($"{OutputDirectory}/{target.ArchiveName(version)}");
            completed(target);
        }

        var now = DateTimeOffset.UtcNow;
        var catalog = new ModulePackageCatalog(1, options.SourceId,
            Math.Max(options.MinimumSequence, checked((old?.Sequence ?? 0) + 1)), now,
            now.AddSeconds(options.ValiditySeconds), releases.OrderBy(ArchiveName, StringComparer.Ordinal).ToArray(),
            old?.RevokedReleaseIds ?? [], old?.RevokedDigests ?? []);
        var (envelope, signedKey) = await ModulePackageBuilder.SignCatalogAsync(catalog, options.KeyId, options.PrivateKeyPath, cancellationToken);
        if (signedKey != publicKey) throw new InvalidOperationException("构建期间模块签名密钥发生变化。");
        var policy = Policy(catalog, publicKey);
        var context = new ModulePackageContext("publisher", "", "1.0.0", false);
        var history = ModulePackageTrustHistory.Empty(policy, context);
        ModulePackageVerifier.VerifyCatalog(envelope, policy, context, history, now);
        if (catalog.Releases.Select(ArchiveName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != catalog.Releases.Length)
            throw new InvalidOperationException("模块目录包含同名归档。");
        foreach (var release in catalog.Releases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = ArchiveName(release);
            if (release.DownloadUri != options.ArtifactOrigin + name)
                throw new InvalidOperationException($"模块归档地址与配置的分发路径不一致：{name}");
            if (catalog.RevokedReleaseIds.Contains(release.Manifest.ReleaseId) || catalog.RevokedDigests.Contains(release.ArchiveSha256))
                continue;
            var archivePath = Path.Combine(stage, name);
            if (!File.Exists(archivePath)) File.Copy(Path.Combine(output, name), archivePath);
            await using var archive = File.OpenRead(archivePath);
            await ModulePackageVerifier.VerifyCandidateAsync(archive, envelope, policy, context with { Rid = release.Manifest.Rid },
                history, release.Manifest.ReleaseId, now, cancellationToken);
        }
        await File.WriteAllBytesAsync(Path.Combine(stage, CatalogName), envelope, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(stage, "public-key.spki"), publicKey, cancellationToken);
        affected.Add($"{OutputDirectory}/{CatalogName}");
        affected.Add($"{OutputDirectory}/public-key.spki");
        return affected;
    }

    internal async Task PreflightAsync(IReadOnlyList<ModuleBuildTarget> targets, string version, string output, CancellationToken cancellationToken)
    {
        var publicKey = ValidateSettings();
        var path = Path.Combine(output, CatalogName);
        var old = File.Exists(path) ? await ReadPreviousAsync(path, publicKey, cancellationToken) : null;
        CheckNewTargets(targets, version, output, old);
    }

    private static void CheckNewTargets(IReadOnlyList<ModuleBuildTarget> targets, string version, string output, ModulePackageCatalog? old)
    {
        foreach (var target in targets)
        {
            var name = target.ArchiveName(version);
            if (old?.Releases.Any(release => ArchiveName(release) == name) == true || File.Exists(Path.Combine(output, name)))
                throw new InvalidOperationException($"模块包 {name} 已发布。请使用新版本。");
        }
    }

    private async Task<ModulePackageCatalog> ReadPreviousAsync(string path, string publicKey, CancellationToken cancellationToken)
    {
        if (new FileInfo(path).Length > 6 * 1024 * 1024) throw new InvalidOperationException("现有模块目录超过大小限制。");
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var envelope = ModulePackageJson.Read<SignedModulePackageCatalog>(bytes, 6 * 1024 * 1024);
        var previous = ModulePackageJson.Read<ModulePackageCatalog>(Convert.FromBase64String(envelope.Payload));
        var policy = Policy(previous, publicKey) with { MaximumValiditySeconds = 2678400 };
        var context = new ModulePackageContext("publisher", "", "1.0.0", false);
        // Validate the historical signature at its issue time so an expired catalog can be renewed.
        var now = DateTimeOffset.UtcNow;
        return ModulePackageVerifier.VerifyCatalog(bytes, policy, context, ModulePackageTrustHistory.Empty(policy, context),
            previous.IssuedAt <= now ? previous.IssuedAt : now).Catalog;
    }

    private ModulePackageTrustPolicy Policy(ModulePackageCatalog catalog, string publicKey) =>
        new(1, options.SourceId, 1, true, options.PublisherId, options.Purpose, options.ArtifactOrigin,
            catalog.Releases.Select(release => release.Manifest.ModuleId).Distinct(StringComparer.Ordinal).ToArray(),
            new Dictionary<string, string> { [options.KeyId] = publicKey }, options.ValiditySeconds);

    private static string ArchiveName(ModulePackageRelease release) =>
        $"{release.Manifest.ModuleId}-{release.Manifest.Version}-{release.Manifest.Rid}.rkmodule";
}
