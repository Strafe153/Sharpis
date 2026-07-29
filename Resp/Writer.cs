namespace Sharpis.Resp;

public class Writer(Stream stream)
{
    public ValueTask WriteAsync(Value value, CancellationToken token) =>
        stream.WriteAsync(value.Marshal(), token);
}
