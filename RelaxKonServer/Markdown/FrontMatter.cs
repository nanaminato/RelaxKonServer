namespace RelaxKonServer.Markdown;

/// <summary>
/// Minimal parser for the YAML-style front matter used by RelaxKonServer content
/// files. Only flat <c>key: value</c> pairs are supported, which is sufficient
/// for documentation, release notes and FAQ metadata.
/// </summary>
public static class FrontMatter
{
    private const string Delimiter = "---";

    /// <summary>Parses the leading front matter block into a case-insensitive dictionary.</summary>
    public static IReadOnlyDictionary<string, string> Parse(string source)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!TryGetBodyStart(source, out var bodyStart)) return result;

        var block = source[..bodyStart];
        foreach (var rawLine in block.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(Delimiter, StringComparison.Ordinal)) continue;

            var separator = line.IndexOf(':');
            if (separator <= 0) continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"', '\'');
            if (key.Length > 0) result[key] = value;
        }

        return result;
    }

    /// <summary>Returns the Markdown body that follows the front matter block.</summary>
    public static string Strip(string source) =>
        TryGetBodyStart(source, out var bodyStart) ? source[bodyStart..].TrimStart('\r', '\n') : source;

    /// <summary>
    /// Locates the first character of the body. When a front matter block is not
    /// present the start of the source is returned so callers can use the result
    /// unconditionally.
    /// </summary>
    private static bool TryGetBodyStart(string source, out int bodyStart)
    {
        bodyStart = 0;
        if (!source.StartsWith(Delimiter, StringComparison.Ordinal)) return false;

        var openingLineEnd = source.IndexOf('\n');
        if (openingLineEnd < 0) return false;

        var closingLineStart = source.IndexOf("\n" + Delimiter, openingLineEnd, StringComparison.Ordinal);
        if (closingLineStart < 0) return false;

        // Skip the newline that introduces the closing delimiter plus the delimiter itself.
        var afterClosing = closingLineStart + 1 + Delimiter.Length;
        // Skip the rest of the closing delimiter line (for example "---\r").
        var lineEnd = source.IndexOf('\n', afterClosing);
        bodyStart = lineEnd < 0 ? source.Length : lineEnd + 1;
        return true;
    }
}
