using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace MaxLib.Net.ServerClient.Connectors
{
    public class DataTransport : Connector
    {
        public DataTransport(bool runAsServer)
        {
            RunAsServer = runAsServer;
            base.Connections.MainProtocol = ConnectorProtocol.TCP;
            base.CanChangeProtocol = false;
            base.MaxConnectionsCount = int.MaxValue;
        }

        protected override void ConnectionRemoved(Connection connection)
        {
            var d = datas.Find((dt) => dt.con == connection);
            if (d == null) return;
            d.active = false;
            datas.Remove(d);
            SetUsedState(connection, false);
        }

        protected override void ConnectionAdded(Connection connection)
        {
            var d = new data();
            d.con = connection;
            d.Manager = Manager;
            d.datatr = this;
            d.connector = ConnectorId;
            d.id = Manager.Users.Users.Find((u) => u.DefaultConnection == connection).Id;
            d.task = new Task(new Action(() =>
                {
                    if (RunAsServer) d.RunAsServer();
                    else d.RunAsClient();
                }));
            d.task.Start();
            datas.Add(d);
            SetUsedState(connection, true);
        }

        public override void StartProgress()
        {
            for (int i = 0; i < Connections.Count; ++i) ConnectionAdded(Connections[i]);
        }

        public override void StopProgress()
        {
            for (int i = 0; i < Connections.Count; ++i) ConnectionRemoved(Connections[i]);
        }

        public override void SendMessage(PrimaryMessage message)
        {
            base.SendMessage(message);
            datas.Find((d) => d.con == message.MessageRoot.Connection).messages.Enqueue(message);
        }

        List<data> datas = new List<data>();
        public bool RunAsServer { get; private set; }

        public event Action<LogoutUser> UserLogedOut;

        private void DoConnectionLost(ConnectionLostEventArgument argument, data sender)
        {
            var rest = new List<PrimaryMessage>();
            DoConnectionLost(argument);
            if (argument.Retry) while (sender.messages.Count > 0) rest.Add(sender.messages.Dequeue());
            ConnectionRemoved(sender.con);
            //if (argument.Retry)
            //{
            //    ConnectionAdded(sender.con);
            //    var d = datas.Find((dt) => dt.con == sender.con);
            //    if (argument.SendedMessage != null&&argument.SendedMessage is PrimaryMessage) 
            //        d.messages.Enqueue(argument.SendedMessage as PrimaryMessage);
            //    for (int i = 0; i < rest.Count; ++i) d.messages.Enqueue(rest[i]);
            //}
            if (Manager != null)
            {
                var user = Manager.Users.Users.Find((u) => u.DefaultConnector == base.ConnectorId);
                if (user != null)
                {
                    user.Ping = -1;
                    if (argument.UserLogout)
                    {
                        Manager.Users.RemoveUser(user);
                        if (UserLogedOut != null)
                        {
                            var lu = new LogoutUser(user, this, argument);
                            UserLogedOut(lu);
                        }
                    }
                }
            }
        }

        class data
        {
            public Task task;
            public DataTransport datatr;
            public Connection con;
            public bool active = true;
            public ConnectionManager Manager;
            public int connector, id;

            public Queue<PrimaryMessage> messages = new Queue<PrimaryMessage>();

            ConnectionLostEventArgument cl = null;

            public void RunAsServer()
            {
                var tcpserv = new TcpListener(new IPEndPoint(IPAddress.Any, con.Port));
                tcpserv.Start();
                var tcp = tcpserv.AcceptTcpClient();
                tcpserv.Stop();
                doConct(tcp);
                tcp.Close();
                if (cl != null) datatr.DoConnectionLost(cl, this);
            }

            public void RunAsClient()
            {
                var tcp = new TcpClient();
                Thread.Sleep(10);
                tcp.Connect(new IPEndPoint(IPAddress.Parse(con.Target), con.Port));
                doConct(tcp);
                tcp.Close();
                if (cl != null) datatr.DoConnectionLost(cl, this);
            }

            int lastPing = Environment.TickCount;
            void DoPing()
            {
                if (Manager == null) return;
                if (Environment.TickCount - lastPing >= Manager.DefaultPingTime)
                {
                    lastPing = Environment.TickCount;
                    var pm = new PrimaryMessage();
                    pm.MessageType = PrimaryMessageType.Ping;
                    pm.ClientData.SetBinary(BitConverter.GetBytes(Environment.TickCount));
                    messages.Enqueue(pm);
                }
            }

            void doConct(TcpClient tcp)
            {
                using (var stream = tcp.GetStream())
                    while (active)
                    {
                        if (!tcp.Connected)
                        {
                            cl = new ConnectionLostEventArgument(datatr, con, null);
                            return;
                        }
                        DoPing();
                        while (tcp.Available > 0)
                        {
                            var l = new List<byte>();
                            var b = new byte[tcp.ReceiveBufferSize];
                            int len;
                            while (true)
                            {
                                len = stream.Read(b, 0, b.Length);
                                l.AddRange(b.ToList().GetRange(0, len).ToArray());
                                if (len < b.Length) break;
                            }
                            try
                            {
                                var mes = new PrimaryMessage();
                                mes.Load(l.ToArray());
                                mes.MessageRoot.Connection = con;
                                mes.MessageRoot.Connector = connector;
                                mes.MessageRoot.RemoteId = id;
                                Manager.ReceiveMessage(mes);
                            }
                            catch
                            {
                                if (tcp.Connected) continue;
                                else
                                {
                                    cl = new ConnectionLostEventArgument(datatr, con, null);
                                    return;
                                }
                            }
                        }
                        while (messages.Count > 0)
                        {
                            var mes = messages.Dequeue();
                            if (mes == null) continue;
                            if (!tcp.Connected)
                            {
                                cl = new ConnectionLostEventArgument(datatr, con, mes);
                                return;
                            }
                            mes.SenderID = Manager.CurrentId.Id;
                            var b = mes.Save();
                            try
                            {
                                stream.Write(b, 0, b.Length);
                            }
                            catch
                            {
                                cl = new ConnectionLostEventArgument(datatr, con, mes);
                                return;
                            }
                        }
                        if (active)
                        {
                            if (!tcp.Connected)
                            {
                                cl = new ConnectionLostEventArgument(datatr, con, null);
                                return;
                            }
                            Thread.Sleep(10);
                        }
                    }
            }
        }
    }

    public class LogoutUser
    {
        public User User { get; private set; }

        public Connector Connector { get; private set; }

        public ConnectionLostEventArgument Argument { get; private set; }

        public LogoutUser(User user, Connector connector, ConnectionLostEventArgument argument)
        {
            User = user;
            Connector = connector;
            Argument = argument;
        }
    }

}
