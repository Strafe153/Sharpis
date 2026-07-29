using System.Text;

namespace Sharpis.Resp;

public class Reader(Stream stream)
{
    public Task<Value> ReadAsync(CancellationToken token)
    {
        var valueType = stream.ReadByte();

        return valueType switch
        {
            TypeIdentifiers.Array => ReadArrayAsync(token),
            TypeIdentifiers.Bulk => ReadBulkAsync(token),
            _ => Task.FromResult(new Value())
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
        Value value = new()
        {
            Type = ValueType.Array,
        };

        var length = ReadInt();
        var values = new Value[length];

        for (int i = 0; i < length; i++)
        {
            var v = await ReadAsync(token);
            values[i] = v;
        }

        value.Array = values;

        return value;
    }

    private async Task<Value> ReadBulkAsync(CancellationToken token)
    {
        Value value = new()
        {
            Type = ValueType.Bulk
        };

        var length = ReadInt();
        var bytes = new byte[length];

        await stream.ReadExactlyAsync(bytes, token);
        await stream.ReadExactlyAsync(new byte[2], token); // Read \r\n

        value.Bulk = Encoding.ASCII.GetString(bytes);

        return value;
    }
}
