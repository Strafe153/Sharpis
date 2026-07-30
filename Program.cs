using System.Net;
using System.Net.Sockets;
using Sharpis.Resp;

short port = 6379;

if (args.Length == 1 && !short.TryParse(args[0], out port))
{
    Console.WriteLine("Invalid port");
    return;
}

IPAddress address = new([127, 0, 0, 1]);
IPEndPoint endpoint = new(address, port);

using TcpListener listener = new(endpoint);

CancellationTokenSource tokenSrc = new();

listener.Start();

try
{
    // for now do this, but in order to try several clients simultaneously, later try moving this inside the while (true) loop
    // and after that add anoter inner while (true) loop around if (bufferedStream.CanRead), though offload that into a separate
    // task method and move buffer initialization in there in order to avoid any issues
    using TcpClient client = await listener.AcceptTcpClientAsync();

    using NetworkStream stream = client.GetStream();
    using BufferedStream bufferedStream = new(stream);

    using AppendOnlyFile appendOnlyFile = new();

    await appendOnlyFile.ReadAsync(async value =>
    {
        var command = value.Array[0].Bulk;
        var args = value.Array[1..];

        if (string.IsNullOrEmpty(command))
        {
            Console.WriteLine("invalid command");
            return;
        }

        if (!Handler.Handlers.TryGetValue(command, out var handler))
        {
            Console.WriteLine("invalid command");
            return;
        }

        handler!(args);
    }, tokenSrc.Token);

    _ = appendOnlyFile.SyncAsync(tokenSrc.Token);

    Reader reader = new(bufferedStream);
    Writer writer = new(bufferedStream);

    while (true)
    {
        var value = await reader.ReadAsync(tokenSrc.Token);

        if (value.Type != Sharpis.Resp.ValueType.Array)
        {
            Console.WriteLine("invalid request, expected array");
            // await writer.WriteAsync(new() { Type = RespType.String, String = string.Empty }, tokenSrc.Token);
            continue;
        }

        if (value.Array.Length == 0)
        {
            Console.WriteLine("invalid request, array length should be > 0");
            // await writer.WriteAsync(new() { Type = RespType.String, String = string.Empty }, tokenSrc.Token);
            continue;
        }

        var command = value.Array[0].Bulk;
        if (string.IsNullOrEmpty(command))
        {
            Console.WriteLine("invalid command");
            await writer.WriteAsync(new() { Type = Sharpis.Resp.ValueType.String, String = string.Empty }, tokenSrc.Token);
            continue;
        }

        var arguments = value.Array[1..];

        if (!Handler.Handlers.TryGetValue(command, out var handler))
        {
            Console.WriteLine("invalid command");
            await writer.WriteAsync(new() { Type = Sharpis.Resp.ValueType.String, String = string.Empty }, tokenSrc.Token);
            continue;
        }

        if (command == "set" || command == "hset")
        {
            await appendOnlyFile.WriteAsync(value, tokenSrc.Token);
        }

        var result = handler!(arguments);
        await writer.WriteAsync(result, tokenSrc.Token);
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
