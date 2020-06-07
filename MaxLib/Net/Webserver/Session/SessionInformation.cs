using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver.Session
{
    public class SessionInformation
    {
        public string HexKey { get; }

        public byte[] ByteKey { get; }

        public DateTime Generated { get; }

        public Dictionary<object, object> Information { get; }

        public SessionInformation(string hexkey, byte[] bytekey, DateTime generated)
        {
            HexKey = hexkey;
            ByteKey = bytekey;
            Generated = generated;
            Information = new Dictionary<object, object>();
        }
    }
}
