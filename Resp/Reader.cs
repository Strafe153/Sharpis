using System.Text;
using Sharpis.Resp.Values;

namespace Sharpis.Resp;

public sealed class Reader(Stream stream)
{
    public async Task<Value> ReadAsync(CancellationToken token)
    {
        var valueType = stream.ReadByte();

        return valueType switch
        {
            TypeIdentifiers.Array => await ReadArrayAsync(token),
            TypeIdentifiers.Bulk => await ReadBulkAsync(token),
            _ => new NullValue()
        };
    }

    private byte[] ReadLine()
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

    private int ReadInt()
    {
        var line = ReadLine();
        var parseResult = int.TryParse(line, out var number);

        if (!parseResult)
        {
            throw new Exception("Failed to parse int");
        }

        return number;
    }

    private async Task<Value> ReadArrayAsync(CancellationToken token)
    {
        var length = ReadInt();

        ArrayValue value = new()
        {
            Value = new Value[length]
        };

        for (int i = 0; i < length; i++)
        {
            var v = await ReadAsync(token);
            value.Value[i] = v;
        }

        return value;
    }

    private async Task<Value> ReadBulkAsync(CancellationToken token)
    {
        var length = ReadInt();
        var bytes = new byte[length];

        await stream.ReadExactlyAsync(bytes, token);
        await stream.ReadExactlyAsync(new byte[2], token); // Read \r\n

        return new BulkValue
        {
            Value = Encoding.ASCII.GetString(bytes)
        };
    }
}
