using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Api.Rest
{
    public class RestApiService : ApiService
    {
        public List<RestEndpoint> RestEndpoints { get; } = new List<RestEndpoint>();

        public RestApiService(params string[] endpoint)
            : base(endpoint)
        { }

        protected override Task<HttpDataSource> HandleRequest(WebProgressTask task, string[] location)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));
            _ = location ?? throw new ArgumentNullException(nameof(location));
            var query = GetQueryArgs(task, location);
            foreach (var endpoint in RestEndpoints)
            {
                if (endpoint == null)
                    continue;
                var q = endpoint.Check(query);
                if (q == null)
                    continue;
                return endpoint.GetSource(q.ParsedArguments);
            }
            return Task.FromResult(NoEndpoint(task, query));
        }

        protected virtual RestQueryArgs GetQueryArgs(WebProgressTask task, string[] location)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));
            _ = location ?? throw new ArgumentNullException(nameof(location));
            return new RestQueryArgs(location, task.Document.RequestHeader.Location.GetParameter, task.Document.RequestHeader.Post);
        }

        protected virtual HttpDataSource NoEndpoint(WebProgressTask task, RestQueryArgs args)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));
            _ = args ?? throw new ArgumentNullException(nameof(args));
            task.Document.ResponseHeader.StatusCode = HttpStateCode.NotFound;
            return new HttpStringDataSource("no endpoint");
        }
    }
}
