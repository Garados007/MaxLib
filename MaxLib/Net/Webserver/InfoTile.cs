using System;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class InfoTile
    {
        public DateTime Date { get; internal set; }

        public InfoType Type { get; private set; }

        public Type Sender { get; private set; }

        public string InfoType { get; private set; }

        public string Information { get; private set; }

        public object AdditionalData { get; }

        public InfoTile(InfoType type, Type sender, string infoType, string information)
        {
            Type = type;
            Sender = sender;
            InfoType = infoType;
            Information = information;
        }

        public InfoTile(InfoType type, Type sender, string infoType, object additionlData, string information)
            : this(type, sender, infoType, information)
        {
            AdditionalData = additionlData;
        }

        public InfoTile(InfoType type, Type sender, string infoType, string mask, params object[] data)
            : this(type, sender, infoType, string.Format(mask, data))
        { }

        public InfoTile(InfoType type, Type sender, string infoType, object additionlData, string mask, params object[] data)
            : this(type, sender, infoType, additionlData, string.Format(mask, data))
        { }

        public override string ToString()
        {
            if (Date == new DateTime())
                return string.Format("[{0}] {1} : {2} -> {3}",
                    Type, Sender.FullName, InfoType, Information);
            else
                return string.Format("[{4}] [{0}] {1} : {2} -> {3}",
                    Type, Sender.FullName, InfoType, Information, Date);
        }
    }
}
