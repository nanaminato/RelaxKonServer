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
    public async Task<ActionResult<PublisherPreview>> Preview(PublisherPlanRequest request, CancellationToken cancellationToken) =>
        await publisher.PreviewAsync(request, cancellationToken);

    [HttpPost("generate")]
    public ActionResult<PublisherJob> Generate(PublisherPlanRequest request) => Accepted(publisher.StartGeneration(request));

    [HttpGet("jobs/{id:guid}")]
    public ActionResult<PublisherJob> GetJob(Guid id) => publisher.GetJob(id) is { } job ? Ok(job) : NotFound();
}
