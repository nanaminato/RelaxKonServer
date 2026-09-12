using Microsoft.AspNetCore.Mvc;
using RelaxKonServer.Models;

namespace RelaxKonServer.Controllers;

[ApiController]
[Route("api")]
public sealed class ContentController : ControllerBase
{
    private static readonly DownloadInfo[] Downloads =
    [
        new("Windows", "x64", "Preview", "#", "Coming soon", "", new DateOnly(2026, 9, 11), false),
        new("Linux", "x64", "Preview", "#", "Coming soon", "", new DateOnly(2026, 9, 11), false),
        new("macOS", "ARM64", "Preview", "#", "Coming soon", "", new DateOnly(2026, 9, 11), false)
    ];
    private static readonly ReleaseDetails[] Releases =
    [new("preview", "RelaxKonOS Preview", "# RelaxKonOS Preview\n\nThe website framework is ready for the first public product releases. Download artifacts will be published here when they are available.", new DateOnly(2026, 9, 11))];
    private static readonly FaqItem[] Faq =
    [
        new("Is RelaxKonOS a remote desktop product?", "No. The client renders its own interface locally; the server manages workspace state, storage, runtime, and remote services instead of sending desktop pixels.", 1),
        new("Which platforms are planned?", "The product architecture is cross-platform. Download availability is published on the Downloads page.", 2),
        new("Where can I learn about built-in applications?", "Use Documentation for application guides and the Concepts section for the architecture behind them.", 3)
    ];

    [HttpGet("downloads")] public ActionResult<IReadOnlyList<DownloadInfo>> DownloadsList() => Ok(Downloads);
    [HttpGet("releases")] public ActionResult<IReadOnlyList<ReleaseSummary>> ReleasesList() => Ok(Releases.Select(item => new ReleaseSummary(item.Version, item.Title, "Initial public website framework.", item.ReleaseDate)));
    [HttpGet("releases/{version}")] public ActionResult<ReleaseDetails> Release(string version) => Releases.FirstOrDefault(item => item.Version.Equals(version, StringComparison.OrdinalIgnoreCase)) is { } item ? Ok(item) : NotFound();
    [HttpGet("faq")] public ActionResult<IReadOnlyList<FaqItem>> FaqList() => Ok(Faq);
}
