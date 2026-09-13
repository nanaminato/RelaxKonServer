namespace RelaxKonServer.Options;

/// <summary>
/// Settings for the product release artifacts distributed by the website API.
/// The directory is deployment data and is intentionally outside of the web UI.
/// </summary>
public sealed class ReleaseDeliveryOptions
{
    public const string SectionName = "ReleaseDelivery";

    /// <summary>Directory containing the versioned release catalog, relative to the content root.</summary>
    public string RootPath { get; init; } = "Content/ReleaseDelivery";

    /// <summary>
    /// Canonical public origin used by the Windows bootstrap loader. Additional DNS aliases may
    /// serve the same application, but should redirect here when a canonical URL is needed.
    /// </summary>
    public string PublicBaseUri { get; init; } = "https://downloads.relaxkon.com";
}
