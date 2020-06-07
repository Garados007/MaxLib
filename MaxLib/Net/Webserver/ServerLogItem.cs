using System;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public readonly struct ServerLogItem
    {
        public DateTime Date { get; }

        public ServerLogType Type { get; }

        public string SenderType => SenderTypeMemory.ToString();

        public ReadOnlyMemory<char> SenderTypeMemory { get; }

        public string InfoType => InfoTypeMemory.ToString();

        public ReadOnlyMemory<char> InfoTypeMemory { get; }

        public string Information => InformationMemory.ToString();

        public ReadOnlyMemory<char> InformationMemory { get; }

        public ServerLogItem(DateTime date, ServerLogType type, Type sender, string infoType, string information)
        {
            Date = date;
            Type = type;
            SenderTypeMemory = (sender ?? throw new ArgumentNullException(nameof(sender))).FullName.AsMemory();
            InfoTypeMemory = infoType.AsMemory();
            InformationMemory = information.AsMemory();
        }

        public ServerLogItem(DateTime date, ServerLogType type, Type sender, string infoType, string mask, params object[] data)
            : this(date, type, sender, infoType, string.Format(mask, data))
        { }

        public ServerLogItem(ServerLogType type, Type sender, string infoType, string information)
            : this(DateTime.Now, type, sender, infoType, information)
        {
        }

        public ServerLogItem(ServerLogType type, Type sender, string infoType, string mask, params object[] data)
            : this(type, sender, infoType, string.Format(mask, data))
        { }


        public override string ToString()
        {
            if (Date == new DateTime())
                return $"[{Type}] {SenderType} : {InfoType} -> {Information}";
            else
                return $"[{Date}] [{Type}] {SenderType} : {InfoType} -> {Information}";
        }
    }
}
