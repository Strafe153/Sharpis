namespace Sharpis.Resp.Values;

public abstract class Value
{
    protected const byte Cr = (byte)'\r';
    protected const byte Lf = (byte)'\n';

    public abstract byte[] Marshal();
}
