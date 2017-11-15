using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Net.ServerClient
{
    [Serializable]
    public class Message : ILoadSaveAble
    {
        public int Reason { get; set; }
        public MessageRootCookie MessageRoot { get; set; }
        public MessageClientData ClientData { get; private set; }
        public byte[] HeaderBytes { get; set; }

        public Message()
        {
            MessageRoot = new MessageRootCookie();
            ClientData = new MessageClientData();
            ClientData.Owner = this;
            HeaderBytes = new byte[0];
        }

        public virtual void Load(byte[] data)
        {
            using (var m = new System.IO.MemoryStream(data))
            using (var r = new System.IO.BinaryReader(m))
            {
                Reason = r.ReadInt32();
                var count = r.ReadInt32();
                HeaderBytes = r.ReadBytes(count);
                count = r.ReadInt32();
                ClientData.Load(r.ReadBytes(count));
            }
        }

        public virtual byte[] Save()
        {
            using (var m = new System.IO.MemoryStream())
            using (var r = new System.IO.BinaryReader(m))
            using (var w = new System.IO.BinaryWriter(m))
            {
                w.Write(Reason);
                w.Write(HeaderBytes.Length);
                w.Write(HeaderBytes);
                var b = ClientData.Save();
                w.Write(b.Length);
                w.Write(b);
                m.Position = 0;
                return r.ReadBytes((int)m.Length);
            }
        }
    }

    [Serializable]
    public class MessageRootCookie
    {
        public int Connector { get; set; }
        public int RemoteId { get; set; }
        public Connection Connection { get; set; }

        public void SetFromUser(User user)
        {
            Connector = user.DefaultConnector;
            RemoteId = user.Id;
            Connection = user.DefaultConnection;
        }
    }

    [Serializable]
    public sealed class MessageClientData : ILoadSaveAble
    {
        public MessageClientDataType Type { get; private set; }
        public Message Owner { get; internal set; }
        public byte[] BinaryBytes { get; private set; }

        public T GetMessage<T>() where T : Message, new()
        {
            if (Type != MessageClientDataType.Message) throw new FormatException();
            var m = new T();
            m.Load(BinaryBytes);
            m.MessageRoot = Owner.MessageRoot;
            return m;
        }
        public void SetMessage<T>(T message) where T : Message
        {
            Type = MessageClientDataType.Message;
            BinaryBytes = message.Save();
            Owner.MessageRoot = message.MessageRoot;
        }

        public byte[] GetBinary()
        {
            if (Type != MessageClientDataType.Binary) throw new FormatException();
            return BinaryBytes;
        }
        public void SetBinary(byte[] data)
        {
            Type = MessageClientDataType.Binary;
            BinaryBytes = data;
        }

        public T GetLoadSaveAble<T>() where T : ILoadSaveAble, new()
        {
            if (Type != MessageClientDataType.LoadSaveAble) throw new FormatException();
            var ls = new T();
            ls.Load(BinaryBytes);
            return ls;
        }
        public void SetLoadSaveAble<T>(T data) where T : ILoadSaveAble
        {
            Type = MessageClientDataType.LoadSaveAble;
            BinaryBytes = data.Save();
        }

        public T GetSerializeAble<T>()
        {
            if (Type != MessageClientDataType.SerializeAble) throw new FormatException();
            using (var m = new System.IO.MemoryStream(BinaryBytes))
            {
                var formater = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)formater.Deserialize(m);
            }
        }
        public void SetSerializeAble<T>(T data)
        {
            Type = MessageClientDataType.SerializeAble;
            using (var m = new System.IO.MemoryStream())
            using (var r = new System.IO.BinaryReader(m))
            {
                var formater = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formater.Serialize(m, data);
                m.Position = 0;
                BinaryBytes = r.ReadBytes((int)m.Length);
            }
        }

        public void Load(byte[] data)
        {
            Type = (MessageClientDataType)data[0];
            BinaryBytes = data.ToList().GetRange(1, data.Length - 1).ToArray();
        }

        public byte[] Save()
        {
            var l = new List<byte>(BinaryBytes.Length + 1);
            l.Add((byte)Type);
            l.AddRange(BinaryBytes);
            return l.ToArray();
        }

        internal MessageClientData()
        {
            BinaryBytes = new byte[0];
        }
    }

    public enum MessageClientDataType : byte
    {
        Binary,
        Message,
        LoadSaveAble,
        SerializeAble
    }

    public class PrimaryMessage : Message
    {
        public PrimaryMessageType MessageType
        {
            get { return (PrimaryMessageType)base.Reason; }
            set { base.Reason = (int)value; }
        }

        public byte[] SenderID
        {
            get { return base.HeaderBytes; }
            set { base.HeaderBytes = value; }
        }

        public bool IgnoreProxy { get; set; }

        public PrimaryMessage()
            : base()
        {
            MessageType = PrimaryMessageType.NormalPush;
        }
    }

    public enum PrimaryMessageType : byte
    {
        NormalPush,
        Pipeline,
        WantToConnect,
        ConnectAllowed,
        ConnectFailed,
        ConnectFailed_FullServer,
        ConnectFailed_WrongKey,
        ConnectFailed_ExpectLogin,
        FileTransportServerToClient,
        FileTransportClientToServer,
        SyncFile,
        SyncData,
        Ping,
        PingAnswer,
        ProxySend,
        ProxyFetchList,
        ProxySendList
    }

    public interface iMessagePipeline
    {
        int GlobalId { get; }

        ConnectionManager Manager { get; set; }

        iMessagePipeline Owner { get; set; }

        void ComputeMessage(Message message);

        void SendMessage(Message message);
    }

    class PipelineMessage : Message
    {
        public PipelineMessage()
        {
            HeaderBytes = new byte[5];
        }

        public int ID
        {
            get
            {
                return BitConverter.ToInt32(HeaderBytes, 0);
            }
            set
            {
                var l = new List<byte>();
                l.AddRange(BitConverter.GetBytes(value));
                l.Add(HeaderBytes[4]);
                HeaderBytes = l.ToArray();
            }
        }

        public bool more
        {
            get
            {
                return BitConverter.ToBoolean(HeaderBytes, 4);
            }
            set
            {
                HeaderBytes[4] = BitConverter.GetBytes(value)[0];
            }
        }
    }

    public abstract class MessagePipelineBase : iMessagePipeline
    {
        public int GlobalId { get; protected set; }

        public ConnectionManager Manager { get; set; }

        public iMessagePipeline Owner { get; set; }

        public List<iMessagePipeline> Clients { get; private set; }

        public void ComputeMessage(Message message)
        {
            if (message is PipelineMessage)
            {
                var ppm = message as PipelineMessage;
                if (ppm.more)
                {
                    var ps = ppm.ClientData.GetMessage<PipelineMessage>();
                    var cl = Clients.Find((mp) => mp.GlobalId == ps.ID);
                    if (cl == null) MessageReceived(ppm);
                    else cl.ComputeMessage(ps);
                }
                else MessageReceived(ppm);
            }
            else
            {
                MessageReceived(message);
            }
        }

        public void SendMessage(Message message)
        {
            var ppm = new PipelineMessage();
            ppm.more = message is PipelineMessage;
            ppm.ID = GlobalId;
            ppm.MessageRoot = message.MessageRoot;
            ppm.ClientData.SetMessage(message);
            if (Owner!=null)
            {
                Owner.SendMessage(ppm);
            }
            else
            {
                var pm = new PrimaryMessage();
                pm.MessageType = PrimaryMessageType.Pipeline;
                pm.SenderID = Manager.CurrentId.Id;
                pm.ClientData.SetMessage(ppm);
                pm.MessageRoot = message.MessageRoot;
                Manager.SendMessage(pm);
            }
        }

        protected abstract void MessageReceived(Message message); //Daten sind in einer Message eingepackt!!!

        public MessagePipelineBase()
        {
            Clients = new List<iMessagePipeline>();
        }
    }
}
