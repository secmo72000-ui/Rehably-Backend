using Hangfire;
using Hangfire.States;
using Rehably.Application.Services.Platform;

namespace Rehably.Infrastructure.Services;

public class JobService : IJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public JobService(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public string Enqueue<T>(string methodCall, object? args = null)
    {
        return BackgroundJob.Enqueue<T>(x => ExecuteJob(x, methodCall, args));
    }

    public string Schedule(string methodCall, object? args, TimeSpan delay)
    {
        return BackgroundJob.Schedule(() => ExecuteJob(methodCall, args), delay);
    }

    public bool Delete(string jobId)
    {
        return BackgroundJob.Delete(jobId);
    }

    public bool Delete(string jobId, string stateName)
    {
        return _backgroundJobClient.ChangeState(jobId, new EnqueuedState(stateName));
    }

    private static void ExecuteJob(string methodCall, object? args)
    {
    }

    private static void ExecuteJob<T>(T service, string methodCall, object? args)
    {
        var method = service?.GetType().GetMethod(methodCall);
        method?.Invoke(service, args != null ? new[] { args } : null);
    }
}
