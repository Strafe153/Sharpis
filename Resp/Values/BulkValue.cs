using System.Text;

namespace Sharpis.Resp.Values;

public sealed class BulkValue : Value
{
    public required string Value { get; init; }

    public override byte[] Marshal()
    {
        var length = Encoding.ASCII.GetBytes(Value.Length.ToString());
        var bulkData = Encoding.ASCII.GetBytes(Value);

        byte[] bytes = [
            (byte)TypeIdentifiers.Bulk,
            ..length,
            Cr,
            Lf,
            ..bulkData,
            Cr,
            Lf
        ];

        return bytes;
    }
}
