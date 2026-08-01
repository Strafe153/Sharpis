using Sharpis.Resp.Values;

namespace Sharpis.Resp;

public sealed class AppendOnlyFile : IAsyncDisposable
{
    private readonly FileStream _file = File.Open(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "sharpis.aof"),
        FileMode.OpenOrCreate,
        FileAccess.ReadWrite);

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async ValueTask DisposeAsync()
    {
        await _file.FlushAsync();
        await _file.DisposeAsync();
    }

    public async Task SyncAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            await _semaphore.WaitAsync(token);

            try
            {
                await _file.FlushAsync(token);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    public async Task WriteAsync(Value value, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);

        try
        {
            await _file.WriteAsync(value.Marshal(), token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ReadAsync(Action<Value> action, CancellationToken token)
    {
        Reader reader = new(_file);

        while (!token.IsCancellationRequested)
        {
            var value = await reader.ReadAsync(token);

            if (value is NullValue)
            {
                break;
            }

            action(value);
        }
    }
}
