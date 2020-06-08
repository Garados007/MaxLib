using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Net.Webserver.Api.Rest
{
    public class ApiRuleFactory
    {
        public class UrlConstantRule : ApiLocationRule
        {
            public string Constant { get; set; }

            public bool IgnoreCase { get; set; } = false;

            public override bool Check(RestQueryArgs args)
            {
                _ = args ?? throw new ArgumentNullException(nameof(args));
                if (args.Location.Length <= Index)
                    return false;
                var comparison = IgnoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
                return string.Equals(Constant, args.Location[Index], comparison);
            }
        }

        public class UrlArgumentRule<T> : ApiLocationRule
        {
            public delegate bool ParseArgumentHandle(string variable, out T value);

            public ParseArgumentHandle ParseArgument { get; set; }

            public string Key { get; set; }

            public override bool Check(RestQueryArgs args)
            {
                _ = args ?? throw new ArgumentNullException(nameof(args));
                if (args.Location.Length <= Index)
                    return false;
                if (ParseArgument == null)
                    return false;
                if (!ParseArgument(args.Location[Index], out T value))
                    return false;
                args.ParsedArguments[Key] = value;
                return true;
            }
        }

        public class MaxLengthRule : ApiLocationRule
        {
            public override bool Check(RestQueryArgs args)
            {
                _ = args ?? throw new ArgumentNullException(nameof(args));
                return args.Location.Length <= Index;
            }
        }

        public class KeyExistsRule : ApiGetRule
        {
            public override bool Check(RestQueryArgs args)
            {
                _ = args ?? throw new ArgumentNullException(nameof(args));
                return args.GetArgs.ContainsKey(Key);
            }
        }

        public class GetArgumentRule<T> : ApiGetRule
        {
            public delegate bool ParseArgumentHandle(string variable, out T value);

            public ParseArgumentHandle ParseArgument { get; set; }

            public override bool Check(RestQueryArgs args)
            {
                _ = args ?? throw new ArgumentNullException(nameof(args));
                if (!args.GetArgs.TryGetValue(Key, out string strValue))
                    return false;
                if (ParseArgument == null)
                    return false;
                if (!ParseArgument(strValue, out T value))
                    return false;
                args.ParsedArguments[Key] = value;
                return true;
            }
        }

        public class GroupRule : ApiRule
        {
            public List<ApiRule> Rules { get; } = new List<ApiRule>();

            public override bool Check(RestQueryArgs args)
            {
                _ = args ?? throw new ArgumentNullException(nameof(args));
                foreach (var rule in Rules)
                {
                    if (rule == null)
                        continue;
                    var success = rule.Check(args);
                    if (!success && rule.Required)
                        return false;
                }
                return true;
            }
        }

        public class ConditionalRule : ApiRule
        {
            public ApiRule Condition { get; set; }

            public ApiRule Success { get; set; }

            public ApiRule Fail { get; set; }

            public override bool Check(RestQueryArgs args)
            {
                _ = args ?? throw new ArgumentNullException(nameof(args));
                if (Condition?.Check(args) ?? false)
                {
                    return Success != null && (Success.Check(args) || !Success.Required);
                }
                else
                {
                    return Fail != null && (Fail.Check(args) || !Fail.Required);
                }
            }
        }

        public UrlConstantRule UrlConstant(string constant, bool ignoreCase = false, int index = 0)
        {
            return new UrlConstantRule
            {
                Index = index,
                Constant = constant,
                IgnoreCase = ignoreCase,
            };
        }

        public UrlArgumentRule<T> UrlArgument<T>(string key, UrlArgumentRule<T>.ParseArgumentHandle parseArgument, int index = 0)
        {
            return new UrlArgumentRule<T>
            {
                Index = index,
                Key = key,
                ParseArgument = parseArgument,
            };
        }

        public UrlArgumentRule<string> UrlArgument(string key, int index = 0)
        {
            return new UrlArgumentRule<string>
            {
                Index = index,
                Key = key,
                ParseArgument = (string val, out string v) =>
                {
                    v = val;
                    return true;
                },
            };
        }

        public MaxLengthRule MaxLength(int maxLength = 0)
        {
            return new MaxLengthRule
            {
                Index = maxLength,
            };
        }

        public KeyExistsRule KeyExists(string key)
        {
            return new KeyExistsRule
            {
                Key = key,
            };
        }

        public GetArgumentRule<T> GetArgument<T>(string key, GetArgumentRule<T>.ParseArgumentHandle parseArgument)
        {
            return new GetArgumentRule<T>
            {
                Key = key,
                ParseArgument = parseArgument,
            };
        }

        public GetArgumentRule<string> GetArgument(string key)
        {
            return new GetArgumentRule<string>
            {
                Key = key,
                ParseArgument = (string value, out string v) =>
                {
                    v = value;
                    return true;
                },
            };
        }

        public GroupRule Group(params ApiRule[] rules)
        {
            var group = new GroupRule();
            if (rules != null)
                group.Rules.AddRange(rules);
            return group;
        }

        public GroupRule Location(params ApiLocationRule[] rules)
            => Location(0, rules);

        public GroupRule Location(int offset, params ApiLocationRule[] rules)
        {
            var group = new GroupRule();
            if (rules == null)
                return group;
            group.Rules.AddRange(rules.Select((r, ind) =>
            {
                r.Index = offset + ind;
                return r;
            }));
            return group;
        }

        public T Optional<T>(T rule)
            where T : ApiRule
        {
            if (rule is null)
                return null;
            rule.Required = false;
            return rule;
        }

        public ConditionalRule Conditional(ApiRule condition, ApiRule success, ApiRule fail)
        {
            return new ConditionalRule
            {
                Condition = condition,
                Success = success,
                Fail = fail,
            };
        }
    }
}
