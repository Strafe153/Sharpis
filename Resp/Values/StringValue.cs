using System.Text;

namespace Sharpis.Resp.Values;

public sealed class StringValue : Value
{
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
