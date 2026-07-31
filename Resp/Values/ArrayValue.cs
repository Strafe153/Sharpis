using System.Text;

namespace Sharpis.Resp.Values;

public sealed class ArrayValue : Value
{
    public required Value[] Value { get; init; }

    public override byte[] Marshal()
    {
        var length = Encoding.ASCII.GetBytes(Value.Length.ToString());

        List<byte> bytes = [
            (byte)TypeIdentifiers.Array,
            ..length,
            Cr,
            Lf
        ];

        foreach (var value in Value)
        {
            var marshaled = value.Marshal();
            bytes.AddRange(marshaled);
        }

        return [.. bytes];
    }
}
