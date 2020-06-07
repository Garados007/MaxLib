using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpSession
    {
        public byte[] SessionKey { get; set; }

        public string Ip { get; set; }

        public TcpClient NetworkClient { get; set; }

        public HttpStream NetworkStream { get; set; }

        public int LastWorkTime { get; set; }

        public Dictionary<object, object> SessionInformation { get; private set; } = new Dictionary<object, object>();

        public void AlwaysSyncSessionInformation(Dictionary<object, object> information)
            => SessionInformation = information ?? throw new ArgumentNullException(nameof(information));
    }
}
