using System;

namespace MaxLib.Net.Webserver.Services
{
    /// <summary>
    /// WebServiceType.PostParseRequest: Verarbeitet die Aktion HEAD oder OPTIONS, die vom Browser angefordert wurde
    /// </summary>
    public class HttpHeaderSpecialAction : WebService
    {
        /// <summary>
        /// WebServiceType.PostParseRequest: Verarbeitet die Aktion HEAD oder OPTIONS, die vom Browser angefordert wurde
        /// </summary>
        public HttpHeaderSpecialAction() : base(WebServiceType.PostParseRequest) { }

        public override void ProgressTask(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));

            switch (task.Document.RequestHeader.ProtocolMethod)
            {
                case HttpProtocollMethod.Head:
                    task.Document.Information["Only Header"] = true;
                    break;
                case HttpProtocollMethod.Options:
                    {
                        var source = new HttpStringDataSource("GET\r\nPOST\r\nHEAD\r\nOPTIONS\r\nTRACE")
                        {
                            MimeType = MimeType.TextPlain,
                            TransferCompleteData = true
                        };
                        task.Document.DataSources.Add(source);
                        task.NextTask = WebServiceType.PreParseRequest;
                    }
                    break;
            }
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));

            switch (task.Document.RequestHeader.ProtocolMethod)
            {
                case HttpProtocollMethod.Head: return true;
                case HttpProtocollMethod.Options: return true;
                default: return false;
            }
        }
    }
}
