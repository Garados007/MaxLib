using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public abstract class HttpHeader
    {
        private string httpProtocol = HttpProtocollDefinition.HttpVersion1_1;
        public string HttpProtocol
        {
            get => httpProtocol;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) 
                    throw new ArgumentException("HttpProtocol cannot contain an empty Protocol", nameof(HttpProtocol));
                httpProtocol = value;
            }
        }

        public Dictionary<string, string> HeaderParameter { get; } = new Dictionary<string, string>();

        private string protocolMethod = HttpProtocollMethod.Get;
        public string ProtocolMethod
        {
            get => protocolMethod;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) 
                    throw new ArgumentException("ProtocolMethod cannot be empty", nameof(ProtocolMethod));
                protocolMethod = value;
            }
        }
    }
}
