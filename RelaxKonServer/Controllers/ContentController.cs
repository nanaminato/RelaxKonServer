using Microsoft.AspNetCore.Mvc;
using RelaxKonServer.Services;

namespace RelaxKonServer.Controllers;

[ApiController]
[Route("api")]
public sealed class ContentController(
    IDownloadService downloads,
    IReleaseService releases,
    IFaqService faq) : ControllerBase
{
    [HttpGet("downloads")]
    public async Task<IActionResult> DownloadsList(CancellationToken cancellationToken) =>
        Ok(await downloads.GetDownloadsAsync(cancellationToken));

    [HttpGet("releases")]
    public async Task<IActionResult> ReleasesList([FromQuery] string language = "en-US", CancellationToken cancellationToken = default) =>
        Ok(await releases.GetReleasesAsync(language, cancellationToken));

    [HttpGet("releases/{version}")]
    public async Task<IActionResult> Release(string version, [FromQuery] string language = "en-US", CancellationToken cancellationToken = default) =>
        await releases.GetReleaseAsync(version, language, cancellationToken) is { } release ? Ok(release) : NotFound();

    [HttpGet("faq")]
    public async Task<IActionResult> FaqList([FromQuery] string language = "en-US", CancellationToken cancellationToken = default) =>
        Ok(await faq.GetFaqAsync(language, cancellationToken));
}
