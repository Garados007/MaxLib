using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Api.Rest
{
    public class RestEndpoint
    {
        readonly private ApiRuleFactory.GroupRule rules = new ApiRuleFactory.GroupRule();

        public List<ApiRule> Rules => rules.Rules;

        public RestEndpoint Add(ApiRule rule)
        {
            if (rule != null)
                Rules.Add(rule);
            return this;
        }

        public RestEndpoint Add(IEnumerable<ApiRule> rules)
        {
            if (rules != null)
                Rules.AddRange(rules.Where(r => r != null));
            return this;
        }

        public RestEndpoint Add(params ApiRule[] rules)
        {
            if (rules != null)
                Rules.AddRange(rules.Where(r => r != null));
            return this;
        }

        public virtual RestQueryArgs Check(RestQueryArgs args)
        {
            _ = args ?? throw new ArgumentNullException(nameof(args));
            var realArgs = new RestQueryArgs(args.Location, args.GetArgs, args.Post);
            if (rules.Check(realArgs))
                return realArgs;
            else return null;
        }

        public virtual Task<HttpDataSource> GetSource(Dictionary<string, object> args)
        {
            _ = args ?? throw new ArgumentNullException(nameof(args));
            var sb = new StringBuilder();
            foreach (var kvp in args)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            return Task.FromResult<HttpDataSource>(new HttpStringDataSource(sb.ToString())); 
        }
    }
}
