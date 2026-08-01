using System.Text;
using Sharpis.Resp.Values;

namespace Sharpis.Resp;

public sealed class Reader(Stream stream)
{
    public async Task<Value> ReadAsync(CancellationToken token)
    {
        var valueType = await ReadByteAsync(token);

        return valueType switch
        {
            TypeIdentifiers.Array => await ReadArrayAsync(token),
            TypeIdentifiers.Bulk => await ReadBulkAsync(token),
            _ => new NullValue()
        };
    }

    // This custom async implementation is needed in order to be able to later on
    // directly call HandleClient without awaiting it,
    // since stream.ReadByte() is blocking and otherwise Task.Run() must be used
    private async Task<int> ReadByteAsync(CancellationToken token)
    {
        try
        {
            var b = new byte[1];
            await stream.ReadExactlyAsync(b, token);

            return b[0];
        }
        catch (EndOfStreamException)
        {
            return -1;
        }
    }

    private async Task<byte[]> ReadLine(CancellationToken token)
    {
        List<byte> line = [];

        while (true)
        {
            var b = await ReadByteAsync(token);
            if (b == -1)
            {
                break;
            }

            line.Add((byte)b);

            if (line.Count >= 2 && line[^2] == '\r')
            {
                break;
            }
        }

        return [.. line];
    }

    private async Task<int> ReadInt(CancellationToken token)
    {
        var line = await ReadLine(token);
        var parseResult = int.TryParse(line, out var number);

        if (!parseResult)
        {
            throw new Exception("Failed to parse int");
        }

        return number;
    }

    private async Task<Value> ReadArrayAsync(CancellationToken token)
    {
        var length = await ReadInt(token);

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
        var length = await ReadInt(token);
        var bytes = new byte[length];

        await stream.ReadExactlyAsync(bytes, token);
        await stream.ReadExactlyAsync(new byte[2], token); // Read \r\n

        return new BulkValue
        {
            Value = Encoding.ASCII.GetString(bytes)
        };
    }
}
