using MaxLib.Collections;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

//Source: Wikipedia, SelfHTML

namespace MaxLib.Net.Webserver
{
    public class WebServer
    {
        public FullDictionary<WebServiceType, WebServiceGroup> WebServiceGroups { get; }

        public WebServerSettings Settings { get; protected set; }

        //Serveraktivitäten

        protected TcpListener Listener;
        protected Thread ServerThread;
        public bool ServerExecution { get; protected set; }
        public SyncedList<HttpSession> KeepAliveSessions { get; } = new SyncedList<HttpSession>();
        public SyncedList<HttpSession> AllSessions { get; } = new SyncedList<HttpSession>();

        public WebServer(WebServerSettings settings)
        {
            Settings = settings;
            WebServiceGroups = new FullDictionary<WebServiceType, WebServiceGroup>((k) => new WebServiceGroup(k));
            WebServiceGroups.FullEnumKeys();
        }

        public virtual void InitialDefault()
        {
            //Pre parse request
            AddWebService(new Services.HttpHeaderParser());
            //post parse request
            AddWebService(new Services.HttpHeaderPostParser());
            AddWebService(new Services.HttpDocumentFinder());
            AddWebService(new Services.HttpHeaderSpecialAction());
            //pre create document
            AddWebService(new Services.StandardDocumentLoader());
            AddWebService(new Services.HttpDirectoryMapper(true));
            //pre create response
            AddWebService(new Services.HttpResponseCreator());
            //send response
            AddWebService(new Services.HttpSender());
        }

        public virtual void AddWebService(WebService webService)
        {
            _ = webService ?? throw new ArgumentNullException(nameof(webService));
            WebServiceGroups[webService.ServiceType].Add(webService);
        }

        public virtual bool ContainsWebService(WebService webService)
        {
            if (webService == null) 
                return false;
            return WebServiceGroups[webService.ServiceType].Contains(webService);
        }

        public virtual void RemoveWebService(WebService webService)
        {
            if (webService == null) 
                return;
            WebServiceGroups[webService.ServiceType].Remove(webService);
        }

        public virtual void Start()
        {
            WebServerLog.Add(ServerLogType.Information, GetType(), "StartUp", "Start Server on Port {0}", Settings.Port);
            ServerExecution = true;
            Listener = new TcpListener(new IPEndPoint(Settings.IPFilter, Settings.Port));
            Listener.Start();
            ServerThread = new Thread(ServerMainTask)
            {
                Name = "ServerThread - Port: " + Settings.Port.ToString()
            };
            ServerThread.Start();
        }

        public virtual void Stop()
        {
            WebServerLog.Add(ServerLogType.Information, GetType(), "StartUp", "Stopped Server");
            ServerExecution = false;
            ServerThread.Join();
        }
        
        protected virtual void ServerMainTask()
        {
            WebServerLog.Add(ServerLogType.Information, GetType(), "StartUp", "Server succesfuly started");
            var watch = new Stopwatch();
            while (ServerExecution)
            {
                watch.Restart();
                //request pending connections
                int step = 0;
                while (step < 10)
                {
                    if (!Listener.Pending()) break;
                    step++;
                    ClientConnected(Listener.AcceptTcpClient());
                }
                //request keep alive connections
                for (int i = 0; i < KeepAliveSessions.Count; ++i)
                {
                    HttpSession kas;
                    try { kas = KeepAliveSessions[i]; }
                    catch { continue; }
                    if (kas == null) continue;

                    if (!kas.NetworkClient.Connected || (kas.LastWorkTime != -1 &&
                        kas.LastWorkTime + Settings.ConnectionTimeout < Environment.TickCount))
                    {
                        kas.NetworkClient.Close();
                        kas.NetworkStream?.Dispose();
                        AllSessions.Remove(kas);
                        KeepAliveSessions.Remove(kas);
                        --i;
                        continue;
                    }

                    if (kas.NetworkClient.Available > 0 && kas.LastWorkTime != -1)
                    {
                        _ = Task.Run(() => SafeClientStartListen(kas));
                    }
                }

                //Warten
                if (Listener.Pending()) 
                    continue;
                var time = watch.ElapsedMilliseconds % 20;
                Thread.Sleep(20 - (int)time);
            }
            watch.Stop();
            Listener.Stop();
            for (int i = 0; i < AllSessions.Count; ++i) 
                AllSessions[i].NetworkClient.Close();
            AllSessions.Clear();
            KeepAliveSessions.Clear();
            WebServerLog.Add(ServerLogType.Information, GetType(), "StartUp", "Server succesfuly stopped");
        }

