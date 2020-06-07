using System;

namespace MaxLib.Net.Webserver
{
    public class WebProgressTask : IDisposable
    {
        public HttpDocument Document { get; set; }

        public System.IO.Stream NetworkStream { get; set; }

        public WebServiceType NextTask { get; set; }

        public WebServiceType CurrentTask { get; set; }

        public WebServer Server { get; set; }

        public HttpSession Session { get; set; }

        public void Dispose()
        {
            Document?.Dispose();
        }
    }
}
