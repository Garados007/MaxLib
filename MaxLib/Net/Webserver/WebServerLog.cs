using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver
{
    public static class WebServerLog
    {
        public static List<ServerLogItem> ServerLog { get; } = new List<ServerLogItem>();
        public static List<Type> IgnoreSenderEvents { get; } = new List<Type>();

        /// <summary>
        /// This event fires if some log item should be added. The log item can now filtered and discarded.
        /// </summary>
        public static event ServerLogAddedHandler LogPreAdded;

        /// <summary>
        /// This event fires after a log item is added.
        /// </summary>
        public static event Action<ServerLogItem> LogAdded;

        static readonly object lockObjekt = new object();
        public static void Add(ServerLogItem logItem)
        {
            if (IgnoreSenderEvents.Exists((type) => type.FullName== logItem.SenderType)) 
                return;
            var eventArgs = new ServerLogArgs(logItem);
            LogPreAdded?.Invoke(eventArgs);
            if (eventArgs.Discard)
                return;
            lock (lockObjekt) 
                ServerLog.Add(logItem);
            LogAdded?.Invoke(logItem);
        }

        public static void Add(ServerLogType type, Type sender, string infoType, string information)
        {
            Add(new ServerLogItem(type, sender, infoType, information));
        }

        public static void Add(ServerLogType type, Type sender, string infoType, string mask, params object[] data)
        {
            Add(new ServerLogItem(type, sender, infoType, mask: mask, data: data));
        }

        public static void Clear()
        {
            ServerLog.Clear();
        }
    }
}