        protected virtual void ClientConnected(TcpClient client)
        {
            //prepare session
            var session = CreateRandomSession();
            session.NetworkClient = client;
            session.Ip = client.Client.RemoteEndPoint is IPEndPoint iPEndPoint
                ? iPEndPoint.Address.ToString()
                : client.Client.RemoteEndPoint.ToString();
            AllSessions.Add(session);
            //listen to connection
            _ = Task.Run(() => SafeClientStartListen(session));
        }

        protected virtual async Task SafeClientStartListen(HttpSession session)
        {
            if (Debugger.IsAttached)
                await ClientStartListen(session);
            else
            {
                try { await ClientStartListen(session); }
                catch (Exception e)
                {
                    WebServerLog.Add(
                        ServerLogType.FatalError, 
                        GetType(), 
                        "Unhandled Exception", 
                        $"{e.GetType().FullName}: {e.Message} in {e.StackTrace}");
                }
            }
        }

        protected virtual async Task ClientStartListen(HttpSession session)
        {
            session.LastWorkTime = -1;
            if (session.NetworkClient.Connected)
            {
                WebServerLog.Add(ServerLogType.Information, GetType(), "Connection", "Listen to Connection {0}", 
                    session.NetworkClient.Client.RemoteEndPoint);
                var task = PrepairProgressTask(session);
                if (task == null)
                {
                    WebServerLog.Add(ServerLogType.Information, GetType(), "Connection",
                        $"Cannot establish data stream to {session.Ip}");
                    RemoveSession(session);
                    return;
                }

                await ExecuteTaskChain(task, WebServiceType.SendResponse);

                if (task.Document.RequestHeader.FieldConnection == HttpConnectionType.KeepAlive)
                {
                    if (!KeepAliveSessions.Contains(session)) 
                        KeepAliveSessions.Add(session);
                }
                else RemoveSession(session);

                session.LastWorkTime = Environment.TickCount;
                task.Dispose();
            }
            else RemoveSession(session);
        }

        protected void RemoveSession(HttpSession session)
        {
            _ = session ?? throw new ArgumentNullException(nameof(session));
            if (KeepAliveSessions.Contains(session))
                KeepAliveSessions.Remove(session);
            AllSessions.Remove(session);
            session.NetworkClient.Close();
        }

        internal protected virtual async Task ExecuteTaskChain(WebProgressTask task, WebServiceType terminationState = WebServiceType.SendResponse)
        {
            if (task == null) return;
            while (true)
            {
                await WebServiceGroups[task.CurrentTask].Execute(task);
                if (task.CurrentTask == terminationState) 
                    break;
                task.CurrentTask = task.NextTask;
                task.NextTask = task.NextTask == WebServiceType.SendResponse
                    ? WebServiceType.SendResponse
                    : (WebServiceType)((int)task.NextTask + 1);
            }
        }

        protected virtual WebProgressTask PrepairProgressTask(HttpSession session)
        {
            var stream = session.NetworkStream;
            if (stream == null)
                try
                {
                    stream = session.NetworkStream = session.NetworkClient.GetStream();
                }
                catch (InvalidOperationException)
                { return null; }
            return new WebProgressTask
            {
                CurrentTask = WebServiceType.PreParseRequest,
                Document = new HttpDocument
                {
                    RequestHeader = new HttpRequestHeader(),
                    ResponseHeader = new HttpResponseHeader(),
                    Session = session
                },
                NextTask = WebServiceType.PostParseRequest,
                Server = this,
                Session = session,
                NetworkStream = stream,
            };
        }

        protected virtual HttpSession CreateRandomSession()
        {
            var s = new HttpSession();
            var r = new Random();
            do
            {
                s.SessionKey = new byte[16];
                r.NextBytes(s.SessionKey);
            }
            while (AllSessions.Exists((ht) => ht != null && WebServerUtils.BytesEqual(ht.SessionKey, s.SessionKey)));
            s.LastWorkTime = -1;
            return s;
        }
    }
}
