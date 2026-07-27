using System.Net;
using System.Net.Sockets;
using System.Text;

IPAddress address = new([127, 0, 0, 1]);
// Add custom port binding
IPEndPoint endpoint = new(address, 6379);

using TcpListener listener = new(endpoint);

var buffer = new byte[4096];
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

    while (true)
    {
        while (true)
        {
            Reader reader = new(stream);
            var value = await reader.Read();

            if (value.Array?.Length > 0)
            {
                foreach (var v in value.Array)
                {
                    Console.WriteLine(v.String);
                }
            }

            await stream.WriteAsync(Encoding.ASCII.GetBytes("+OK\r\n").AsMemory());
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

public static class RespTypes
{
    public const char Number = ':';
    public const char String = '+';
    public const char Error = '-';
    public const char Bulk = '$';
    public const char Array = '*';
}

public enum RespType
{
    String,
    Bulk,
    Array
}

public class Value
{
    public RespType Type { get; set; }
    public string String { get; set; }
    public int Number { get; set; }
    public string Bulk { get; set; }
    public Value[] Array { get; set; }
}

public class Reader(Stream stream)
{
    public async Task<string[]> ReadLines(CancellationToken token)
    {
        var buffer = new byte[4096];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(), token);

        if (bytesRead > 0)
        {
            var data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            return data.Split("\r\n");
        }

        return [];
    }

    public byte[] ReadLine()
    {
        List<byte> line = [];

        while (true)
        {
            var b = stream.ReadByte();
            line.Add((byte)b);

            if (line.Count >= 2 && line[^2] == '\r')
            {
                break;
            }
        }

        return [.. line];
    }

    public int ReadInt()
    {
        var line = ReadLine();
        var parseResult = int.TryParse(line, out var number);

        if (!parseResult)
        {
            throw new Exception("Failed to parse int");
        }

        return number;
    }

    public async Task<Value> ReadArray()
    {
        Value value = new()
        {
            Type = RespType.Array,
        };

        var length = ReadInt();
        var values = new Value[length];

        for (int i = 0; i < length; i++)
        {
            var v = await Read();
            values[i] = v;
        }

        value.Array = values;

        return value;
    }

    public async Task<Value> ReadBulk()
    {
        Value value = new()
        {
            Type = RespType.Bulk
        };

        var length = ReadInt();
        var bytes = new byte[length];

        await stream.ReadExactlyAsync(bytes.AsMemory());
        await stream.ReadExactlyAsync(new byte[2].AsMemory()); // Read \r\n

        value.String = Encoding.ASCII.GetString(bytes);

        return value;
    }

    public Task<Value> Read()
    {
        var valueType = stream.ReadByte();

        return valueType switch
        {
            RespTypes.Array => ReadArray(),
            RespTypes.Bulk => ReadBulk(),
            _ => Task.FromResult(new Value())
        };
    }
}
