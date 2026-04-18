namespace Rehably.Application.Services.Platform;

public interface IJobService
{
    string Enqueue<T>(string methodCall, object? args = null);
    string Schedule(string methodCall, object? args, TimeSpan delay);
    bool Delete(string jobId);
    bool Delete(string jobId, string stateName);
}
