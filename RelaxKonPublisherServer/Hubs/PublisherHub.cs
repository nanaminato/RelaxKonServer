using Microsoft.AspNetCore.SignalR;
using RelaxKon_Publisher.Models;
using RelaxKon_Publisher.Services;

namespace RelaxKon_Publisher.Hubs;

/// <summary>Local-only SignalR channel for publishing task status and execution logs.</summary>
public sealed class PublisherHub(PublisherService publisher) : Hub
{
    public static string JobGroup(Guid jobId) => $"publisher-job-{jobId:N}";

    public Task StartPreview(PublisherPlanRequest request)
    {
        publisher.StartPreview(request, Context.ConnectionId);
        return Task.CompletedTask;
    }

    public async Task Subscribe(Guid jobId)
    {
        var job = publisher.GetJob(jobId) ?? throw new HubException("发布任务不存在。");
        await Groups.AddToGroupAsync(Context.ConnectionId, JobGroup(jobId));
        await Clients.Caller.SendAsync("jobUpdated", job);
    }
}
