using MaxLib.Data.IniFiles;
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.SSL
{
    public class SecureWebServer : WebServer
    {
        public SecureWebServerSettings SecureSettings => (SecureWebServerSettings)Settings;

        //Secure Server
        protected TcpListener SecureListener;
        protected Thread SecureServerThread;

        public SecureWebServer(SecureWebServerSettings settings) : base(settings)
        {
        }

        public override void Start()
        {
            if (SecureSettings.EnableUnsafePort)
                base.Start();
            WebServerInfo.Add(InfoType.Information, GetType(), "StartUp", "Start Secure Server on Port {0}", SecureSettings.SecurePort);
            ServerExecution = true;
            SecureListener = new TcpListener(new IPEndPoint(Settings.IPFilter, SecureSettings.SecurePort));
            SecureListener.Start();
            SecureServerThread = new Thread(SecureMainTask)
            {
                Name = "SecureServerThread - Port: " + SecureSettings.SecurePort.ToString()
            };
            SecureServerThread.Start();
        }

        public override void Stop()
        {
            if (SecureSettings.EnableUnsafePort)
                base.Stop();
            WebServerInfo.Add(InfoType.Information, GetType(), "StartUp", "Stopped Secure Server");
            ServerExecution = false;
        }

        protected virtual void SecureMainTask()
        {
            WebServerInfo.Add(InfoType.Information, GetType(), "StartUp", "Secure Server succesfuly started");
            while (ServerExecution)
            {
                var start = Environment.TickCount;
                //Ausstehende Verbindungen
                int step = 0;
                for (; step < 10; step++)
                {
                    if (!SecureListener.Pending()) break;
                    SecureClientConnected(SecureListener.AcceptTcpClient());
                }
                //Warten
                var stop = Environment.TickCount;
                if (SecureListener.Pending()) continue;
                var time = (stop - start) % 20;
                Thread.Sleep(20 - time);
            }
            SecureListener.Stop();
            WebServerInfo.Add(InfoType.Information, GetType(), "StartUp", "Secure Server succesfuly stopped");
        }

        protected virtual void SecureClientConnected(TcpClient client)
        {
            if (SecureSettings.Certificate == null)
            {
                client.Close();
                return;
            }
            var session = CreateRandomSession();
            session.NetworkClient = client;
            session.Ip = client.Client.RemoteEndPoint.ToString();
            var ind = session.Ip.LastIndexOf(':');
            if (ind != -1) session.Ip = session.Ip.Remove(ind);
            AllSessions.Add(session);
            Task.Run(() =>
            {
                var stream = new SslStream(client.GetStream(), false);
                session.NetworkStream = stream;
                stream.AuthenticateAsServer(SecureSettings.Certificate, false, SslProtocols.Default, true);
                if (!stream.IsAuthenticated)
                {
                    stream.Dispose();
                    client.Close();
                    AllSessions.Remove(session);
                    return;
                }

                ClientStartListen(session);
            });
        }
    }

    public class SecureWebServerSettings : WebServerSettings
    {
        public int SecurePort { get; private set; }

        bool enableUnsafePort = true;
        public bool EnableUnsafePort
        {
            get => enableUnsafePort;
            set => enableUnsafePort = value;
        }

        public X509Certificate Certificate { get; set; }

        public SecureWebServerSettings(string settingFolderPath)
            : base(settingFolderPath)
        {
        }

        public SecureWebServerSettings(int port, int securePort, int connectionTimeout)
            : base(port, connectionTimeout)
        {
            SecurePort = securePort;
        }

        public SecureWebServerSettings(int securePort, int connectionTimeout)
            : base(80, connectionTimeout)
        {
            SecurePort = securePort;
            EnableUnsafePort = false;
        }

        protected override void Load_Server(OptionsLoader set)
        {
            base.Load_Server(set);
            var server = set["Server"].Options;
            SecurePort = server.GetInt32("SecurePort", 443);
            EnableUnsafePort = server.GetBool("EnableUnsafePort", true);
        }
    }
}
