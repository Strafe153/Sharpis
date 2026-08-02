namespace Sharpis.Resp;

public struct Commands
{
    public const string Ping = "ping";
    public const string Set = "set";
    public const string Get = "get";
    public const string HGet = "hget";
    public const string HSet = "hset";
    public const string HGetAll = "hgetall";
    public const string StrLen = "strlen";
    public const string Incr = "incr";
    public const string IncrBy = "incrby";
    public const string Exists = "exists";
    public const string HExists = "hexists";
    public const string MGet = "mget";
    public const string MSet = "mset";

    public static bool IsModification(string key)
    {
        string[] commands = [Set, HSet, Incr, IncrBy];
        var isModification = commands.Any(o => string.Equals(key, o, StringComparison.OrdinalIgnoreCase));

        return isModification;
    }
}
