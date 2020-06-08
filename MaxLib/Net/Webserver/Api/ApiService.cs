using System;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Api
{
    public abstract class ApiService : WebService
    {
        public ApiService(params string[] endpoint) 
            : base(WebServiceType.PreCreateDocument)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        private string[] endpoint;
        public string[] Endpoint
        {
            get => endpoint;
            set => endpoint = value ?? throw new ArgumentNullException(nameof(Endpoint));
        }

        public bool IgnoreCase { get; set; }

        protected abstract Task<HttpDataSource> HandleRequest(WebProgressTask task, string[] location);

        public override bool CanWorkWith(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));
            return task.Document.RequestHeader.Location.StartsUrlWith(endpoint, IgnoreCase);
        }

        public override async Task ProgressTask(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));
            var tiles = task.Document.RequestHeader.Location.DocumentPathTiles;
            var location = new string[tiles.Length - endpoint.Length];
            Array.Copy(tiles, endpoint.Length, location, 0, location.Length);
            var data = await HandleRequest(task, location);
            if (data != null)
                task.Document.DataSources.Add(data);
            else task.Document.ResponseHeader.StatusCode = HttpStateCode.InternalServerError;
        }
    }
}
