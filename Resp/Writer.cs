using Sharpis.Resp.Values;

namespace Sharpis.Resp;

public sealed class Writer(Stream stream)
{
    public ValueTask WriteAsync(Value value, CancellationToken token) =>
        stream.WriteAsync(value.Marshal(), token);
}
