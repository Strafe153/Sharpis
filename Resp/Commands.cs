namespace Sharpis.Resp;

public static class Commands
{
    public const string Ping = "ping";
    public const string Set = "set";
    public const string Get = "get";
    public const string HGet = "hget";
    public const string HSet = "hset";
    public const string HGetAll = "hgetall";

    public static bool IsModification(string key) =>
        string.Equals(key, Set, StringComparison.OrdinalIgnoreCase)
        || string.Equals(key, HSet, StringComparison.OrdinalIgnoreCase);
}
