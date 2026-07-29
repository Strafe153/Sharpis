namespace Sharpis.Resp;

public class AppendOnlyFile : IDisposable
{
    private readonly FileStream _file = File.Open(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "sharpis.aof"),
        FileMode.OpenOrCreate,
        FileAccess.ReadWrite);

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public void Dispose()
    {
        _file.Flush();
        _file.Dispose();
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

    // If i were to add a support for multiple clients work with the app simultaneously
    // then adding a semaphore wait around reading potentially makes sense too
    // however at that point I think none of them would be able to read the whole file
    // thus a filestream on the AOF for every client seems like the way to go
    public async Task ReadAsync(Action<Value> action, CancellationToken token)
    {
        Reader reader = new(_file);

        while (true)
        {
            var value = await reader.ReadAsync(token);

            if (value.Type == ValueType.String && string.IsNullOrEmpty(value.String))
            {
                break;
            }

            action(value);
        }
    }
}
