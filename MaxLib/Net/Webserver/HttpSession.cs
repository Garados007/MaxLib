using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpSession
    {
        public long InternalSessionKey { get; set; }

        public byte[] PublicSessionKey { get; set; }

        public string Ip { get; set; }

        public TcpClient NetworkClient { get; set; }

        public Stream NetworkStream { get; set; }

        public int LastWorkTime { get; set; }

        private Dictionary<object, object> sessionInformation = new Dictionary<object, object>();
        public Dictionary<object, object> SessionInformation
        {
            get { return sessionInformation; }
        }

        public void AlwaysSyncSessionInformation(Dictionary<object, object> information)
        {
            sessionInformation = information;
        }
    }
}
