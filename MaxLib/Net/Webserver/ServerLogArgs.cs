using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Net.Webserver
{
    public class ServerLogArgs : EventArgs
    {
        public ServerLogItem LogItem { get; }

        public bool Discard { get; set; } = false;

        public ServerLogArgs(ServerLogItem logItem)
            => LogItem = logItem;
    }
}
