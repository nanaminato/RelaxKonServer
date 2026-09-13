using Microsoft.Extensions.Options;
using RelaxKonServer.Options;

namespace RelaxKonServer.Services;

public interface IReleaseDeliveryService
{
    bool TryOpen(string relativePath, out ReleaseDeliveryFile file);
}

public sealed record ReleaseDeliveryFile(Stream Stream, string DownloadName, string ContentType, bool IsVersioned);

/// <summary>Opens only known release artifact types from the configured release catalog.</summary>
public sealed class ReleaseDeliveryService : IReleaseDeliveryService
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".zip"] = "application/zip",
        [".json"] = "application/json",
        [".sha256"] = "text/plain",
        [".ps1"] = "text/plain",
        [".sh"] = "text/plain"
    };

    private readonly string _root;

    public ReleaseDeliveryService(IWebHostEnvironment environment, IOptions<ReleaseDeliveryOptions> options)
    {
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));
    }

    public bool TryOpen(string relativePath, out ReleaseDeliveryFile file)
    {
        file = default!;
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Length > 512) return false;

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".." || !segment.All(IsSafePathCharacter))) return false;

        var extension = Path.GetExtension(normalized);
        if (!ContentTypes.TryGetValue(extension, out var contentType)) return false;

        var path = Path.GetFullPath(Path.Combine(_root, Path.Combine(segments)));
        var rootWithSeparator = _root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return false;

        var isVersioned = segments.Length >= 3 && !segments.Contains("latest", StringComparer.OrdinalIgnoreCase);
        // The publisher atomically replaces latest descriptors. Allow the replacement while an
        // in-flight response continues reading its already-open file handle.
        file = new ReleaseDeliveryFile(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Path.GetFileName(path), contentType, isVersioned);
        return true;
    }

    private static bool IsSafePathCharacter(char value) => char.IsAsciiLetterOrDigit(value) || value is '.' or '-' or '_';
}
