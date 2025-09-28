using System.Collections.Concurrent;

namespace MyTelegram.Messenger.Services.Impl;

public class ResponseCacheManager : MyTelegram.Messenger.Services.Interfaces.IResponseCacheManager
{
    private readonly ConcurrentDictionary<long, TaskCompletionSource<object>> _responseTasks = new();

    public TaskCompletionSource<object> CreateTaskCompletionSource<T>(long key)
    {
        var tcs = new TaskCompletionSource<object>();
        _responseTasks.TryAdd(key, tcs);
        return tcs;
    }

    public Task SetResponseAsync(long key, object response)
    {
        if (_responseTasks.TryRemove(key, out var tcs))
        {
            tcs.SetResult(response);
        }
        return Task.CompletedTask;
    }
}