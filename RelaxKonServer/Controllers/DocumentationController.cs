using Microsoft.AspNetCore.Mvc;
using RelaxKonServer.Services;

namespace RelaxKonServer.Controllers;

[ApiController]
[Route("api/docs")]
public sealed class DocumentationController(IDocumentService documents) : ControllerBase
{
    [HttpGet("languages")]
    public async Task<IActionResult> Languages(CancellationToken cancellationToken) => Ok(await documents.GetLanguagesAsync(cancellationToken));

    [HttpGet("versions")]
    public async Task<IActionResult> Versions([FromQuery] string? language, CancellationToken cancellationToken) => Ok(await documents.GetVersionsAsync(language, cancellationToken));

    [HttpGet("{language}/{version}/navigation")]
    public async Task<IActionResult> Navigation(string language, string version, CancellationToken cancellationToken) =>
        await documents.GetNavigationAsync(language, version, cancellationToken) is { } navigation ? Ok(navigation) : NotFound();

    [HttpGet("{language}/{version}/{**slug}")]
    public async Task<IActionResult> Document(string language, string version, string slug, CancellationToken cancellationToken) =>
        await documents.GetDocumentAsync(language, version, slug, cancellationToken) is { } document ? Ok(document) : NotFound();

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string language = "en-US", [FromQuery] string version = "latest", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2) return BadRequest(new { message = "Search query must contain at least two characters." });
        return Ok(await documents.SearchAsync(q, language, version, cancellationToken));
    }
}
