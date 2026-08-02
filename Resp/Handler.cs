using Sharpis.Resp.Values;

namespace Sharpis.Resp;

public static class Handler
{
    private static readonly Dictionary<string, string> _sets = [];
    private static readonly Dictionary<string, Dictionary<string, string>> _hSets = [];

    private static readonly Dictionary<string, Func<Value[], Value>> _handlers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { Commands.Ping, Pong },
            { Commands.Set, Set },
            { Commands.Get, Get },
            { Commands.HSet, Hset },
            { Commands.HGet, Hget },
            { Commands.HGetAll, HgetAll },
            { Commands.StrLen, StrLen },
            { Commands.Incr, Incr }
        };

    public static Dictionary<string, Func<Value[], Value>> Handlers => _handlers;

    private static Value Pong(Value[] args)
    {
        if (args.Length == 0)
        {
            return new StringValue
            {
                Value = "PONG"
            };
        }

        var (bulkArgs, error) = VerifyArguments(args);
        if (error is not null)
        {
            return error;
        }

        return new StringValue
        {
            Value = bulkArgs[0].Value
        };
    }

    private static Value Set(Value[] args)
    {
        if (args.Length != 2)
        {
            return new ErrorValue
            {
                Value = "Wrong number of arguments for the \"set\" command."
            };
        }

        var (bulkArgs, error) = VerifyArguments(args);
        if (error is not null)
        {
            return error;
        }

        var key = bulkArgs[0].Value;
        var value = bulkArgs[1].Value;

        _sets[key] = value;

        return new StringValue
        {
            Value = "OK"
        };
    }

    private static Value Get(Value[] args)
    {
        if (args.Length != 1)
        {
            return new ErrorValue
            {
                Value = "Wrong number of arguments for the \"get\" command."
            };
        }

        var (bulkArgs, error) = VerifyArguments(args);
        if (error is not null)
        {
            return error;
        }

        if (!_sets.TryGetValue(bulkArgs[0].Value, out var value))
        {
            return new NullValue();
        }

        return new StringValue
        {
            Value = value
        };
    }

    private static Value Hset(Value[] args)
    {
        if (args.Length != 3)
        {
            return new ErrorValue
            {
                Value = "Wrong number of arguments for the \"hset\" command."
            };
        }

        var (bulkArgs, error) = VerifyArguments(args);
        if (error is not null)
        {
            return error;
        }

        var groupKey = bulkArgs[0].Value;
        var key = bulkArgs[1].Value;
        var value = bulkArgs[2].Value;

        if (!_hSets.TryGetValue(groupKey, out _))
        {
            _hSets[groupKey] = [];
        }

        _hSets[groupKey][key] = value;

        return new StringValue
        {
            Value = "OK"
        };
    }

    private static Value Hget(Value[] args)
    {
        if (args.Length != 2)
        {
            return new ErrorValue
            {
                Value = "Wrong number of arguments for the \"hget\" command."
            };
        }

        var (bulkArgs, error) = VerifyArguments(args);
        if (error is not null)
        {
            return error;
        }

        var groupKey = bulkArgs[0].Value;
        var key = bulkArgs[1].Value;

        if (!_hSets.TryGetValue(groupKey, out var group))
        {
            return new NullValue();
        }

        if (!group.TryGetValue(key, out var value))
        {
            return new NullValue();
        }

        return new StringValue
        {
            Value = value
        };
    }

    private static Value HgetAll(Value[] args)
    {
        if (args.Length != 1)
        {
            return new ErrorValue
            {
                Value = "Wrong number of arguments for the \"hgetall\" command."
            };
        }

        var (bulkArgs, error) = VerifyArguments(args);
        if (error is not null)
        {
            return error;
        }

        if (!_hSets.TryGetValue(bulkArgs[0].Value, out var group))
        {
            return new NullValue();
        }

        ArrayValue value = new()
        {
            Value = new Value[group.Count]
        };

        int i = 0;

        foreach (var v in group.Values)
        {
            value.Value[i++] = new StringValue
            {
                Value = v
            };
        }

        return value;
    }

    private static Value StrLen(Value[] args)
    {
        if (args.Length != 1)
        {
            return new ErrorValue
            {
                Value = "Wrong number of arguments for the \"strlen\" command."
            };
        }

        var (bulkArgs, error) = VerifyArguments(args);
        if (error is not null)
        {
            return error;
        }

        if (!_sets.TryGetValue(bulkArgs[0].Value, out var value))
        {
            return new NullValue();
        }

        return new IntegerValue
        {
            Value = value.Length
        };
    }

    private static Value Incr(Value[] args)
    {
        if (args.Length != 1)
        {
            return new ErrorValue
            {
                Value = "Wrong number of arguments for the \"incr\" command."
            };
        }

        var (bulkArgs, error) = VerifyArguments(args);
        if (error is not null)
        {
            return error;
        }

        if (!_sets.TryGetValue(bulkArgs[0].Value, out var value)
            || !int.TryParse(value, out var current))
        {
            SetCustom("1");

            return new IntegerValue
            {
                Value = 1
            };
        }

        var incremented = current + 1;
        SetCustom(incremented.ToString());

        return new IntegerValue
        {
            Value = incremented
        };

        void SetCustom(string value)
        {
            BulkValue[] args = [
                bulkArgs[0],
                new()
                {
                    Value = value
                }
            ];

            Set(args);
        }
    }

    private static (BulkValue[], ErrorValue?) VerifyArguments(Value[] args)
    {
        var bulkArgs = new BulkValue[args.Length];

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is not BulkValue bulkArg)
            {
                ErrorValue error = new()
                {
                    Value = "Bulk value expected."
                };

                return ([], error);
            }

            bulkArgs[i] = bulkArg;
        }

        return (bulkArgs, null);
    }
}
