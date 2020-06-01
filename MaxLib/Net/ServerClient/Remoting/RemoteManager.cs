using System;
using System.Collections.Generic;
using MaxLib.Net.ServerClient.Connectors;

namespace MaxLib.Net.ServerClient.Remoting
{
    public class RemoteManager : RemoteObject<ConnectionManager>
    {
        protected ConnectionManager Manager
        {
            get { return MainObject; }
        }

        public RemoteUserCollection GetUsers()
        {
            return new RemoteUserCollection(Manager.Users);
        }

        public RemoteIdentification GetCurrentId()
        {
            return new RemoteIdentification(MainObject.CurrentId);
        }

        public RemoteConnector GetDefaultLogin()
        {
            if (Manager.DefaultLogin != null)
                return new RemoteConnector(Manager.DefaultLogin);
            else return null;
        }
        public RemoteConnector GetDefaultDataTransport()
        {
            if (Manager.DefaultDataTransport != null)
                return new RemoteConnector(Manager.DefaultDataTransport);
            else return null;
        }
        public RemoteConnector GetDefaultFileTransport()
        {
            if (Manager.DefaultFileTransport != null)
                return new RemoteConnector(Manager.DefaultFileTransport);
            else return null;
        }

        public RemoteManager(ConnectionManager manager) 
            : base(manager)
        {
            manager.PushMessageReceived += Manager_PushMessageReceived;
        }

        void Manager_PushMessageReceived(Message message)
        {
            pushMessageReceived.ForEach((re) => re.Invoke(message));
        }

        new protected ConnectionManager GetObject(AppDomain current)
        {
            return base.GetObject(current);
        }
        public ConnectionManager GetManager(AppDomain current)
        {
            return base.GetObject(current);
        }

        public void SendPushMessage(Message message)
        {
            Manager.SendPushMessage(message);
        }

        public void SendMessage(PrimaryMessage message)
        {
            Manager.SendMessage(message);
        }

        readonly List<RemoteEvent<MessageReceivedHandle>> pushMessageReceived = new List<RemoteEvent<MessageReceivedHandle>>();

        public void AddPushMessageReceivedEvent(RemoteEvent<MessageReceivedHandle> handle)
        {
            pushMessageReceived.Add(handle);
        }

        public void RemovePushMessageReceivedEvent(RemoteEvent<MessageReceivedHandle> handle)
        {
            pushMessageReceived.Remove(handle);
        }

        [field: NonSerialized]
        readonly List<RemoteConnectorIntegration> connectors = new List<RemoteConnectorIntegration>();

        public void AddConnector(RemoteConnectorHelper connector)
        {
            var con = new RemoteConnectorIntegration
            {
                helper = connector
            };
            connectors.Add(con);
            connector.integr = con;
            Manager.AddConnector(con);
        }

        public void RemoveConnector(RemoteConnectorHelper connector)
        {
            var con = connectors.Find((rci) => rci.helper == connector);
            if (con == null) return;
            connectors.Remove(con);
            Manager.RemoveConnector(con);
        }

        internal void RecData(PrimaryMessage message)
        {
            Manager.ReceiveMessage(message);
        }
    }

    public class RemoteConnector : RemoteObject<Connector>
    {
        public RemoteConnector(Connector con)
            : base(con)
        { }

        public bool IsLoginServer
        {
            get { return MainObject is LoginServer; }
        }
        public bool IsLoginClient
        {
            get { return MainObject is LoginClient; }
        }
        public bool IsDataTransport
        {
            get { return MainObject is DataTransport; }
        }
        public bool IsFileTransport
        {
            get { return MainObject is FileTransport; }
        }
        public bool IsRemote
        {
            get { return MainObject is RemoteConnectorIntegration; }
        }

        new protected Connector GetObject(AppDomain current)
        {
            return base.GetObject(current);
        }
        public Connector GetConnector(AppDomain current)
        {
            return base.GetObject(current);
        }

        public int GetConnectorId()
        {
            return MainObject.ConnectorId;
        }
        public int GetMaxConnectionsCount()
        {
            return MainObject.MaxConnectionsCount;
        }
        public int GetConnectionsCount()
        {
            return MainObject.Connections.Count;
        }
        public Connection GetConnectionByIndex(int index)
        {
            return MainObject.Connections[index];
        }
    }

    public class RemoteConnectorHelper : RemoteObject<Connector>
    {
        public RemoteManager Owner { get; protected set; }
        internal RemoteConnectorIntegration integr;

