namespace Sharpis.Resp.Values;

public sealed class NullValue : Value
{
    private static byte[] Value => [
        (byte)TypeIdentifiers.Bulk,
        (byte)'-',
        (byte)'1',
        Cr,
        Lf
    ];

    public override byte[] Marshal() => Value;
}
