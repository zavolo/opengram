namespace MyTelegram.Services.Services;

public interface IResponseCacheManager
{
    TaskCompletionSource<object> CreateTaskCompletionSource<T>(long key);
    Task SetResponseAsync(long key, object response);
}