        public RemoteConnectorHelper(Connector con, RemoteManager man)
            : base (con)
        {
            Owner = man;
            con.MesRec += Con__MesRec;
        }

        void Con__MesRec(PrimaryMessage obj)
        {
            obj.MessageRoot.Connector = MainObject.ConnectorId;
            Owner.RecData(obj);
        }

        new protected Connector GetObject(AppDomain current)
        {
            return base.GetObject(current);
        }
        public Connector GetConnector(AppDomain current)
        {
            return base.GetObject(current);
        }

        internal void StartProgress()
        {
            MainObject.StartProgress();
        }
        internal void StopProgress()
        {
            MainObject.StopProgress();
        }
        internal void SetConnectorId(int id)
        {
            MainObject.ConnectorId = id;
        }

        internal int GetMaxConCount()
        {
            return MainObject.MaxConnectionsCount;
        }
        internal void SetMaxConCount(int mc)
        {
            MainObject.Connections.MaxCount = mc;
        }

        internal void SendMessage(PrimaryMessage message)
        {
            MainObject.SendMessage(message);
        }
    }

    class RemoteConnectorIntegration : Connector
    {
        public RemoteConnectorHelper helper;

        public override void StartProgress()
        {
            helper.StartProgress();
        }

        public override void StopProgress()
        {
            helper.StopProgress();
        }

        public override int ConnectorId
        {
            get => base.ConnectorId;
            internal set
            {
                base.ConnectorId = value;
                helper.SetConnectorId(value);
            }
        }

        public override int MaxConnectionsCount
        {
            get => helper.GetMaxConCount();
            protected set => helper.SetMaxConCount(value);
        }

        public override void SendMessage(PrimaryMessage message)
        {
            helper.SendMessage(message);
        }
    }

    public class RemoteUser : RemoteObject<User>
    {
        public RemoteUser(User user)
            : base(user)
        { }

        new protected User GetObject(AppDomain current)
        {
            return base.GetObject(current);
        }
        public User GetUser(AppDomain current)
        {
            return base.GetObject(current);
        }

        public int GetId()
        {
            return MainObject.Id;
        }

        public byte[] GetGlobalId()
        {
            return MainObject.GlobalId;
        }

        public int GetDefaultConnector()
        {
            return MainObject.DefaultConnector;
        }

        public Connection GetDefaultConnection()
        {
            return MainObject.DefaultConnection;
        }
    }

    public class RemoteUserCollection : RemoteObject<UserCollection>
    {
        public RemoteUserCollection(UserCollection uc)
            : base(uc)
        {

        }

        new protected UserCollection GetObject(AppDomain current)
        {
            return base.GetObject(current);
        }
        public UserCollection GetCollection(AppDomain current)
        {
            return base.GetObject(current);
        }

        public RemoteUser GetUserFromIndex(int index)
        {
            return new RemoteUser(MainObject.Users[index]);
        }

        public RemoteUser GetUserFromId(byte[] id)
        {
            return new RemoteUser(MainObject.GetUserFromId(id));
        }

        public RemoteUser GetUserFromId(int id)
        {
            return new RemoteUser(MainObject.GetUserFromId(id));
        }

        public int GetUserCount()
        {
            return MainObject.Count;
        }
    }

    public class RemoteIdentification : RemoteObject<CurrentIdentification>
    {
        public RemoteIdentification(CurrentIdentification ci)
            : base(ci)
        { }

        new protected CurrentIdentification GetObject(AppDomain current)
        {
            return base.GetObject(current);
        }
        public CurrentIdentification GetIdentification(AppDomain current)
        {
            return base.GetObject(current);
        }

        public byte[] GetId()
        {
            return MainObject.Id;
        }

        public string GetIdentification()
        {
            return MainObject.StaticIdentification;
        }

        public string GetVersion()
        {
            return MainObject.Version;
        }
    }

    [Serializable]
    public class RemoteObject<T> : MarshalByRefObject where T:class
    {
        [field: NonSerialized]
        protected T MainObject = null;
        [field: NonSerialized]
        protected AppDomain domain = null;

        public RemoteObject(T DefaultObject)
        {
            MainObject = DefaultObject;
            domain = AppDomain.CurrentDomain;
        }

        public virtual bool IsObjectAvaible(AppDomain current)
        {
            return current.ToString() == domain.ToString();
        }

        public virtual T GetObject(AppDomain current)
        {
            if (IsObjectAvaible(current)) return MainObject;
            else return null;
        }

        public AppDomain GetInitialDomain()
        {
            return domain;
        }
    }
}
