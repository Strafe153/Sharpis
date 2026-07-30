namespace Sharpis.Resp;

public class Handler
{
    private static readonly Dictionary<string, string> _sets = [];
    private static readonly Dictionary<string, Dictionary<string, string>> _hSets = [];

    private static readonly Dictionary<string, Func<Value[], Value>> _handlers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "ping", Pong },
            { "set", Set },
            { "get", Get },
            { "hset", Hset },
            { "hget", Hget },
            { "hgetall", HgetAll }
        };

    public static Dictionary<string, Func<Value[], Value>> Handlers => _handlers;

    private static Value Pong(Value[] args)
    {
        Value value = new()
        {
            Type = ValueType.String
        };

        if (args.Length == 0)
        {
            value.String = "PONG";
            return value;
        }

        value.String = args[0].Bulk;
        return value;
    }

    private static Value Set(Value[] args)
    {
        if (args.Length != 2)
        {
            return new()
            {
                Type = ValueType.Error,
                String = "Wrong number of arguments for the \"set\" command."
            };
        }

        var key = args[0].Bulk;
        var value = args[1].Bulk;

        _sets[key] = value;

        return new()
        {
            Type = ValueType.String,
            String = "OK"
        };
    }

    private static Value Get(Value[] args)
    {
        if (args.Length != 1)
        {
            return new()
            {
                Type = ValueType.Error,
                String = "Wrong number of arguments for the \"get\" command."
            };
        }

        var key = args[0].Bulk;

        if (!_sets.TryGetValue(key, out var value))
        {
            return new()
            {
                Type = ValueType.Null,
            };
        }

        return new()
        {
            Type = ValueType.String,
            String = value!
        };
    }

    private static Value Hset(Value[] args)
    {
        if (args.Length != 3)
        {
            return new()
            {
                Type = ValueType.Error,
                String = "Wrong number of arguments for the \"hset\" command."
            };
        }

        var groupKey = args[0].Bulk;
        var key = args[1].Bulk;
        var value = args[2].Bulk;

        if (!_hSets.TryGetValue(groupKey, out var group))
        {
            _hSets[groupKey] = [];
        }

        group![key] = value;

        return new()
        {
            Type = ValueType.String,
            String = "OK"
        };
    }

    private static Value Hget(Value[] args)
    {
        if (args.Length != 2)
        {
            return new()
            {
                Type = ValueType.Error,
                String = "Wrong number of arguments for the \"hget\" command."
            };
        }

        var groupKey = args[0].Bulk;
        var key = args[1].Bulk;

        if (!_hSets.TryGetValue(groupKey, out var group))
        {
            return new()
            {
                Type = ValueType.Null
            };
        }

        if (!group.TryGetValue(key, out var value))
        {
            return new()
            {
                Type = ValueType.Null
            };
        }

        return new()
        {
            Type = ValueType.String,
            String = value!
        };
    }

    private static Value HgetAll(Value[] args)
    {
        if (args.Length != 1)
        {
            return new()
            {
                Type = ValueType.Error,
                String = "Wrong number of arguments for the \"hgetall\" command."
            };
        }

        var groupKey = args[0].Bulk;

        if (!_hSets.TryGetValue(groupKey, out var group))
        {
            return new()
            {
                Type = ValueType.Null
            };
        }

        Value value = new()
        {
            Type = ValueType.Array,
            Array = new Value[group!.Count]
        };

        int i = 0;

        foreach (var v in group.Values)
        {
            value.Array[i++] = new()
            {
                Type = ValueType.String,
                String = v
            };
        }

        return value;
    }
}
