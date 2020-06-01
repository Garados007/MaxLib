using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Net.ServerClient
{
    public class Proxy
    {
        public Proxy(ConnectionManager manager)
        {
            Manager = manager;
            Server = new List<ProxyServer>();
        }

        public List<ProxyServer> Server { get; private set; }

        public ConnectionManager Manager { get; private set; }

        public bool EnableAutoFetchList { get; set; }

        public event Action<User> RemoteUserAdded;

        protected User AddProxyUser(int serverId, byte[] targetGlobalId)
        {
            var user = Manager.Users.AddNewUser();
            user.IsProxy = true;
            user.GlobalId = targetGlobalId;
            if (RemoteUserAdded != null) RemoteUserAdded(user);
            var serv = Manager.Users.GetUserFromId(serverId);
            if (serv!=null)
            {
                var ps = Server.Find((p) => p.ServerUser == serv);
                if (ps == null) Server.Add(ps = new ProxyServer() { ServerUser = serv });
                if (!ps.OwnedUser.Contains(user)) ps.OwnedUser.Add(user);
            }
            return user;
        }

        public virtual void FetchUser()
        {
            foreach (var user in Manager.Users.Users.ToArray())
                FetchUser(user);
        }

        public virtual void FetchUser(User serverUser)
        {
            var pm = new PrimaryMessage();
            pm.IgnoreProxy = false;
            pm.MessageRoot.SetFromUser(serverUser);
            pm.MessageType = PrimaryMessageType.ProxyFetchList;
            Manager.SendMessage(pm);
        }

        public virtual void SendUserList(PrimaryMessage message)
        {
            var user = Manager.Users.GetUserFromId(message.SenderID) ?? AddProxyUser(message.MessageRoot.RemoteId, message.SenderID);
            var l = new List<byte>();
            foreach (var u in Manager.Users.Users.ToArray())
                if (u != user) l.AddRange(u.GlobalId);
            var pm = new PrimaryMessage();
            pm.ClientData.SetBinary(l.ToArray());
            pm.MessageRoot.SetFromUser(user);
            pm.MessageType = PrimaryMessageType.ProxySendList;
            Manager.SendMessage(pm);
        }

        public virtual void ReceiveUserList(PrimaryMessage message)
        {
            var user = Manager.Users.GetUserFromId(message.SenderID) ?? AddProxyUser(message.MessageRoot.RemoteId, message.SenderID);
            var serv = Server.Find((ps) => ps.ServerUser == user || ps.OwnedUser.Contains(user));
            if (serv == null) Server.Add(serv = new ProxyServer() { ServerUser = user });
            var b = message.ClientData.GetBinary().ToList();
            for (var i = 0; i<b.Count; i+=16)
            {
                var id = b.GetRange(i, 16).ToArray();
                var rm = Manager.Users.GetUserFromId(id);
                //if (rm==null) serv.OwnedUser.Add(AddProxyUser()) //dringend weiterprogrammieren!!!
            }
        }

        public virtual bool IsProxyRequired(PrimaryMessage message)
        {
            if (message.IgnoreProxy) return false;
            var user = Manager.Users.GetUserFromId(message.MessageRoot.RemoteId);
            return user != null && user.IsProxy;
        }

        public virtual void SendMessageToProxy(PrimaryMessage message)
        {
            if (message.MessageType== PrimaryMessageType.Ping)
            {
                message.MessageType = PrimaryMessageType.PingAnswer;
                Manager.ReceiveMessage(message);
                return;
            }
            var proxymes = new ProxyMessage();
            proxymes.Message = message;
            var user = Manager.Users.GetUserFromId(message.MessageRoot.RemoteId);
            var serv = Server.Find((server) => server.OwnedUser.Contains(user));
            if (user == null || serv == null)
            {
                message.IgnoreProxy = true;
                Manager.SendMessage(message);
            }
            else
            {
                proxymes.TargetId = user.GlobalId;
                proxymes.MessageRoot.SetFromUser(serv.ServerUser);
                var pm = new PrimaryMessage();
                pm.ClientData.SetMessage(proxymes);
                pm.MessageType = PrimaryMessageType.ProxySend;
                pm.MessageRoot.SetFromUser(serv.ServerUser);
                Manager.SendMessage(pm);
            }
        }

        public virtual void ReceivedMessageAsProxy(PrimaryMessage message)
        {
            var proxymes = message.ClientData.GetMessage<ProxyMessage>();
            if (!ProxyMessageReceived(proxymes)) return;
            var isTarget = IdEquals(Manager.CurrentId.Id, proxymes.TargetId);
            if (isTarget)
            {
                if (!ProxyMessageInTarget(proxymes)) return;
                var user = Manager.Users.GetUserFromId(proxymes.Message.SenderID) ?? AddProxyUser(message.MessageRoot.RemoteId, message.SenderID); //message.SenderID???
                proxymes.MessageRoot.RemoteId = user.Id;
                Manager.ReceiveMessage(proxymes.Message);
            }
            else
            {
                if (!SendProxyMessageToTarget(proxymes)) return;
                var user = Manager.Users.GetUserFromId(proxymes.TargetId) ?? AddProxyUser(message.MessageRoot.RemoteId, message.SenderID); //message.SenderID???
                var serv = Server.Find((server) => server.OwnedUser.Contains(user));
                if (serv == null) return;
                proxymes.MessageRoot.SetFromUser(serv.ServerUser);
                var pm = new PrimaryMessage();
                pm.ClientData.SetMessage(proxymes);
                pm.MessageType = PrimaryMessageType.ProxySend;
                pm.MessageRoot.SetFromUser(serv.ServerUser);
                Manager.SendMessage(pm);
            }
        }

        bool IdEquals(byte[] id1, byte[] id2)
        {
            if (id1.Length != id2.Length) return false;
            for (var i = 0; i < id1.Length; ++i) if (id1[i] != id2[i]) return false;
            return true;
        }

        protected virtual bool ProxyMessageReceived(ProxyMessage message)
        {
            return true;
        }

        protected virtual bool ProxyMessageInTarget(ProxyMessage message)
        {
            return true;
        }

        protected virtual bool SendProxyMessageToTarget(ProxyMessage message)
        {
            return true;
        }
    }

    public class ProxyMessage : Message
    {
        public byte[] TargetId
        {
            get => HeaderBytes;
            set => HeaderBytes = value;
        }

        public PrimaryMessage Message
        {
            get => ClientData.GetMessage<PrimaryMessage>();
            set => ClientData.SetMessage(value);
        }
    }

    public class ProxyServer
    {
        public List<User> OwnedUser { get; private set; }

        public User ServerUser { get; set; }

        public ProxyServer()
        {
            OwnedUser = new List<User>();
        }
    }
}
