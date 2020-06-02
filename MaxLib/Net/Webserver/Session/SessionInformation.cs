using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver.Session
{
    public class SessionInformation
    {
        public string HexKey { get; private set; }

        public byte[] ByteKey { get; private set; }

        public DateTime Generated { get; private set; }

        public Dictionary<object, object> Information { get; private set; }

        public SessionInformation(string hexkey, byte[] bytekey, DateTime generated)
        {
            HexKey = hexkey;
            ByteKey = bytekey;
            Generated = generated;
            Information = new Dictionary<object, object>();
        }
    }
}
