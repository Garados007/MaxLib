using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver.Session
{
    public class SessionInformation
    {
        public byte[] Key { get; }

        public DateTime Generated { get; }

        public Dictionary<object, object> Information { get; }

        public SessionInformation( byte[] bytekey, DateTime generated)
        {
            Key = bytekey;
            Generated = generated;
            Information = new Dictionary<object, object>();
        }
    }
}
