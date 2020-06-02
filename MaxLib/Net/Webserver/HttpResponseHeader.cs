using System;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpResponseHeader : HttpHeader
    {
        public HttpStateCode StatusCode { get; set; } = HttpStateCode.OK;

        public string FieldLocation
        {
            get => HeaderParameter["Location"];
            set => HeaderParameter["Location"] = value;
        }

        public string FieldDate
        {
            get => HeaderParameter["Date"];
            set => HeaderParameter["Date"] = value;
        }

        public string FieldLastModified
        {
            get => HeaderParameter["Last-Modified"];
            set => HeaderParameter["Last-Modified"] = value;
        }

        public string FieldContentType
        {
            get => HeaderParameter["Content-Type"];
            set => HeaderParameter["Content-Type"] = value;
        }

        public virtual void SetActualDate()
        {
            FieldDate = WebServerHelper.GetDateString(DateTime.UtcNow);
        }
    }
}
