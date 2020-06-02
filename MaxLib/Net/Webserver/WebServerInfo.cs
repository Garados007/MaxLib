using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver
{
    public static class WebServerInfo
    {
        public static List<InfoTile> Information { get; } = new List<InfoTile>();
        public static List<Type> IgnoreSenderEvents { get; } = new List<Type>();

        public static event InformationReceivedHandler InformationReceived;

        static readonly object lockObjekt = new object();
        public static void Add(InfoTile tile)
        {
            tile.Date = DateTime.Now;
            lock (lockObjekt) { Information.Add(tile); }
            if (InformationReceived != null)
            {
                if (IgnoreSenderEvents.Exists((type) => type.AssemblyQualifiedName == tile.Sender.AssemblyQualifiedName)) return;
                InformationReceived(tile);
            }
        }

        public static void Add(InfoType type, Type sender, string infoType, string information)
        {
            Add(new InfoTile(type, sender, infoType, information));
        }

        public static void Add(InfoType type, Type sender, string infoType, object additionlData, string information)
        {
            Add(new InfoTile(type, sender, infoType, additionlData, information));
        }

        public static void Add(InfoType type, Type sender, string infoType, string mask, params object[] data)
        {
            Add(new InfoTile(type, sender, infoType, mask: mask, data: data));
        }

        public static void Add(InfoType type, Type sender, string infoType, object additionlData, string mask, params object[] data)
        {
            Add(new InfoTile(type, sender, infoType, additionlData, mask, data));
        }

        public static void Clear()
        {
            Information.Clear();
        }
    }
}
