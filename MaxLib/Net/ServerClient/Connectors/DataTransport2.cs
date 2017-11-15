using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace MaxLib.Net.ServerClient.Connectors
{
    public class DataTransport2 : Connector
    {
        public DataTransport2()
        {
            base.Connections.MainProtocol = ConnectorProtocol.TCP;
            base.CanChangeProtocol = false;
            base.MaxConnectionsCount = int.MaxValue;
        }

        public void AddConnection(TcpClient client, Connection connection)
        {
            base.Connections.Add(connection);
            var d = new data();
            d.con = connection;
            d.Manager = Manager;
            d.datatr = this;
            d.connector = ConnectorId;
            d.id = Manager.Users.Users.Find((u) => u.DefaultConnection == connection).Id;
            d.tcp = client;
            d.task = new Task(d.Run);
            d.task.Start();
            datas.Add(d);
            SetUsedState(connection, true);
        }

        protected override void ConnectionRemoved(Connection connection)
        {
            base.ConnectionRemoved(connection);
            var d = datas.Find((dt) => dt.con == connection);
            if (d == null) return;
            d.active = false;
            datas.Remove(d);
        }

        public override void StartProgress()
        {
        }

        public override void StopProgress()
        {
            Connections.Clear();
        }

        public override void SendMessage(PrimaryMessage message)
        {
            base.SendMessage(message);
            datas.Find((d) => d.con == message.MessageRoot.Connection).messages.Enqueue(message);
        }

        List<data> datas = new List<data>();
        public event Action<LogoutUser> UserLogedOut;

        private void DoConnectionLost(ConnectionLostEventArgument argument, data sender)
        {
            var rest = new List<PrimaryMessage>();
            DoConnectionLost(argument);
            if (argument.Retry) rest.AddRange(sender.messages);
            Connections.Remove(sender.con);
            if (argument.Retry)
            {
                if (Manager != null && Manager.DefaultLogin is LoginClient2)
                {
                    var lc = Manager.DefaultLogin as LoginClient2;
                    lc.Connect(sender.con.Target);
                }
            }
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
                            UserLogedOut(new LogoutUser(user, this, argument));
                    }
                }
            }
        }

        class data
        {
            public Task task;
            public DataTransport2 datatr;
            public Connection con;
            public bool active = true;
            public ConnectionManager Manager;
            public int connector, id;
            public TcpClient tcp;
            public Queue<PrimaryMessage> messages = new Queue<PrimaryMessage>();

            ConnectionLostEventArgument cl = null;
            int lastPing = Environment.TickCount;

            public void Run()
            {
                doCont();
                tcp.Close();
                if (cl != null) datatr.DoConnectionLost(cl, this);
            }

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

            void doCont()
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
                        while (tcp.Available>0)
                        {
                            var data = ConnectionHelper.Receive2(stream);
                            try
                            {
                                var mes = new PrimaryMessage();
                                mes.Load(data);
                                mes.MessageRoot.Connection = con;
                                mes.MessageRoot.Connector = connector;
                                mes.MessageRoot.RemoteId = id;
                                var task = new Task(() => Manager.ReceiveMessage(mes));
                                task.Start();
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
                        while (messages.Count>0)
                        {
                            var mes = messages.Dequeue();
                            if (mes == null) continue;
                            if (!tcp.Connected)
                            {
                                cl = new ConnectionLostEventArgument(datatr, con, mes);
                                return;
                            }
                            mes.SenderID = Manager.CurrentId.Id;
                            try { ConnectionHelper.Send2(stream, mes); }
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
}
