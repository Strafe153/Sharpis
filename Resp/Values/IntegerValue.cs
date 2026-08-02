using System.Text;

namespace Sharpis.Resp.Values;

public class IntegerValue : Value
{
    public required int Value { get; init; }

    public override byte[] Marshal()
    {
        var integerData = Encoding.ASCII.GetBytes(Value.ToString());

        byte[] bytes = [
            (byte)TypeIdentifiers.Integer,
            ..integerData,
            Cr,
            Lf
        ];

        return bytes;
    }
}
