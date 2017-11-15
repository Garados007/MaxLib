using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace MaxLib.Net.ServerClient.Connectors
{
    public class LoginServer2 : Connector
    {
        public LoginServer2(Connection MainConnection)
        {
            if (MainConnection.Protocol != ConnectorProtocol.TCP)
                throw new WrongProtocolException();
            base.Connections.MainProtocol = ConnectorProtocol.TCP;
            base.CanChangeProtocol = false;
            base.MaxConnectionsCount = 1;
            base.Connections.Add(MainConnection);
            base.Connections[MainConnection] = true;
        }

        public override void StartProgress()
        {
            searcher = new Thread(RunLoop);
            searcher.Name = "LoginServer2 - ID=" + base.ConnectorId.ToString();
            searcher.Start();
        }

        public override void StopProgress()
        {
        }

        Thread searcher;
        public event GetUserConnectionHandler2 GetUserConnection;
        public event AddConnectionHandler AddConnection;

        void RunLoop()
        {
            var tcp = new TcpListener(new IPEndPoint(
                IPAddress.Any, Connections[0].Port));
            tcp.Start();
            while (ProgressRun)
            {
                if (tcp.Pending())
                {
                    var client = tcp.AcceptTcpClient();
                    var task = new Task(() => ListenTo(client));
                    task.Start();
                }
                else Thread.Sleep(10);
            }
            tcp.Stop();
        }

        void ListenTo(TcpClient tcp)
        {
            var stream = tcp.GetStream();
            var data = ConnectionHelper.Receive2(stream);
            // ==== 1. Nachricht ====
            var m = new PrimaryMessage();
            m.Load(data);
            // ---- Überprüfe den Typ der Nachricht ----
            if (m.MessageType!=PrimaryMessageType.WantToConnect)
            {
                ConnectionHelper.Send2(stream,
                    CreateFail(PrimaryMessageType.ConnectFailed_ExpectLogin));
                return;
            }
            // ---- Überprüfe die Identifikation des Clienten ----
            var id = m.ClientData.GetLoadSaveAble<CurrentIdentification>();
            var cid = base.Manager.CurrentId;
            if (id.StaticIdentification!=cid.StaticIdentification||
                id.Version!=cid.Version)
            {
                var b = CreateFail(PrimaryMessageType.ConnectFailed_WrongKey);
                b.ClientData.SetLoadSaveAble(cid);
                ConnectionHelper.Send2(stream, b);
                return;
            }
            // ---- Überprüfe den Platz auf dem Server ----
            if (base.Manager.Users.Count>=base.Manager.Users.MaxCount)
            {
                ConnectionHelper.Send2(stream,
                    CreateFail(PrimaryMessageType.ConnectFailed_FullServer));
                return;
            }
            // ---- Lege Nutzerdaten an ----
            var user = Manager.Users.AddNewUser();
            user.GlobalId = id.Id;
            Connection con = new Connection(ConnectorProtocol.TCP, 
                (tcp.Client.LocalEndPoint as IPEndPoint).Port);
            int ctr = Manager.DefaultDataTransport.ConnectorId;
            if (GetUserConnection != null)
                GetUserConnection(user, ref con, ref ctr);
            user.DefaultConnection = con;
            user.DefaultConnector = ctr;
            if (AddConnection != null) AddConnection(tcp, con);
            else (Manager.DefaultDataTransport as DataTransport2).AddConnection(tcp, con);
            // ==== 2. Nachricht - Die Verbindung wurde stattgegeben ====
            var pm = new PrimaryMessage();
            pm.MessageType = PrimaryMessageType.ConnectAllowed;
            ConnectionHelper.Send2(stream, pm);
        }

        PrimaryMessage CreateFail(PrimaryMessageType reason)
        {
            var pm = new PrimaryMessage();
            pm.MessageType = reason;
            return pm;
        }
    }

    public class LoginClient2 : Connector
    {
        public LoginClient2()
        {
            base.Connections.MainProtocol = ConnectorProtocol.TCP;
            base.CanChangeProtocol = false;
            State = LoginState.Wait;
        }

        public override void StartProgress()
        {
            searcher = new Thread(RunLoop);
            searcher.Name = "LoginClient2 - ID=" + base.ConnectorId.ToString();
            searcher.Start();
            base.MaxConnectionsCount = 0;
        }

        public override void StopProgress()
        {
        }

        Thread searcher;
        public event ConnectedHandler2 Connected;
        public event Action ServerFull, ErrorWhileConnection;
        public event AddConnectionHandler AddConnection;
        internal string ConnectTo = null;
        public void Connect(string serverIp)
        {
            ConnectTo = serverIp;
            State = LoginState.IsConnecting;
        }
        public LoginState State { get; private set; }
        public int ServerPort { get; set; }
        public CurrentIdentification ExceptedId { get; private set; }

        void RunLoop()
        {
            while (base.ProgressRun)
            {
                if (State == LoginState.IsConnecting)
                    try
                    {
                        var ip = new IPEndPoint(IPAddress.Parse(ConnectTo),
                            ServerPort);
                        var pm = new PrimaryMessage();
                        pm.MessageType = PrimaryMessageType.WantToConnect;
                        pm.ClientData.SetLoadSaveAble(Manager.CurrentId);
                        var tcp = new TcpClient();
                        tcp.Connect(ip);
                        var stream = tcp.GetStream();
                        ConnectionHelper.Send2(stream, pm);
                        pm.Load(ConnectionHelper.Receive2(stream));
                        ExceptedId = null;
                        if (pm.MessageType == PrimaryMessageType.ConnectAllowed)
                        {
                            State = LoginState.Connected;
                            var con = new Connection(ConnectorProtocol.TCP,
                                (tcp.Client.LocalEndPoint as IPEndPoint).Port,
                                ConnectTo);
                            var user = Manager.Users.AddNewUser();
                            user.GlobalId = pm.SenderID;
                            user.DefaultConnection = con;
                            user.DefaultConnector = Manager.DefaultDataTransport != null ?
                                Manager.DefaultDataTransport.ConnectorId : -1;
                            if (Connected != null) Connected(user);
                            if (AddConnection != null) AddConnection(tcp, con);
                            else (Manager.DefaultDataTransport as DataTransport2).AddConnection(tcp, con);
                        }
                        else if (pm.MessageType == PrimaryMessageType.ConnectFailed_FullServer)
                        {
                            State = LoginState.ServerFull;
                            if (ServerFull != null) ServerFull();
                        }
                        else if (pm.MessageType == PrimaryMessageType.ConnectFailed_WrongKey)
                        {
                            State = LoginState.WrongID;
                            ExceptedId = pm.ClientData.GetLoadSaveAble<CurrentIdentification>();
                            if (ErrorWhileConnection != null) ErrorWhileConnection();
                        }
                        else
                        {
                            State = LoginState.NotConnectable;
                            if (ErrorWhileConnection != null) ErrorWhileConnection();
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

    static class ConnectionHelper
    {
        public static byte[] Receive2(NetworkStream stream)
        {
            var l = new List<byte>();

            var buffer = new byte[4];
            var time = 0;
            for (; time < 50 && !stream.DataAvailable; time++)
                Thread.Sleep(10);
            if (time == 50) return new byte[0];
            stream.Read(buffer, 0, 4);
            var length = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[4 * 1024];
            int readed;
            for (time = 0; time < 50 && l.Count < length; time++)
            {
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(10);
                    continue;
                }
                while ((readed = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    l.AddRange(buffer.ToList().GetRange(0, readed));
                    if (readed < buffer.Length) break;
                }
            }
            if (time == 50) return new byte[0];

            return l.ToArray();
        }

        public static void Send2(NetworkStream stream, byte[] data)
        {
            var length = BitConverter.GetBytes(data.Length);
            stream.Write(length, 0, 4);
            stream.Write(data, 0, data.Length);
        }
        public static void Send2(NetworkStream stream, ILoadSaveAble data)
        {
            Send2(stream, data.Save());
        }
    }

    public delegate void GetUserConnectionHandler2(User user, ref Connection connection, ref int connector);
    public delegate void ConnectedHandler2(User targetUser);
    public delegate void AddConnectionHandler(TcpClient client, Connection connection);
}
