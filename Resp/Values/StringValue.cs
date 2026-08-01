using System.Text;

namespace Sharpis.Resp.Values;

public sealed class StringValue : Value
{
    private static readonly StringValue _empty = new()
    {
        Value = string.Empty
    };

    public static StringValue Empty => _empty;

    public required string Value { get; init; }

    public override byte[] Marshal()
    {
        var stringBytes = Encoding.ASCII.GetBytes(Value);

        byte[] bytes = [
            (byte)TypeIdentifiers.String,
            ..stringBytes,
            Cr,
            Lf
        ];

        return bytes;
    }
}
