using System.Text;

namespace Sharpis.Resp;

public class Value
{
    private const byte Cr = (byte)'\r';
    private const byte Lf = (byte)'\n';

    public ValueType Type { get; set; }
    public string String { get; set; }
    public int Number { get; set; }
    public string Bulk { get; set; }
    public Value[] Array { get; set; }

    public byte[] Marshal() => Type switch
    {
        ValueType.Array => MarshalArray(),
        ValueType.Bulk => MarshalBulk(),
        ValueType.String => MarshalString(),
        ValueType.Null => MarshalNull(),
        ValueType.Error => MarshalError(),
        _ => []
    };

    private byte[] MarshalString()
    {
        var stringBytes = Encoding.ASCII.GetBytes(String);

        byte[] bytes = [
            (byte)TypeIdentifiers.String,
            ..stringBytes,
            Cr,
            Lf
        ];

        return bytes;
    }

    private byte[] MarshalBulk()
    {
        var length = Encoding.ASCII.GetBytes(Bulk.Length.ToString());
        var bulkData = Encoding.ASCII.GetBytes(Bulk);

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

    private byte[] MarshalArray()
    {
        var length = Encoding.ASCII.GetBytes(Array.Length.ToString());

        List<byte> bytes = [
            (byte)TypeIdentifiers.Array,
            ..length,
            Cr,
            Lf
        ];

        foreach (var value in Array)
        {
            var marshaled = value.Marshal();
            bytes.AddRange(marshaled);
        }

        return [.. bytes];
    }

    private byte[] MarshalError()
    {
        var errorData = Encoding.ASCII.GetBytes(String);

        byte[] bytes = [
            (byte)TypeIdentifiers.Error,
            ..errorData,
            Cr,
            Lf
        ];

        return bytes;
    }

    private static byte[] MarshalNull() => [
        (byte)TypeIdentifiers.Bulk,
        (byte)'-',
        (byte)'1',
        Cr,
        Lf
    ];
}
