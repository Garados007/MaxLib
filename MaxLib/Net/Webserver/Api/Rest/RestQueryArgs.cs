using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver.Api.Rest
{
    public class RestQueryArgs
    {
        public string[] Location { get; }

        public Dictionary<string, string> GetArgs { get; }

        public HttpPost Post { get; }

        public Dictionary<string, object> ParsedArguments { get; }

        public RestQueryArgs(string[] location, Dictionary<string, string> getArgs, HttpPost post)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
            GetArgs = getArgs ?? throw new ArgumentNullException(nameof(getArgs));
            Post = post ?? throw new ArgumentNullException(nameof(post));
            ParsedArguments = new Dictionary<string, object>();
        }
    }
}
