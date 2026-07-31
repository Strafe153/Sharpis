using System.Text;

namespace Sharpis.Resp.Values;

public sealed class ErrorValue : Value
{
    public required string Value { get; init; }

    public override byte[] Marshal()
    {
        var errorData = Encoding.ASCII.GetBytes(Value);

        byte[] bytes = [
            (byte)TypeIdentifiers.Error,
            ..errorData,
            Cr,
            Lf
        ];

        return bytes;
    }
}
