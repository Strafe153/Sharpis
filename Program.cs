using System.Net;
using System.Net.Sockets;
using Sharpis.Resp;
using Sharpis.Resp.Values;

ushort port = 6379;

if (args.Length == 1 && !ushort.TryParse(args[0], out port))
{
    Console.WriteLine("Invalid port");
    return;
}

IPAddress address = new([127, 0, 0, 1]);
IPEndPoint endpoint = new(address, port);

using TcpListener listener = new(endpoint);

CancellationTokenSource tokenSrc = new();

try
{
    using AppendOnlyFile appendOnlyFile = new();

    await appendOnlyFile.ReadAsync(async value =>
    {
        if (value is ArrayValue arr)
        {
            if (arr.Value[0] is BulkValue bulk)
            {
                if (string.IsNullOrEmpty(bulk.Value)
                    || !Handler.Handlers.TryGetValue(bulk.Value, out var handler))
                {
                    Console.WriteLine("Invalid command");
                    return;
                }

                handler(arr.Value[1..]);
            }
        }
    }, tokenSrc.Token);

    listener.Start();

    // for now do this, but in order to try several clients simultaneously, later try moving this inside the while (true) loop
    // and after that add anoter inner while (true) loop around if (bufferedStream.CanRead), though offload that into a separate
    // task method and move buffer initialization in there in order to avoid any issues
    using TcpClient client = await listener.AcceptTcpClientAsync();

    using NetworkStream stream = client.GetStream();
    using BufferedStream bufferedStream = new(stream);

    _ = appendOnlyFile.SyncAsync(tokenSrc.Token);

    Reader reader = new(bufferedStream);
    Writer writer = new(bufferedStream);

    while (!tokenSrc.IsCancellationRequested)
    {
        var value = await reader.ReadAsync(tokenSrc.Token);
        if (value is NullValue)
        {
            tokenSrc.Cancel();
            continue;
        }

        if (value is not ArrayValue arr)
        {
            Console.WriteLine("Invalid request, expected array");
            continue;
        }

        if (arr.Value.Length == 0)
        {
            Console.WriteLine("Invalid request, array length should be > 0");
            continue;
        }

        if (arr.Value[0] is BulkValue bulk)
        {
            if (string.IsNullOrEmpty(bulk.Value)
                || !Handler.Handlers.TryGetValue(bulk.Value, out var handler))
            {
                Console.WriteLine("Invalid command");
                await writer.WriteAsync(new StringValue() { Value = string.Empty }, tokenSrc.Token);
                continue;
            }

            if (bulk.Value == "set" || bulk.Value == "hset")
            {
                await appendOnlyFile.WriteAsync(value, tokenSrc.Token);
            }

            var result = handler(arr.Value[1..]);
            await writer.WriteAsync(result, tokenSrc.Token);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}
finally
{
    listener.Stop();
}
