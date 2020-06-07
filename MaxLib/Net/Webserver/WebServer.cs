using MaxLib.Collections;
using System;
using System.Collections.Generic;
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
            WebServiceGroups[WebServiceType.PreParseRequest].Add(new Services.HttpHeaderParser());
            WebServiceGroups[WebServiceType.PostParseRequest].Add(new Services.HttpHeaderPostParser());
            WebServiceGroups[WebServiceType.PostParseRequest].Add(new Services.HttpDocumentFinder());
            WebServiceGroups[WebServiceType.PostParseRequest].Add(new Services.HttpHeaderSpecialAction());
            WebServiceGroups[WebServiceType.PreCreateDocument].Add(new Services.StandardDocumentLoader());
            WebServiceGroups[WebServiceType.PreCreateDocument].Add(new Services.HttpDirectoryMapper(true));
            WebServiceGroups[WebServiceType.PreCreateResponse].Add(new Services.HttpResponseCreator());
            WebServiceGroups[WebServiceType.SendResponse].Add(new Services.HttpSender());
        }

        public virtual void AddWebService(WebService webService)
        {
            if (webService == null) return;
            WebServiceGroups[webService.ServiceType].Add(webService);
        }

        public virtual bool ContainsWebService(WebService webService)
        {
            if (webService == null) return false;
            return WebServiceGroups[webService.ServiceType].Contains(webService);
        }

        public virtual void RemoveWebService(WebService webService)
        {
            if (webService == null) return;
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
            while (ServerExecution)
            {
                var start = Environment.TickCount;
                //Ausstehende Verbindungen abfragen
                int step = 0;
                while (step < 10)
                {
                    if (!Listener.Pending()) break;
                    step++;
                    ClientConnected(Listener.AcceptTcpClient());
                }
                step = 10;
                //Keep Alive Verbindungen abfragen
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
                        step++;
                        continue;
                    }
                    if (kas.NetworkClient.Available > 0 && kas.LastWorkTime != -1)
                    {
                        var task = new Task((session) => SafeClientStartListen((HttpSession)session), kas);
                        task.Start();
                        step++;
                    }
                }

                //Warten
                var stop = Environment.TickCount;
                if (Listener.Pending()) continue;
                var time = (stop - start) % 20;
                Thread.Sleep(20 - time);
            }
            Listener.Stop();
            for (int i = 0; i < AllSessions.Count; ++i) AllSessions[i].NetworkClient.Close();
            AllSessions.Clear();
            KeepAliveSessions.Clear();
            WebServerLog.Add(ServerLogType.Information, GetType(), "StartUp", "Server succesfuly stopped");
        }

        protected virtual void ClientConnected(TcpClient client)
        {
            //Session vorbereiten
            var session = CreateRandomSession();
            session.NetworkClient = client;
            session.Ip = client.Client.RemoteEndPoint.ToString();
            var ind = session.Ip.IndexOf(':');
            if (ind != -1) session.Ip = session.Ip.Remove(ind);
            AllSessions.Add(session);
            //Verbindung abhorchen
            var task = new Task(() => ClientStartListen(session));
            task.Start();
        }

        protected virtual void SafeClientStartListen(HttpSession session)
        {
            if (System.Diagnostics.Debugger.IsAttached)
                ClientStartListen(session);
            else
            {
                try { ClientStartListen(session); }
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

        protected virtual void ClientStartListen(HttpSession session)
        {
            session.LastWorkTime = -1;
            if (session.NetworkClient.Connected)
            {
                WebServerLog.Add(ServerLogType.Information, GetType(), "Connection", "Listen to Connection {0}", session.NetworkClient.Client.RemoteEndPoint);
                var task = PrepairProgressTask(session);
                if (task == null)
                {
                    ClientStartListen(session);
                    return;
                }
                while (true)
                {
                    WebServiceGroups[task.CurrentTask].Execute(task);
                    if (task.CurrentTask == WebServiceType.SendResponse) break;
                    task.CurrentTask = task.NextTask;
                    task.NextTask = task.NextTask == WebServiceType.SendResponse ?
                        WebServiceType.SendResponse :
                        (WebServiceType)((int)task.NextTask + 1);
                }
                if (task.Document.RequestHeader.FieldConnection == HttpConnectionType.KeepAlive)
                {
                    if (!KeepAliveSessions.Contains(session)) KeepAliveSessions.Add(session);
                }
                else
                {
                    if (KeepAliveSessions.Contains(session)) KeepAliveSessions.Remove(session);
                    AllSessions.Remove(session);
                    session.NetworkClient.Close();
                }
                session.LastWorkTime = Environment.TickCount;
                task.Dispose();
            }
            else
            {
                if (KeepAliveSessions.Contains(session)) KeepAliveSessions.Remove(session);
                AllSessions.Remove(session);
                session.NetworkClient.Close();
            }
        }

        internal protected virtual void ListenFromTaskCreator(WebProgressTask task, WebServiceType terminationState = WebServiceType.SendResponse)
        {
            if (task == null) return;
            while (true)
            {
                WebServiceGroups[task.CurrentTask].Execute(task);
                if (task.CurrentTask == terminationState) break;
                task.CurrentTask = task.NextTask;
                task.NextTask = task.NextTask == WebServiceType.SendResponse ?
                    WebServiceType.SendResponse :
                    (WebServiceType)((int)task.NextTask + 1);
            }
        }

        protected virtual WebProgressTask PrepairProgressTask(HttpSession session)
        {
            var task = new WebProgressTask
            {
                CurrentTask = WebServiceType.PreParseRequest,
                Document = new HttpDocument
                {
                    RequestHeader = new HttpRequestHeader(),
                    ResponseHeader = new HttpResponseHeader(),
                    Session = session
                }
            };
            if (session.NetworkStream != null)
                task.NetworkStream = session.NetworkStream;
            else try { session.NetworkStream = task.NetworkStream = session.NetworkClient.GetStream(); }
                catch { return null; }
            task.NextTask = WebServiceType.PostParseRequest;
            task.Server = this;
            task.Session = session;
            return task;
        }

        protected virtual HttpSession CreateRandomSession()
        {
            var s = new HttpSession();
            var r = new Random();
            do
            {
                var b = new byte[8];
                r.NextBytes(b);
                s.InternalSessionKey = BitConverter.ToInt64(b, 0);
            }
            while (AllSessions.Exists((ht) => ht != null && ht.InternalSessionKey == s.InternalSessionKey));
            do
            {
                s.PublicSessionKey = new byte[16];
                r.NextBytes(s.PublicSessionKey);
            }
            while (AllSessions.Exists((ht) => ht != null && WebServerUtils.BytesEqual(ht.PublicSessionKey, s.PublicSessionKey)));
            s.LastWorkTime = -1;
            return s;
        }
    }
}
