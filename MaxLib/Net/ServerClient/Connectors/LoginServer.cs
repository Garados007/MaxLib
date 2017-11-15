using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace MaxLib.Net.ServerClient.Connectors
{
    public class LoginServer : Connector
    {
        public LoginServer(Connection MainConnection)
        {
            if (MainConnection.Protocol != ConnectorProtocol.UDP) throw new WrongProtocolException();
            base.Connections.MainProtocol = ConnectorProtocol.UDP;
            base.CanChangeProtocol = false;
            base.MaxConnectionsCount = 1;
            base.Connections.Add(MainConnection);
            base.Connections[MainConnection] = true;
        }

        public override void StartProgress()
        {
            checker = new Thread(RunLoop);
            checker.Name = "LoginServer - ID=" + base.ConnectorId.ToString();
            checker.Start();
        }

        public override void StopProgress()
        {
        }

        Thread checker;
        public event GetUserConnectionHandler GetUserConnection;

        void RunLoop()
        {
            using (var udp = new UdpClient(new IPEndPoint(IPAddress.Any, Connections[0].Port)))
                while (ProgressRun)
                {
                    //Kurzerklärung:
                    //Ein Aufrufsignal mit einem ID-Paket wird geschickt. Dieses wird eingelesen und ein User wird erstellt.
                    //Eine Zielkooridinate wird erstellt und rückgesendet. Fertig.
                    while (udp.Available>0)
                    {
                        var ep = new IPEndPoint(IPAddress.Any, 0);
                        var data = udp.Receive(ref ep);
                        //try
                        //{
                            var m = new PrimaryMessage();
                            m.Load(data);
                            byte[] b;
                            if (m.MessageType != PrimaryMessageType.WantToConnect)
                            {
                                b = CreateFail(PrimaryMessageType.ConnectFailed_ExpectLogin).Save();
                                udp.Send(b, b.Length, ep);
                                continue;
                            }
                            var id = m.ClientData.GetLoadSaveAble<CurrentIdentification>();
                            var cid = base.Manager.CurrentId;
                            if (id.StaticIdentification!=cid.StaticIdentification||id.Version!=cid.Version)
                            {
                                b = CreateFail(PrimaryMessageType.ConnectFailed_WrongKey).Save();
                                udp.Send(b, b.Length, ep);
                                continue;
                            }
                            if (base.Manager.Users.Count>=base.Manager.Users.MaxCount)
                            {
                                b = CreateFail(PrimaryMessageType.ConnectFailed_FullServer).Save();
                                udp.Send(b, b.Length, ep);
                                continue;
                            }
                            var user = Manager.Users.AddNewUser();
                            user.GlobalId = id.Id;
                            Connection con;
                            int ctr;
                            if (GetUserConnection!=null)
                            {
                                GetUserConnection(user, out con, out ctr);
                            }
                            else
                            {
                                ctr = Manager.DefaultDataTransport.ConnectorId;
                                con = Manager.DefaultDataTransport.Connections.GetFree();
                                Manager.FindConnector(ctr).Connections[con] = true;
                            }
                            user.DefaultConnection = con;
                            user.DefaultConnector = ctr;
                            var pm = new PrimaryMessage();
                            pm.MessageType = PrimaryMessageType.ConnectAllowed;
                            pm.ClientData.SetSerializeAble(con);
                            b = pm.Save();
                            udp.Send(b, b.Length, ep);
                        //}
                        //catch
                        //{
                        //    var b = CreateFail(PrimaryMessageType.ConnectFailed).Save();
                        //    udp.Send(b, b.Length, ep);
                        //    continue;
                        //}
                    }

                    if (base.ProgressRun) Thread.Sleep(10);
                }

        }

        PrimaryMessage CreateFail(PrimaryMessageType reason)
        {
            var pm = new PrimaryMessage();
            pm.MessageType = reason;
            return pm;
        }
    }

    public class LoginClient : Connector
    {
        public LoginClient()
        {
            base.Connections.MainProtocol = ConnectorProtocol.UDP;
            base.CanChangeProtocol = false;
            State = LoginState.Wait;
        }

        public override void StartProgress()
        {
            checker = new Thread(RunLoop);
            checker.Name = "LoginClient - ID=" + base.ConnectorId.ToString();
            checker.Start();
            base.MaxConnectionsCount = 0;
        }

        public override void StopProgress()
        {
        }

        Thread checker;
        public event ConnectedHandler Connected;
        public event Action ServerFull, ErrorWhileConnection;
        internal string ConnectTo = null;
        public void Connect(string serverIp)
        {
            ConnectTo = serverIp;
            State = LoginState.IsConnecting;
        }

        public LoginState State { get; private set; }
        public int ServerPort { get; set; }

        //public event GetUserConnectionHandler CreateServerUser;

        void RunLoop()
        {
            while (base.ProgressRun)
            {
                if (State == LoginState.IsConnecting)
                    try
                    {
                        var ip = new IPEndPoint(IPAddress.Parse(ConnectTo), ServerPort);
                        var pm = new PrimaryMessage();
                        pm.MessageType = PrimaryMessageType.WantToConnect;
                        pm.ClientData.SetLoadSaveAble(Manager.CurrentId);
                        using (var udp = new UdpClient())
                        {
                            var b = pm.Save();
                            udp.Send(b, b.Length, ip);
                            for (int i = 0; i < 500; ++i)
                            {
                                if (udp.Available != 0) break;
                                Thread.Sleep(1);
                            }
                            b = udp.Receive(ref ip);
                            pm.Load(b);
                            if (pm.MessageType == PrimaryMessageType.ConnectAllowed)
                            {
                                State = LoginState.Connected;
                                var con = pm.ClientData.GetSerializeAble<Connection>();
                                con = new Connection(con.Protocol, con.Port, ConnectTo);
                                var user = Manager.Users.AddNewUser();
                                user.GlobalId = pm.SenderID;
                                user.DefaultConnection = con;
                                user.DefaultConnector = Manager.DefaultDataTransport != null ? 
                                    Manager.DefaultDataTransport.ConnectorId : -1;
                                if (Connected != null) Connected(con);
                            }
                            else if (pm.MessageType == PrimaryMessageType.ConnectFailed_FullServer)
                            {
                                State = LoginState.ServerFull;
                                if (ServerFull != null) ServerFull();
                            }
                            else if (pm.MessageType == PrimaryMessageType.ConnectFailed_WrongKey)
                            {
                                State = LoginState.WrongID;
                                if (ErrorWhileConnection != null) ErrorWhileConnection();
                            }
                            else
                            {
                                State = LoginState.NotConnectable;
                                if (ErrorWhileConnection != null) ErrorWhileConnection();
                            }
                        }
                    }
                    catch
                    {
                        State = LoginState.NotConnectable;
                        if (ErrorWhileConnection != null) ErrorWhileConnection();
                    }
                Thread.Sleep(10);
            }
        }
    }

    public delegate void GetUserConnectionHandler(User user, out Connection connection, out int connector);
    public delegate void ConnectedHandler(Connection targetConnection);

    public enum LoginState
    {
        Wait,
        IsConnecting,
        Connected,
        ServerFull,
        WrongID,
        NotConnectable
    }
}
