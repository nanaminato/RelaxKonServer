using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RelaxKonOS.ModulePackaging;
using RelaxKon_Publisher.Hubs;
using RelaxKon_Publisher.Models;
using RelaxKon_Publisher.Services;

var root = Path.Combine(Path.GetTempPath(), "publisher-packaging-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var source = Path.Combine(root, "source");
    Write("deployment/bootstrap/install-relaxkonos.sh", "#!/bin/bash\r\nexit 0\r\n");
    Write("deployment/linux/install-relaxkonos-services.sh", "#!/bin/bash\r\nexit 0\r\n");
    Write("deployment/windows/Install-RelaxKonOSServices.ps1", "Write-Host OK\r\n");
    Write("deployment/user/relaxkon", "#!/bin/bash\r\nexit 0\r\n");
    Write("deployment/bootstrap/uninstall-relaxkonos.sh", "#!/bin/bash\r\nexit 0\r\n");
    Write("deployment/verify-release-inventory.py", "# verifier\n");
    foreach (var platform in new[] { "linux", "windows" })
    {
        var package = Path.Combine(root, platform);
        Directory.CreateDirectory(package);
        Invoke("CopyServerDeploymentFiles", source, package, platform);
        var verifier = Path.Combine(package, "deployment", "verify-release-inventory.py");
        Check(File.Exists(verifier) == (platform == "linux"), "Linux verifier must be packaged");
        if (platform == "linux")
        {
            var launcher = Path.Combine(package, "deployment", "user", "relaxkon");
            Check(File.Exists(launcher) && !File.ReadAllText(launcher).Contains('\r'), "User launcher must be packaged with LF");
        }
        var script = Path.Combine(package, "deployment", "bootstrap", "install-relaxkonos.sh");
        Check(File.ReadAllText(script).Contains('\r') == (platform == "windows"), "Linux scripts must use LF");
        await Manifest(package, "server", platform == "linux" ? "linux-x64" : "win-x64", platform);
        VerifyInventory(package);
        // Regenerating must not list the previous manifest itself.
        await Manifest(package, "server", platform == "linux" ? "linux-x64" : "win-x64", platform);
        VerifyInventory(package);
    }
    var mac = Path.Combine(root, "macos");
    Directory.CreateDirectory(mac);
    File.WriteAllText(Path.Combine(mac, "RelaxKonOS"), "client");
    await (Task)Invoke("WriteMacOsReadmeAsync", mac, CancellationToken.None)!;
    await Manifest(mac, "client", "osx-arm64", "macos");
    VerifyInventory(mac);
    using (var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(mac, "manifest.json"))))
        Check(manifest.RootElement.GetProperty("files").EnumerateArray().Any(item => item.GetProperty("path").GetString() == "README-macOS.txt"), "macOS README must be inventoried");
    File.Delete(Path.Combine(source, "deployment", "user", "relaxkon"));
    try
    {
        Invoke("CopyServerDeploymentFiles", source, Path.Combine(root, "missing-launcher"), "linux");
        throw new Exception("Missing user launcher must fail packaging");
    }
    catch (TargetInvocationException error) when (error.InnerException is FileNotFoundException missing && missing.Message.Contains("deployment/user/relaxkon")) { }
    var linuxPackage = Path.Combine(root, "linux");
    File.Delete(Path.Combine(linuxPackage, "deployment", "user", "relaxkon"));
    try
    {
        await Manifest(linuxPackage, "server", "linux-x64", "linux");
        throw new Exception("Incomplete staged package must fail manifest generation");
    }
    catch (FileNotFoundException) { }
    Write("deployment/user/relaxkon", "#!/bin/bash\nexit 0\n");
    File.Delete(Path.Combine(source, "deployment", "verify-release-inventory.py"));
    try
    {
        Invoke("CopyServerDeploymentFiles", source, Path.Combine(root, "missing"), "linux");
        throw new Exception("Missing verifier must fail packaging");
    }
    catch (TargetInvocationException error) when (error.InnerException is FileNotFoundException) { }
    Console.WriteLine("PASS: user launcher, missing launcher and incomplete staging rejection, Linux verifier, LF conversion, complete inventories, repeat generation, macOS README, missing verifier rejection.");
    if (args is ["--module-acceptance", var osPath, var serverPath, var artifacts])
        await VerifyModuleBatchAsync(Path.GetFullPath(osPath), Path.GetFullPath(serverPath), Path.GetFullPath(artifacts));

    void Write(string relative, string content)
    {
        var path = Path.Combine(source, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
finally { Directory.Delete(root, recursive: true); }

static async Task VerifyModuleBatchAsync(string osPath, string serverPath, string artifacts)
{
    var run = Path.Combine(artifacts, "module-acceptance-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(run);
    var keyPath = Path.Combine(run, "test-key.pem");
    var output = Path.Combine(run, "output");
    using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    await File.WriteAllTextAsync(keyPath, key.ExportPkcs8PrivateKeyPem());
    try
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSignalR();
        using var provider = services.BuildServiceProvider();
        var host = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(
            new Microsoft.AspNetCore.Builder.WebApplicationOptions { ContentRootPath = run });
        var publisher = new PublisherService(Options.Create(new PublisherPathsOptions()), Options.Create(new AndroidImportOptions()),
            Options.Create(new ModulePublishingOptions
            {
                SourceId = "publisher-test", PublisherId = "publisher-test", KeyId = "test-key",
                PrivateKeyPath = keyPath, ArtifactOrigin = "https://modules.example/"
            }), host.Environment, provider.GetRequiredService<IHubContext<PublisherHub>>(), NullLogger<PublisherService>.Instance);
        var discovered = publisher.DiscoverModules(osPath).Single(module => module.ModuleId == "cloud-storage");
        Check(discovered.Rids.Contains("win-x64") && discovered.Rids.Contains("linux-x64"), "CloudStorage RID discovery");
        await PublishAsync(Plan("0.0.1", ["win-x64", "linux-x64"]), 2);
        var catalogPath = Path.Combine(output, "ModuleDelivery", "catalog.json");
        var firstEnvelope = await File.ReadAllBytesAsync(catalogPath);
        var context = new ModulePackageContext("publisher-test", "win-x64", "1.0.0", false);
        var policy = new ModulePackageTrustPolicy(1, "publisher-test", 1, true, "publisher-test", "official",
            "https://modules.example/", ["cloud-storage"],
            new Dictionary<string, string> { ["test-key"] = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()) }, 604800);
        var first = ModulePackageVerifier.VerifyCatalog(firstEnvelope, policy, context,
            ModulePackageTrustHistory.Empty(policy, context), DateTimeOffset.UtcNow);
        Check(first.Catalog.Sequence == 1 && first.Catalog.Releases.Select(release => release.Manifest.Rid)
            .Order(StringComparer.Ordinal).SequenceEqual(["linux-x64", "win-x64"]), "one signed directory contains both RIDs");
        try
        {
            await publisher.PreviewAsync(Plan("0.0.1", ["win-x64"]), CancellationToken.None);
            throw new Exception("Expected immutable package rejection");
        }
        catch (InvalidOperationException error) when (error.Message.Contains("cloud-storage-0.0.1-win-x64.rkmodule")) { }
        var unchangedEnvelope = await File.ReadAllBytesAsync(catalogPath);
        Check(firstEnvelope.SequenceEqual(unchangedEnvelope), "rejected preview leaves catalog intact");
        await PublishAsync(Plan("0.0.2", ["win-x64"]), 1);
        var second = ModulePackageVerifier.VerifyCatalog(await File.ReadAllBytesAsync(catalogPath), policy, context,
            first.NextHistory, DateTimeOffset.UtcNow);
        Check(second.Catalog.Sequence == 2 && second.Catalog.Releases.Length == 3, "next catalog preserves both previous releases");
        Check(!Directory.Exists(Path.Combine(output, "Downloads")), "module-only task leaves website download output alone");
        Console.WriteLine($"PASS: CloudStorage discovery, two RID archives, unified signature, immutable version, and catalog history (sequence 1 → 2). Output: {output}");

        PublisherPlanRequest Plan(string version, string[] rids) => new()
        {
            Paths = new(osPath, serverPath, output), Version = version, BuildClient = false, BuildServer = false,
            Modules = [new("cloud-storage", rids)]
        };

        async Task PublishAsync(PublisherPlanRequest plan, int archiveCount)
        {
            var preview = await publisher.PreviewAsync(plan, CancellationToken.None);
            Check(preview.Changes.Count(change => change.RelativePath.EndsWith(".rkmodule")) == archiveCount, "preview lists each module archive");
            var job = publisher.StartGeneration(plan);
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try
            {
                while (job.FinishedAt is null) await Task.Delay(100, timeout.Token);
            }
            catch (OperationCanceledException) { publisher.CancelJob(job.Id); throw; }
            Check(job.State == "succeeded", string.Join(Environment.NewLine, job.Logs.Select(entry => entry.Message)));
            Check(job.AffectedFiles.Count(file => file.EndsWith(".rkmodule")) == archiveCount, "job publishes each planned archive");
        }
    }
    finally { File.Delete(keyPath); }
}

static object? Invoke(string name, params object[] arguments) =>
    typeof(PublisherService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, arguments);

static Task Manifest(string root, string kind, string runtime, string platform) =>
    (Task)Invoke("WriteManifestAsync", root, kind, "0.2.0-test", runtime, platform, new Dictionary<string, string>())!;

static void Check(bool valid, string message)
{
    if (!valid) throw new Exception(message);
}

static void VerifyInventory(string root)
{
    using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "manifest.json")));
    var actual = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).Where(path => path != "manifest.json").ToHashSet();
    var listed = new HashSet<string>();
    foreach (var item in manifest.RootElement.GetProperty("files").EnumerateArray())
    {
        var relative = item.GetProperty("path").GetString()!;
        Check(listed.Add(relative), "Inventory paths must be unique");
        var bytes = File.ReadAllBytes(Path.Combine(root, relative));
        Check(bytes.LongLength == item.GetProperty("length").GetInt64(), "Inventory lengths must match");
        Check(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() == item.GetProperty("sha256").GetString(), "Inventory hashes must match normalized bytes");
    }
    Check(actual.SetEquals(listed), "Every packaged file must be inventoried");
}
