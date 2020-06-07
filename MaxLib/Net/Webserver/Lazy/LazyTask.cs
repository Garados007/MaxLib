using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver.Lazy
{
    [Serializable]
    public class LazyTask
    {
        public WebServer Server { get; }

        public HttpSession Session { get; }

        public HttpRequestHeader Header { get; }

        public Dictionary<object, object> Information { get; }
        
        public object this[object identifer]
        {
            get => Information[identifer];
            set => Information[identifer] = value;
        }

        public LazyTask(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));
            Server = task.Server;
            Session = task.Session;
            Header = task.Document.RequestHeader;
            Information = task.Document.Information;
        }
    }
}
