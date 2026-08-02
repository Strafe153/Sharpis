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
            { Commands.Incr, Incr },
            { Commands.IncrBy, IncrBy },
            { Commands.Exists, Exists },
            { Commands.HExists, Hexists },
        };

    public static Dictionary<string, Func<Value[], Value>> Handlers => _handlers;

    private static Value Handle(
        Func<Value[], Value?> validationFunc,
        Func<BulkValue[], Value> valueFunc,
        Value[] args)
    {
        var error = validationFunc(args);
        if (error is not null)
        {
            return error;
        }

        var bulkArgs = new BulkValue[args.Length];

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is not BulkValue bulkArg)
            {
                ErrorValue errValue = new()
                {
                    Value = "Bulk value expected."
                };

                return errValue;
            }

            bulkArgs[i] = bulkArg;
        }

        return valueFunc(bulkArgs);
    }

    private static Value Pong(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length == 0)
            {
                return new StringValue
                {
                    Value = "PONG"
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args) =>
            new StringValue
            {
                Value = args[0].Value
            };

        return Handle(validationFunc, valueFunc, args);
    }

    private static Value Set(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length != 2)
            {
                return new ErrorValue
                {
                    Value = "Wrong number of arguments for the \"set\" command."
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args)
        {
            var key = args[0].Value;
            var value = args[1].Value;

            _sets[key] = value;

            return new StringValue
            {
                Value = "OK"
            };
        }

        return Handle(validationFunc, valueFunc, args);
    }

    private static Value Get(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length != 1)
            {
                return new ErrorValue
                {
                    Value = "Wrong number of arguments for the \"get\" command."
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args)
        {
            if (!_sets.TryGetValue(args[0].Value, out var value))
            {
                return new NullValue();
            }

            return new StringValue
            {
                Value = value
            };
        }

        return Handle(validationFunc, valueFunc, args);
    }

    private static Value Hset(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length != 3)
            {
                return new ErrorValue
                {
                    Value = "Wrong number of arguments for the \"hset\" command."
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args)
        {
            var groupKey = args[0].Value;
            var key = args[1].Value;
            var value = args[2].Value;

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

        return Handle(validationFunc, valueFunc, args);
    }

    private static Value Hget(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length != 2)
            {
                return new ErrorValue
                {
                    Value = "Wrong number of arguments for the \"hget\" command."
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args)
        {
            var groupKey = args[0].Value;
            var key = args[1].Value;

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

        return Handle(validationFunc, valueFunc, args);
    }

    private static Value HgetAll(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length != 1)
            {
                return new ErrorValue
                {
                    Value = "Wrong number of arguments for the \"hgetall\" command."
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args)
        {
            if (!_hSets.TryGetValue(args[0].Value, out var group))
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

        return Handle(validationFunc, valueFunc, args);
    }

    private static Value StrLen(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length != 1)
            {
                return new ErrorValue
                {
                    Value = "Wrong number of arguments for the \"strlen\" command."
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args)
        {
            if (!_sets.TryGetValue(args[0].Value, out var value))
            {
                return new NullValue();
            }

            return new IntegerValue
            {
                Value = value.Length
            };
        }

        return Handle(validationFunc, valueFunc, args);
    }

    private static Value Incr(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length != 1)
            {
                return new ErrorValue
                {
                    Value = "Wrong number of arguments for the \"incr\" command."
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args)
        {
            Value[] incrByArgs = [
                .. args,
                new BulkValue
                {
                    Value = "1"
                }
            ];

            return IncrBy(incrByArgs);
        }

        return Handle(validationFunc, valueFunc, args);
    }

    private static Value IncrBy(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length != 2)
            {
                return new ErrorValue
                {
                    Value = "Wrong number of arguments for the \"incrby\" command."
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args)
        {
            if (!int.TryParse(args[1].Value, out var increment))
            {
                return new ErrorValue
                {
                    Value = "Value is not an integer or is out of range"
                };
            }

            if (!_sets.TryGetValue(args[0].Value, out var value)
                || !int.TryParse(value, out var current))
            {
                SetCustom(increment.ToString());

                return new IntegerValue
                {
                    Value = increment
                };
            }

            var incremented = current + increment;
            SetCustom(incremented.ToString());

            return new IntegerValue
            {
                Value = incremented
            };

            void SetCustom(string value)
            {
                BulkValue[] newArgs = [
                    args[0],
                    new()
                    {
                        Value = value
                    }
                ];

                Set(newArgs);
            }
        }

        return Handle(validationFunc, valueFunc, args);
    }

    private static Value Exists(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length != 1)
            {
                return new ErrorValue
                {
                    Value = "Wrong number of arguments for the \"exists\" command."
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args)
        {
            var exists = _sets.TryGetValue(args[0].Value, out _) ? 1 : 0;

            return new IntegerValue
            {
                Value = exists
            };
        }

        return Handle(validationFunc, valueFunc, args);
    }

    private static Value Hexists(Value[] args)
    {
        static Value? validationFunc(Value[] args)
        {
            if (args.Length != 2)
            {
                return new ErrorValue
                {
                    Value = "Wrong number of arguments for the \"hexists\" command."
                };
            }

            return null;
        }

        static Value valueFunc(BulkValue[] args)
        {
            var exists = 1;

            if (!_hSets.TryGetValue(args[0].Value, out var set)
                || !set.TryGetValue(args[1].Value, out _))
            {
                exists = 0;
            }

            return new IntegerValue
            {
                Value = exists
            };
        }

        return Handle(validationFunc, valueFunc, args);
    }
}
