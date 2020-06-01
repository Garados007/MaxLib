using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Net.ServerClient
{
    public abstract class Connector
    {
        public virtual int ConnectorId { get; internal set; }

        public ConnectionList Connections { get; private set; }

        public virtual int MaxConnectionsCount
        {
            get => Connections.MaxCount;
            protected set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("MaxConnectionsCount");
                while (value < Connections.Count) Connections.Remove(Connections.connections.ElementAt(Connections.Count - 1));
                Connections.MaxCount = value;
            }
        }

        protected bool CanChangeProtocol
        {
            get => Connections.CanChangeProtocol;
            set => Connections.CanChangeProtocol = value;
        }

        public Connector()
        {
            Connections = new ConnectionList();
            ProgressRun = false;
            Connections.ConAdd += ConnectionAdded;
            Connections.ConRem += ConnectionRemoved;
        }

        protected virtual void ConnectionRemoved(Connection connection)
        {
            
        }

        protected virtual void ConnectionAdded(Connection connection)
        {
            
        }

        public abstract void StartProgress();
        public abstract void StopProgress();
        internal protected bool ProgressRun { get; internal set; }

        public ConnectionManager Manager { get; internal set; }

        public virtual void SendMessage(PrimaryMessage message)
        {

        }

        protected void MessageReceived(PrimaryMessage message)
        {
            message.MessageRoot.Connector = ConnectorId;
            if (Manager != null) Manager.ReceiveMessage(message);
            MesRec?.Invoke(message);
        }

        internal event Action<PrimaryMessage> MesRec;

        protected void SetUsedState(Connection con, bool used)
        {
            Connections[con] = used;
        }

        public event EventHandler<ConnectionLostEventArgument> ConnectionLost;

        protected ConnectionLostEventArgument DoConnectionLost(ConnectionLostEventArgument argument)
        {
            ConnectionLost?.Invoke(this, argument);
            if (Manager != null) Manager.DoConnectionLost(argument);
            return argument;
        }
    }

    [Serializable]
    public class Connection : System.Runtime.Serialization.ISerializable
    {
        public ConnectorProtocol Protocol { get; private set; }

        public int Port { get; private set; }

        public string Target { get; private set; }

        public Connection()
        {
            Protocol = ConnectorProtocol.TCP;
            Port = 0;
            Target = "";
        }

        public Connection(ConnectorProtocol protocol, int port)
        {
            if (protocol == ConnectorProtocol.Unknown) throw new WrongProtocolException();
            Protocol = protocol;
            Port = port;
            Target = "";
        }

        public static Connection Create(ConnectorProtocol protocol, int port)
        {
            return new Connection(protocol, port);
        }

        public static Connection[] Create(ConnectorProtocol protocol, int portRangeStart, int portRangeEnd)
        {
            var c = new List<Connection>();
            for (int p = portRangeStart; p <= portRangeEnd; ++p) c.Add(new Connection(protocol, p));
            return c.ToArray();
        }

        public Connection(ConnectorProtocol protocol, int port, string target)
        {
            if (protocol == ConnectorProtocol.Unknown) throw new WrongProtocolException();
            Protocol = protocol;
            Port = port;
            Target = target;
        }

        public static Connection Create(ConnectorProtocol protocol, int port, string target)
        {
            return new Connection(protocol, port, target);
        }

        public static Connection[] Create(ConnectorProtocol protocol, int portRangeStart, int portRangeEnd, string target)
        {
            var c = new List<Connection>();
            for (int p = portRangeStart; p <= portRangeEnd; ++p) c.Add(new Connection(protocol, p, target));
            return c.ToArray();
        }

        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            info.AddValue("protocol", (byte)Protocol);
            info.AddValue("port", Port);
            info.AddValue("target", Target, typeof(string));
        }

        public Connection(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            Protocol = (ConnectorProtocol)info.GetByte("protocol");
            Port = info.GetInt32("port");
            Target = info.GetValue("target", typeof(string)).ToString();
        }

        public static bool operator ==(Connection c1, Connection c2)
        {
            if (c1 is null && c2 is null) return true;
            if (c1 is null || c2 is null) return false;
            return c1.Port == c2.Port && c1.Protocol == c2.Protocol && c1.Target == c2.Target;
        }

        public static bool operator !=(Connection c1, Connection c2)
        {
            return !(c1 == c2);
        }

        public override bool Equals(object obj)
        {
            if (obj is Connection) return this == (Connection)obj;
            else return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public enum ConnectorProtocol
    {
        Unknown,
        TCP,
        UDP
    }

    [Serializable]
    public class WrongProtocolException : Exception
    {
        public WrongProtocolException() { }
        public WrongProtocolException(string message) : base(message) { }
        public WrongProtocolException(string message, Exception inner) : base(message, inner) { }
        protected WrongProtocolException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class FullConnectionListException : Exception
    {
        public FullConnectionListException() { }
        public FullConnectionListException(string message) : base(message) { }
        public FullConnectionListException(string message, Exception inner) : base(message, inner) { }
        protected FullConnectionListException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    public sealed class ConnectionList // : IDictionary<Connection, bool>
    {
        internal ConnectionList()
        {

        }

        internal Dictionary<Connection, bool> connections = new Dictionary<Connection, bool>();

        internal int MaxCount = int.MaxValue;
        internal bool CanChangeProtocol = true;
        private ConnectorProtocol mainProtocol = ConnectorProtocol.Unknown;
        public ConnectorProtocol MainProtocol
        {
            get => mainProtocol;
            set => SetProtocol(value, true);
        }
        internal event Action<Connection> ConAdd, ConRem;

        public Connection GetFree()
        {
            for (int i = 0; i < Count; ++i) if (!this[this[i]]) return this[i];
            return null;
        }

        public void SetProtocol(ConnectorProtocol protocol, bool removeWrongConnections)
        {
            if (!CanChangeProtocol) throw new FieldAccessException();
            mainProtocol = ConnectorProtocol.Unknown;
            if (protocol == ConnectorProtocol.Unknown) return;
            if (removeWrongConnections) for (int i = 0; i<Count; ++i)
                {
                    if (connections.ElementAt(i).Key.Protocol!=protocol)
                    {
                        Remove(connections.ElementAt(i));
                        --i;
                    }
                }
        }

        public void Add(Connection key, bool value)
        {
            if (isReadOnly) throw new AccessViolationException();
            if (MainProtocol != ConnectorProtocol.Unknown && key.Protocol != MainProtocol)
                throw new WrongProtocolException();
            if (Count >= MaxCount) throw new FullConnectionListException();
            connections.Add(key, value);
            ConAdd(key);
        }

        public bool ContainsKey(Connection key)
        {
            return connections.ContainsKey(key);
        }

        public ICollection<Connection> Keys
        {
            get { return connections.Keys; }
        }

        public bool Remove(Connection key)
        {
            if (isReadOnly) throw new AccessViolationException();
            ConRem(key);
            return connections.Remove(key);
        }

        public bool TryGetValue(Connection key, out bool value)
        {
            return connections.TryGetValue(key, out value);
        }

        public Connection FindConnection(int port)
        {
            for (int i = 0; i < Count; ++i) if (connections.ElementAt(i).Key.Port == port) return connections.ElementAt(i).Key;
            return null;
        }

        internal bool this[Connection key]
        {
            get => connections[key];
            set
            {
                if (isReadOnly) throw new AccessViolationException();
                connections[key] = value;
            }
        }
        public Connection this[int index]
        {
            get
            {
                return connections.ElementAt(index).Key;
            }
        }

        public void Add(KeyValuePair<Connection, bool> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            if (isReadOnly) throw new AccessViolationException();
            foreach (var c in connections) ConRem(c.Key);
            connections.Clear();
        }

        public bool Contains(KeyValuePair<Connection, bool> item)
        {
            return connections.Contains(item);
        }

        public void CopyTo(KeyValuePair<Connection, bool>[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; ++i) array[arrayIndex + i] = connections.ElementAt(i);
        }

        public int Count
        {
            get { return connections.Count; }
        }

        bool isReadOnly = false;
        public bool IsReadOnly
        {
            get => isReadOnly;
            set => isReadOnly = value;
        }

        public bool Remove(KeyValuePair<Connection, bool> item)
        {
            if (isReadOnly) throw new AccessViolationException();
            ConRem(item.Key);
            return connections.Remove(item.Key);
        }

        public void Add(Connection connection)
        {
            Add(connection, false);
        }

        public void Add(ConnectorProtocol protocol, int port)
        {
            Add(new Connection(protocol, port));
        }

        public void Add(Connection[] connections)
        {
            foreach (var con in connections) Add(con);
        }

        public void Add(ConnectorProtocol protocol, int portRangeStart, int portRangeEnd)
        {
            Add(Connection.Create(protocol, portRangeStart, portRangeEnd));
        }
    }

    [Serializable]
    public class ConnectionLostEventArgument : EventArgs
    {
        public Connector Connector { get; private set; }

        public Connection Connection { get; private set; }

        public bool Retry { get; set; }

        public bool UserLogout { get; set; }

        public Message SendedMessage { get; private set; }

        public ConnectionLostEventArgument(Connector connector, Connection connection, Message sendedMessage)
        {
            Connector = connector;
            Connection = connection;
            SendedMessage = sendedMessage;
            Retry = false;
            UserLogout = true;
        }
    }

    public class ConnectionManager : EventArgs
    {
        readonly List<Connector> connectors = new List<Connector>();

        int FindFreeId()
        {
            int id = 0;
            while (connectors.Exists((cn) => cn.ConnectorId == id)) id++;
            return id;
        }

        public void AddConnector(Connector connector)
        {
            if (connector == null) throw new ArgumentNullException("connector");
            connector.Manager = this;
            connector.ConnectorId = FindFreeId();
            connectors.Add(connector);
        }

        public void RemoveConnector(Connector connector)
        {
            if (connector == null) throw new ArgumentNullException("connector");
            connector.Manager = null;
            connector.ConnectorId = -1;
            connectors.Remove(connector);
        }

        public Connector FindConnector(int id)
        {
            return connectors.Find((c) => c.ConnectorId == id);
        }

        public Connector[] GetAllConnectors()
        {
            return connectors.ToArray();
        }

        private Connector defaultLogin = null;
        public Connector DefaultLogin
        {
            get => defaultLogin;
            set => defaultLogin = value;
        }

        private Connector defaultFileTransport = null;
        public Connector DefaultFileTransport
        {
            get => defaultFileTransport;
            set => defaultFileTransport = value;
        }

        private Connector defaultDataTransport = null;
        public Connector DefaultDataTransport
        {
            get => defaultDataTransport;
            set => defaultDataTransport = value;
        }

        internal Connectors.Sync.SyncFileManager SyncFile = null;
        internal List<AutoSync.SyncManager> SyncData = new List<AutoSync.SyncManager>();

        public void SendPushMessage(Message message)
        {
            var pm = new PrimaryMessage
            {
                MessageType = PrimaryMessageType.NormalPush
            };
            pm.ClientData.SetMessage(message);
            SendMessage(pm);
        }

        public void SendMessage(PrimaryMessage message)
        {
            message.SenderID = CurrentId.Id;
            if (Proxy.IsProxyRequired(message)) Proxy.SendMessageToProxy(message);
            else connectors.Find((c) => c.ConnectorId == message.MessageRoot.Connector).SendMessage(message);
        }

        public void ReceiveMessage(PrimaryMessage message)
        {
            var t = new System.Threading.Tasks.Task(() =>
                {
                    switch (message.MessageType)
                    {
                        case PrimaryMessageType.NormalPush:
                            PushMessageReceived?.Invoke(message.ClientData.GetMessage<Message>());
                            break;
                        case PrimaryMessageType.Pipeline:
                            var pipe = MessagePipelines.Find((mp) => mp.GlobalId == message.ClientData.GetMessage<PipelineMessage>().ID);
                            if (pipe != null) pipe.ComputeMessage(message.ClientData.GetMessage<PipelineMessage>());
                            break;
                        case PrimaryMessageType.FileTransportClientToServer:
                            if (defaultFileTransport != null) defaultFileTransport.SendMessage(message);
                            break;
                        case PrimaryMessageType.FileTransportServerToClient:
                            if (defaultFileTransport != null) defaultFileTransport.SendMessage(message);
                            break;
                        case PrimaryMessageType.SyncFile:
                            if (SyncFile!=null)
                            {
                                var mes = message.ClientData.GetMessage<Connectors.Sync.SyncFileMessage>();
                                SyncFile.MessageReceived(mes);
                            } break;
                        case PrimaryMessageType.SyncData:
                            {
                                var sd = message.ClientData.GetMessage<AutoSync.SyncMessage>();
                                var dat = sd.ClientData.GetSerializeAble<AutoSync.SyncMessageData>();
                                var man = SyncData.Find((sm) => sm.Id == dat.SyncManagerId);
                                if (man != null) man.MessageReceived(sd);
                            } break;
                        case PrimaryMessageType.Ping:
                            {
                                message.MessageType = PrimaryMessageType.PingAnswer;
                                SendMessage(message);
                            } break;
                        case PrimaryMessageType.PingAnswer:
                            {
                                var time = BitConverter.ToInt32(message.ClientData.GetBinary(), 0);
                                var user = Users.GetUserFromId(message.SenderID);
                                if (user!=null)
                                {
                                    user.Ping = Environment.TickCount - time;
                                }
                            } break;
                        case PrimaryMessageType.ProxySend:
                            {
                                Proxy.ReceivedMessageAsProxy(message);
                            } break;
                    }
                });
            t.Start();
        }

        public UserCollection Users { get; private set; }

        public List<IMessagePipeline> MessagePipelines { get; private set; }

        public CurrentIdentification CurrentId { get; private set; }

        public event MessageReceivedHandle PushMessageReceived;

        private Proxy proxy;
        public Proxy Proxy
        {
            get => proxy;
            set => proxy = value ?? throw new ArgumentNullException("Proxy");
        }

        private int defaultPingTime = 1000;
        public int DefaultPingTime
        {
            get => defaultPingTime;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException();
                defaultPingTime = value;
            }
        }

        public event EventHandler<ConnectionLostEventArgument> ConnectionLost;

        private int autoReconectTime = 15000;

        public int AutoReconectTime
        {
            get => autoReconectTime;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException();
                autoReconectTime = value;
            }
        }

        internal void DoConnectionLost(ConnectionLostEventArgument argument)
        {
            ConnectionLost?.Invoke(this, argument);
            if (argument.Retry)
            {
                if (DefaultLogin == null || !(DefaultLogin is Connectors.LoginClient2)) return;
                var t = new System.Threading.Tasks.Task(() =>
                {
                    System.Threading.Thread.Sleep(AutoReconectTime);
                    var login = DefaultLogin as Connectors.LoginClient2;
                    login.Connect(login.ConnectTo);
                });
                t.Start();
            }
        }
        
        public void StartProgress()
        {
            connectors.ForEach((con) =>
                {
                    con.ProgressRun = true;
                    con.StartProgress();
                });
        }
        public void StopProgress()
        {
            connectors.ForEach((con) =>
            {
                con.ProgressRun = false;
                con.StopProgress();
            });
        }

        public ConnectionManager()
        {
            Users = new UserCollection();
            MessagePipelines = new List<IMessagePipeline>();
            CurrentId = new CurrentIdentification();
            Proxy = new Proxy(this);
        }
    }

    [Serializable]
    public delegate void MessageReceivedHandle(Message message);
}
