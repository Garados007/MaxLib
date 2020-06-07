using System;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpResponseHeader : HttpHeader
    {
        public HttpStateCode StatusCode { get; set; } = HttpStateCode.OK;

        public string FieldLocation
        {
            get => HeaderParameter.TryGetValue("Location", out string value) ? value : null;
            set => HeaderParameter["Location"] = value;
        }

        public string FieldDate
        {
            get => HeaderParameter.TryGetValue("Date", out string value) ? value : null;
            set => HeaderParameter["Date"] = value;
        }

        public string FieldLastModified
        {
            get => HeaderParameter.TryGetValue("Last-Modified", out string value) ? value : null;
            set => HeaderParameter["Last-Modified"] = value;
        }

        public string FieldContentType
        {
            get => HeaderParameter.TryGetValue("Content-Type", out string value) ? value : null;
            set => HeaderParameter["Content-Type"] = value;
        }

        public virtual void SetActualDate()
        {
            FieldDate = WebServerUtils.GetDateString(DateTime.UtcNow);
        }
    }
}
