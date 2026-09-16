using Microsoft.AspNetCore.Mvc;
using RelaxKon_Publisher.Models;
using RelaxKon_Publisher.Services;

namespace RelaxKon_Publisher.Controllers;

[ApiController]
[Route("api/publisher")]
public sealed class PublisherController(PublisherService publisher) : ControllerBase
{
    [HttpGet("settings")]
    public ActionResult<PublisherPaths> GetSettings() => publisher.GetDefaultPaths();

    [HttpPost("preview")]
    public async Task<ActionResult<PublisherPreview>> Preview(PublisherPlanRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await publisher.PreviewAsync(request, cancellationToken)); }
        catch (InvalidOperationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("generate")]
    public ActionResult<PublisherJob> Generate(PublisherPlanRequest request)
    {
        try { return Accepted(publisher.StartGeneration(request)); }
        catch (InvalidOperationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("jobs/{id:guid}")]
    public ActionResult<PublisherJob> GetJob(Guid id) => publisher.GetJob(id) is { } job ? Ok(job) : NotFound();

    [HttpPost("jobs/{id:guid}/cancel")]
    public ActionResult<PublisherJob> Cancel(Guid id) => publisher.CancelJob(id) is { } job ? Ok(job) : NotFound();
}
