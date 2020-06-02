using System.Linq;

namespace MaxLib.Net.Webserver.Services
{
    /// <summary>
    /// WebServiceType.PreCreateResponse: Erstellt den Response-Header und füllt diesen mit den wichtigsten Daten.
    /// </summary>
    public class HttpResponseCreator : WebService
    {
        /// <summary>
        /// WebServiceType.PreCreateResponse: Erstellt den Response-Header und füllt diesen mit den wichtigsten Daten.
        /// </summary>
        public HttpResponseCreator() : base(WebServiceType.PreCreateResponse) { }

        public override void ProgressTask(WebProgressTask task)
        {
            var request = task.Document.RequestHeader;
            var response = task.Document.ResponseHeader;
            response.FieldContentType = task.Document.PrimaryMime;
            response.SetActualDate();
            response.HttpProtocol = request.HttpProtocol;
            response.HeaderParameter["Connection"] = "keep-alive";
            response.HeaderParameter["X-UA-Compatible"] = "IE=Edge";
            response.HeaderParameter["Content-Length"] =
                task.Document.DataSources.Sum((s) => s.AproximateLength()).ToString();
            if (task.Document.PrimaryEncoding != null)
                response.HeaderParameter["Content-Type"] += "; charset=" +
                    task.Document.PrimaryEncoding;
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            return !task.Document.Information.ContainsKey("block default response creator");
        }
    }
}
