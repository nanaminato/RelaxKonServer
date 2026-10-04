using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using RelaxKon_Publisher.Services;

var root = Path.Combine(Path.GetTempPath(), "publisher-packaging-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var source = Path.Combine(root, "source");
    Write("deployment/bootstrap/install-relaxkonos.sh", "#!/bin/bash\r\nexit 0\r\n");
    Write("deployment/linux/install-relaxkonos-services.sh", "#!/bin/bash\r\nexit 0\r\n");
    Write("deployment/windows/Install-RelaxKonOSServices.ps1", "Write-Host OK\r\n");
    Write("deployment/verify-release-inventory.py", "# verifier\n");
    foreach (var platform in new[] { "linux", "windows" })
    {
        var package = Path.Combine(root, platform);
        Directory.CreateDirectory(package);
        Invoke("CopyServerDeploymentFiles", source, package, platform);
        var verifier = Path.Combine(package, "deployment", "verify-release-inventory.py");
        Check(File.Exists(verifier) == (platform == "linux"), "Linux verifier must be packaged");
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
    File.Delete(Path.Combine(source, "deployment", "verify-release-inventory.py"));
    try
    {
        Invoke("CopyServerDeploymentFiles", source, Path.Combine(root, "missing"), "linux");
        throw new Exception("Missing verifier must fail packaging");
    }
    catch (TargetInvocationException error) when (error.InnerException is FileNotFoundException) { }
    Console.WriteLine("PASS: Linux verifier, LF conversion, complete inventories, repeat generation, macOS README, missing verifier rejection.");

    void Write(string relative, string content)
    {
        var path = Path.Combine(source, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
finally { Directory.Delete(root, recursive: true); }

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
