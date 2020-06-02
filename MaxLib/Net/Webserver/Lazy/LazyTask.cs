using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver.Lazy
{
    [Serializable]
    public class LazyTask
    {
        public WebServer Server { get; }

        public HttpSession Session { get; private set; }

        public HttpRequestHeader Header { get; private set; }

        public Dictionary<object, object> Information { get; private set; }
        
        public object this[object identifer]
        {
            get => Information[identifer];
            set => Information[identifer] = value;
        }

        public LazyTask(WebProgressTask task)
        {
            if (task == null) throw new ArgumentNullException("task");
            Server = task.Server;
            Session = task.Session;
            Header = task.Document.RequestHeader;
            Information = task.Document.Information;
        }
    }
}
