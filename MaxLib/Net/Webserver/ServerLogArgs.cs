using System;

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
