using System.Net;
using System.Net.Sockets;
using Sharpis.Resp;
using Sharpis.Resp.Values;

using TcpListener listener = CreateListener();
using CancellationTokenSource tokenSrc = new();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    tokenSrc.Cancel();
};

try
{
    await using AppendOnlyFile aof = new();
    await ReadAofAsync(aof, tokenSrc.Token);

    _ = aof.SyncAsync(tokenSrc.Token);
    listener.Start();

    while (!tokenSrc.IsCancellationRequested)
    {
        var client = await listener.AcceptTcpClientAsync(tokenSrc.Token);
        _ = HandleClient(client, aof, tokenSrc.Token);
    }
}
catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
{
    Console.WriteLine("The server is shutting down");
}
catch (SocketException)
{
    Console.WriteLine("Port is already in use");
}
catch
{
    Console.WriteLine("Unexpected error");
}

TcpListener CreateListener()
{
    ushort port = 6379;

    if (args.Length == 1 && !ushort.TryParse(args[0], out port))
    {
        Console.WriteLine("Invalid port");
        Environment.Exit(1);
    }

    IPAddress address = new([127, 0, 0, 1]);
    IPEndPoint endpoint = new(address, port);

    return new(endpoint);
}

Task ReadAofAsync(AppendOnlyFile file, CancellationToken token)
{
    return file.ReadAsync(async value =>
    {
        if (value is not ArrayValue arr || arr.Value.Length == 0)
        {
            Console.WriteLine("Invalid request, expected a non-empty array");
            return;
        }

        if (arr.Value[0] is not BulkValue bulk
            || string.IsNullOrEmpty(bulk.Value)
            || !Handler.Handlers.TryGetValue(bulk.Value, out var handler))
        {
            Console.WriteLine("Invalid command");
            return;
        }

        handler(arr.Value[1..]);
    }, token);
}

async Task HandleClient(TcpClient client, AppendOnlyFile appendOnlyFile, CancellationToken token)
{
    try
    {
        using (client)
        {
            await using NetworkStream stream = client.GetStream();
            await using BufferedStream bufferedStream = new(stream);

            if (!bufferedStream.CanRead || !bufferedStream.CanWrite)
            {
                Console.WriteLine("The stream does not support reading and/or writing");
                return;
            }

            Reader reader = new(bufferedStream);
            Writer writer = new(bufferedStream);

            while (!token.IsCancellationRequested)
            {
                var value = await reader.ReadAsync(token);
                if (value is NullValue)
                {
                    break;
                }

                if (value is not ArrayValue arr || arr.Value.Length == 0)
                {
                    Console.WriteLine("Invalid request, expected a non-empty array");
                    continue;
                }

                if (arr.Value[0] is not BulkValue bulk
                    || string.IsNullOrEmpty(bulk.Value)
                    || !Handler.Handlers.TryGetValue(bulk.Value, out var handler))
                {
                    Console.WriteLine("Invalid command");
                    await writer.WriteAsync(StringValue.Empty, token);
                    continue;
                }

                if (Commands.IsModification(bulk.Value))
                {
                    await appendOnlyFile.WriteAsync(value, token);
                }

                var responseValue = handler(arr.Value[1..]);
                await writer.WriteAsync(responseValue, token);
            }
        }
    }
    catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
    {
        Console.WriteLine("Client connection terminated due to server shutdown.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Client exception: {0}", ex);
    }
}
