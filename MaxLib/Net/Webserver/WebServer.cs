using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MaxLib.Data.IniFiles;
using MaxLib.Collections;

//Source: Wikipedia, SelfHTML

namespace MaxLib.Net.Webserver
{
    #region WebServer

    public class WebServer
    {
        private Dictionary<WebServiceType, WebServiceGroup> webServiceGroups = new Dictionary<WebServiceType, WebServiceGroup>();
        public Dictionary<WebServiceType, WebServiceGroup> WebServiceGroups
        {
            get { return webServiceGroups; }
        }

        public WebServerSettings Settings { get; protected set; }

        //Serveraktivitäten

        protected TcpListener Listener;
        protected Thread ServerThread;
        public bool ServerExecution { get; protected set; }

        //Sessions

        private SyncedList<HttpSession> keepAliveSessions = new SyncedList<HttpSession>();
        public SyncedList<HttpSession> KeepAliveSessions
        {
            get { return keepAliveSessions; }
        }

        private SyncedList<HttpSession> allSessions = new SyncedList<HttpSession>();
        public SyncedList<HttpSession> AllSessions
        {
            get { return allSessions; }
        }

        public WebServer(WebServerSettings settings)
        {
            Settings = settings;
            for (int i = 1; i <= 7; ++i) WebServiceGroups.Add((WebServiceType)i, new WebServiceGroup((WebServiceType)i));
        }

        public virtual void InitialDefault()
        {
            WebServiceGroups[WebServiceType.PreParseRequest].WebServices.Add(new Services.HttpHeaderParser());
            WebServiceGroups[WebServiceType.PostParseRequest].WebServices.Add(new Services.HttpHeaderPostParser());
            WebServiceGroups[WebServiceType.PostParseRequest].WebServices.Add(new Services.HttpDocumentFinder());
            WebServiceGroups[WebServiceType.PostParseRequest].WebServices.Add(new Services.HttpHeaderSpecialAction());
            WebServiceGroups[WebServiceType.PreCreateDocument].WebServices.Add(new Services.StandartDocumentLoader());
            WebServiceGroups[WebServiceType.PreCreateDocument].WebServices.Add(new Services.HttpDirectoryMapper(true));
            WebServiceGroups[WebServiceType.PreCreateResponse].WebServices.Add(new Services.HttpResponseCreator());
            WebServiceGroups[WebServiceType.SendResponse].WebServices.Add(new Services.HttpSender());
        }

        public virtual void AddWebService(WebService webService)
        {
            if (webService == null) return;
            webServiceGroups[webService.ServiceType].WebServices.Add(webService);
        }

        public virtual bool ContainsWebService(WebService webService)
        {
            if (webService == null) return false;
            return webServiceGroups[webService.ServiceType].WebServices.Contains(webService);
        }

        public virtual void RemoveWebService(WebService webService)
        {
            if (webService == null) return;
            webServiceGroups[webService.ServiceType].WebServices.Remove(webService);
        }

        public virtual void Start()
        {
            WebServerInfo.Add(InfoType.Information, GetType(), "StartUp", "Start Server on Port {0}", Settings.Port);
            ServerExecution = true;
            Listener = new TcpListener(new IPEndPoint(IPAddress.Any, Settings.Port));
            Listener.Start();
            ServerThread = new Thread(ServerMainTask);
            ServerThread.Name = "ServerThread - Port: " + Settings.Port.ToString();
            ServerThread.Start();
        }

        public virtual void Stop()
        {
            WebServerInfo.Add(InfoType.Information, GetType(), "StartUp", "Stopped Server");
            ServerExecution = false;
        }

        static object lockObject = new object();
        protected virtual void ServerMainTask()
        {
            WebServerInfo.Add(InfoType.Information, GetType(), "StartUp", "Server succesfuly started");
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
                lock (lockObject)
                {
                    for (int i = 0; i < KeepAliveSessions.Count; ++i)
                    {
                        HttpSession kas;
                        try { kas = KeepAliveSessions[i]; }
                        catch { continue; }
                        if (kas == null) continue;
                        if (!kas.NetworkCient.Connected || (kas.LastWorkTime != -1 &&
                            kas.LastWorkTime + Settings.ConnectionTimeout < Environment.TickCount))
                        {
                            kas.NetworkCient.Close();
                            AllSessions.Remove(kas);
                            KeepAliveSessions.Remove(kas);
                            --i;
                            step++;
                            continue;
                        }
                        if (kas.NetworkCient.Available > 0 && kas.LastWorkTime != -1)
                        {
                            var task = new Task((session) => SecureClientStartListen((HttpSession)session), kas);
                            task.Start();
                            step++;
                        }
                    }
                }
                //Warten
                var stop = Environment.TickCount;
                if (Listener.Pending()) continue;
                var time = (stop - start) % 20;
                Thread.Sleep(20 - time);
            }
            Listener.Stop();
            for (int i = 0; i < AllSessions.Count; ++i) AllSessions[i].NetworkCient.Close();
            AllSessions.Clear();
            KeepAliveSessions.Clear();
            WebServerInfo.Add(InfoType.Information, GetType(), "StartUp", "Server succesfuly stopped");
        }

        protected virtual void ClientConnected(TcpClient client)
        {
            //WebServerInfo.Add(InfoType.Information, GetType(), "Connection", "Connection received on {0}", client.Client.RemoteEndPoint);
            //Session vorbereiten
            var session = CreateRandomSession();
            session.NetworkCient = client;
            session.Ip = client.Client.RemoteEndPoint.ToString();
            var ind = session.Ip.IndexOf(':');
            if (ind != -1) session.Ip = session.Ip.Remove(ind);
            allSessions.Add(session);
            //Verbindung abhorchen
            var task = new Task(() => ClientStartListen(session));
            task.Start();
        }

        protected virtual void SecureClientStartListen(HttpSession session)
        {
            if (System.Diagnostics.Debugger.IsAttached)
                ClientStartListen(session);
            else
            {
                try { ClientStartListen(session); }
                catch (Exception e)
                {
                    WebServerInfo.Add(InfoType.FatalError, GetType(), "Unhandled Exception", e, "{0} in {1}", e.Message, e.StackTrace);
                }
            }
        }

        protected virtual void ClientStartListen(HttpSession session)
        {
            session.LastWorkTime = -1;
            if (session.NetworkCient.Connected)
            {
                WebServerInfo.Add(InfoType.Information, GetType(), "Connection", "Listen to Connection {0}", session.NetworkCient.Client.RemoteEndPoint);
                var task = PrepairProgressTask(session);
                if (task == null)
                {
                    ClientStartListen(session);
                    return;
                }
                while (true)
                {
                    webServiceGroups[task.CurrentTask].Execute(task);
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
                    session.NetworkCient.Close();
                }
                session.LastWorkTime = Environment.TickCount;
                task.Dispose();
            }
            else
            {
                if (KeepAliveSessions.Contains(session)) KeepAliveSessions.Remove(session);
                AllSessions.Remove(session);
                session.NetworkCient.Close();
            }
        }

        internal protected virtual void ListenFromTaskCreator(WebProgressTask task, WebServiceType terminationState = WebServiceType.SendResponse)
        {
            if (task == null) return;
            while (true)
            {
                webServiceGroups[task.CurrentTask].Execute(task);
                if (task.CurrentTask == terminationState) break;
                task.CurrentTask = task.NextTask;
                task.NextTask = task.NextTask == WebServiceType.SendResponse ?
                    WebServiceType.SendResponse :
                    (WebServiceType)((int)task.NextTask + 1);
            }
        }

        protected virtual WebProgressTask PrepairProgressTask(HttpSession session)
        {
            var task = new WebProgressTask();
            task.CurrentTask = WebServiceType.PreParseRequest;
            task.Document = new HttpDocument();
            task.Document.RequestHeader = new HttpRequestHeader();
            task.Document.ResponseHeader = new HttpResponseHeader();
            task.Document.Session = session;
            try { task.NetworkStream = session.NetworkCient.GetStream(); }
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
            while (allSessions.Exists((ht) => ht != null && ht.InternalSessionKey == s.InternalSessionKey));
            do
            {
                s.PublicSessionKey = new byte[16];
                r.NextBytes(s.PublicSessionKey);
            }
            while (allSessions.Exists((ht) => ht != null && WebServerHelper.BytesEqual(ht.PublicSessionKey, s.PublicSessionKey)));
            s.LastWorkTime = -1;
            return s;
        }
    }

    public class WebServerSettings
    {
        public int Port { get; private set; }

        public int ConnectionTimeout { get; private set; }

        //Debug
        public bool Debug_WriteRequests = false;
        public bool Debug_LogConnections = false;

        private Dictionary<string, string> defaultFileMimeAssociation = new Dictionary<string, string>();
        public Dictionary<string, string> DefaultFileMimeAssociation
        {
            get { return defaultFileMimeAssociation; }
        }

        protected enum SettingTypes
        {
            MimeAssociation,
            ServerSettings
        }

        public string SettingsPath { get; private set; }

        public virtual void LoadSettingFromData(string data)
        {
            var sf = new OptionsLoader(data);
            var type = sf["Setting"].Options.GetEnum<SettingTypes>("Type");
            switch (type)
            {
                case SettingTypes.MimeAssociation: Load_Mime(sf); break;
                case SettingTypes.ServerSettings: Load_Server(sf); break;
            }
        }

        public virtual void LoadSetting(string path)
        {
            SettingsPath = path;
            var sf = new OptionsLoader(path, false);
            var type = sf["Setting"].Options.GetEnum<SettingTypes>("Type");
            switch (type)
            {
                case SettingTypes.MimeAssociation: Load_Mime(sf); break;
                case SettingTypes.ServerSettings: Load_Server(sf); break;
            }
        }

        protected virtual void Load_Mime(OptionsLoader set)
        {
            DefaultFileMimeAssociation.Clear();
            var gr = set["Mime"].Options.GetSearch().FilterKeys(true);
            foreach (var keypair in gr)
            {
                if (defaultFileMimeAssociation.ContainsKey((keypair as OptionsKey).Name)) { }
                defaultFileMimeAssociation.Add((keypair as OptionsKey).Name, (keypair as OptionsKey).GetString());
            }
        }

        protected virtual void Load_Server(OptionsLoader set)
        {
            var server = set["Server"].Options;
            Port = server.GetInt32("Port", 80);
            ConnectionTimeout = server.GetInt32("ConnectionTimeout", 2000);
        }

        public WebServerSettings(string settingFolderPath)
        {
            foreach (var file in Directory.GetFiles(settingFolderPath))
                if (file.EndsWith(".ini"))
                    LoadSetting(file);
        }

        public WebServerSettings(int port, int connectionTimeout)
        {
            Port = port;
            ConnectionTimeout = connectionTimeout;
        }
    }

    public static class WebServerHelper
    {
        public static string EncodeUri(string uri)
        {
            return WebUtility.UrlEncode(uri);
        }

        public static string DecodeUri(string uri)
        {
            return WebUtility.UrlDecode(uri);
        }

        public static string GetVolumeString(long byteCount, bool shortVersion, int digits)
        {
            var sn = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            var ln = new[] { "Byte", "Kilobyte", "Megabyte", "Gigabyte", "Terabyte", "Petabyte", "Exabyte", "Zettabyte", "Yottabyte" };
            var names = shortVersion ? sn : ln;
            var step = 0;
            var bc = (double)byteCount;
            while (bc >= 1024)
            {
                step++;
                bc /= 1024;
            }
            if (step >= names.Length) throw new ArgumentOutOfRangeException("byteCount");
            var vkd = bc < 1000 ? bc < 100 ? bc < 10 ? 1 : 2 : 3 : 4;
            digits = Math.Max(Math.Min(digits, vkd + 3 * step), vkd);
            var mask = vkd == 4 ? "0,000" : new string('0', vkd);
            if (digits > vkd) mask += "." + new string('#', digits - vkd);
            return (bc.ToString(mask) + " " + names[step]).TrimEnd('.', ',');
        }

        public static string GetDateString(DateTime date)
        {
            var mn = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", 
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var format = "{0}, {1:00} {2} {3:0000} {4:00}:{5:00}:{6:00} GMT";
            var s = string.Format(format, date.DayOfWeek.ToString().Substring(0, 3),
                date.Day, mn[date.Month - 1], date.Year,
                date.Hour, date.Minute, date.Second);
            return s;
        }

        public static DateTime GetDateFromString(string date)
        {
            var tiles = date.Split(new char[] { ',', ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
            var min = DateTime.MinValue;
            int day = min.Day, month = min.Month, year = min.Year, hour = min.Hour, minute = min.Minute, second = min.Second;
            var mn = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", 
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" }.ToList();
            if (tiles.Length >= 2) int.TryParse(tiles[1], out day);
            if (tiles.Length >= 3) month = mn.IndexOf(tiles[2]);
            if (tiles.Length >= 4) int.TryParse(tiles[3], out year);
            if (tiles.Length >= 5) int.TryParse(tiles[4], out hour);
            if (tiles.Length >= 6) int.TryParse(tiles[5], out minute);
            if (tiles.Length >= 7) int.TryParse(tiles[6], out second);
            return new DateTime(year, month, day, hour, minute, second);
        }

        public static bool BytesEqual(byte[] ba1, byte[] ba2)
        {
            if (ba1 == null) throw new ArgumentNullException("ba1");
            if (ba2 == null) throw new ArgumentNullException("ba2");
            if (ba1.Length != ba2.Length) return false;
            for (int i = 0; i < ba1.Length; ++i) if (ba1[i] != ba2[i]) return false;
            return true;
        }
    }

    #endregion

    #region Session -> Static Http Session

    namespace Session
    {
        public class SessionInformation
        {
            public string HexKey { get; private set; }

            public byte[] ByteKey { get; private set; }

            public DateTime Generated { get; private set; }

            public Dictionary<object, object> Information { get; private set; }

            public SessionInformation(string hexkey, byte[] bytekey, DateTime generated)
            {
                HexKey = hexkey;
                ByteKey = bytekey;
                Generated = generated;
                Information = new Dictionary<object, object>();
            }
        }

        public static class SessionManager
        {
            static List<SessionInformation> Sessions = new List<SessionInformation>();

            public static void Register(WebProgressTask task)
            {
                var cookie = task.Document.RequestHeader.Cookie.Get("Session");
                if (cookie == null)
                {
                    var si = RegisterNewSession(task.Session);
                    task.Session.PublicSessionKey = si.ByteKey;
                    task.Session.AlwaysSyncSessionInformation(si.Information);
                    task.Document.RequestHeader.Cookie.AddedCookies.Add(new HttpCookie.Cookie("Session", si.HexKey, 3600));
                }
                else
                {
                    if (!RegisterSession(task.Session, cookie.Value))
                        task.Document.RequestHeader.Cookie.AddedCookies.Add(
                            new HttpCookie.Cookie("Session", Get(task.Session.PublicSessionKey).HexKey, 3600));
                }
            }

            public static bool RegisterSession(HttpSession session, string hexkey)
            {
                var si = Get(hexkey);
                var added = false;
                if (si == null) si = RegisterNewSession(session);
                else added = true;
                session.PublicSessionKey = si.ByteKey;
                session.AlwaysSyncSessionInformation(si.Information);
                return added;
            }

            public static bool RegisterSession(HttpSession session, byte[] binkey)
            {
                var si = Get(binkey);
                var added = false;
                if (si == null) si = RegisterNewSession(session);
                else added = true;
                session.PublicSessionKey = si.ByteKey;
                session.AlwaysSyncSessionInformation(si.Information);
                return !added;
            }

            public static SessionInformation RegisterNewSession(HttpSession session)
            {
                byte[] bkey;
                var key = GenerateSessionKey(out bkey);
                session.PublicSessionKey = bkey;
                var si = new SessionInformation(key, bkey, DateTime.Now);
                Sessions.Add(si);
                return si;
            }

            public static SessionInformation Get(string hexkey)
            {
                return Sessions.Find((si) => si != null && si.HexKey == hexkey);
            }

            public static SessionInformation Get(byte[] binkey)
            {
                return Sessions.Find((si) => WebServerHelper.BytesEqual(si.ByteKey, binkey));
            }

            public static void DeleteSession(string hexkey)
            {
                var ind = Sessions.FindIndex((si) => si.HexKey == hexkey);
                if (ind != -1) Sessions.RemoveAt(ind);
            }

            public static void DeleteSession(byte[] binkey)
            {
                var ind = Sessions.FindIndex((si) => si.ByteKey == binkey);
                if (ind != -1) Sessions.RemoveAt(ind);
            }

            const string hex = "0123456789ABCDEF";

            static string GenerateSessionKey(out byte[] b)
            {
                var r = new Random();
                while (true)
                {
                    b = new byte[16];
                    r.NextBytes(b);
                    var h = "";
                    for (int i = 0; i < b.Length; ++i) h += hex[b[i] / 16].ToString() + hex[b[i] % 16].ToString();
                    if (!Sessions.Exists((si) => si.HexKey == h)) return h;
                }
            }



        }
    }

    #endregion

    #region Services -> Default Web Services

    namespace Services
    {
        /// <summary>
        /// WebServiceType.PreParseRequest: Liest und parst den Header aus dem Netzwerk-Stream und sammelt alle Informationen
        /// </summary>
        public class HttpHeaderParser : WebService
        {
            static object lockHeaderFile = new object();
            static object lockRequestFile = new object();

            /// <summary>
            /// WebServiceType.PreParseRequest: Liest und parst den Header aus dem Netzwerk-Stream und sammelt alle Informationen
            /// </summary>
            public HttpHeaderParser() : base(WebServiceType.PreParseRequest) { }

            public override void ProgressTask(WebProgressTask task)
            {
                var header = task.Document.RequestHeader;
                var stream = task.NetworkStream;
                var reader = new StreamReader(stream);
                var mwt = 50;
                var sb = new StringBuilder();
                if (task.Server.Settings.Debug_WriteRequests)
                {
                    sb.AppendLine(new string('=', 100));
                    var date = WebServerHelper.GetDateString(DateTime.Now);
                    sb.AppendLine("=   " + date + new string(' ', 95 - date.Length) + "=");
                    sb.AppendLine(new string('=', 100));
                    sb.AppendLine();
                }
                while (!((NetworkStream)stream).DataAvailable && mwt > 0)
                {
                    Thread.Sleep(100);
                    mwt--;
                    if (!task.Session.NetworkCient.Connected) return;
                }
                try
                {
                    if (!((NetworkStream)stream).DataAvailable)
                    {
                        task.Document.RequestHeader.FieldConnection = HttpConnectionType.KeepAlive;
                        WebServerInfo.Add(InfoType.Error, GetType(), "Header", "Request Time out");
                        task.Document.ResponseHeader.StatusCode = HttpStateCode.RequestTimeOut;
                        task.NextTask = WebServiceType.PreCreateResponse;
                        return;
                    }
                }
                catch (ObjectDisposedException)
                {
                    WebServerInfo.Add(InfoType.Error, GetType(), "Header", "Connection closed by remote host");
                    task.Document.ResponseHeader.StatusCode = HttpStateCode.RequestTimeOut;
                    task.NextTask = task.CurrentTask = WebServiceType.SendResponse;
                    return;
                }
                string line;
                try { line = reader.ReadLine(); }
                catch
                {
                    WebServerInfo.Add(InfoType.Error, GetType(), "Header", "Connection closed by remote host");
                    task.Document.ResponseHeader.StatusCode = HttpStateCode.RequestTimeOut;
                    task.NextTask = task.CurrentTask = WebServiceType.SendResponse;
                    return;
                }
                if (line == null)
                {
                    WebServerInfo.Add(InfoType.Error, GetType(), "Header", "Can't read Header line");
                    task.Document.ResponseHeader.StatusCode = HttpStateCode.BadRequest;
                    task.NextTask = WebServiceType.PreCreateResponse;
                    return;
                }
                try
                {
                    if (task.Server.Settings.Debug_WriteRequests) sb.AppendLine(line);
                    var parts = line.Split(' ');
                    WebServerInfo.Add(InfoType.Debug, GetType(), "Header", line);
                    header.ProtocolMethod = parts[0];
                    header.Url = parts[1];
                    header.HttpProtocol = parts[2];
                    while (!string.IsNullOrWhiteSpace((line = reader.ReadLine())))
                    {
                        if (task.Server.Settings.Debug_WriteRequests) sb.AppendLine(line);
                        //WebServerInfo.Add(InfoType.Debug, GetType(), "Header", line);
                        var ind = line.IndexOf(':');
                        var key = line.Remove(ind);
                        var value = line.Substring(ind + 1).Trim();
                        header.HeaderParameter.Add(key, value);
                    }
                    if (task.Server.Settings.Debug_WriteRequests) sb.AppendLine();
                }
                catch
                {
                    WebServerInfo.Add(InfoType.Error, GetType(), "Header", "Bad Request");
                    task.Document.ResponseHeader.StatusCode = HttpStateCode.BadRequest;
                    task.NextTask = WebServiceType.PreCreateResponse;
                    return;
                }
                if (header.ProtocolMethod == HttpProtocollMethods.Post)
                {
                    var buffer = new char[int.Parse(header.HeaderParameter["Content-Length"])];
                    var count = reader.ReadBlock(buffer, 0, buffer.Length);
                    header.Post.SetPost(new string(buffer));
                    if (task.Server.Settings.Debug_WriteRequests) sb.AppendLine(new string(buffer));
                }
                if (task.Server.Settings.Debug_WriteRequests)
                {
                    sb.AppendLine(); sb.AppendLine();
                    lock (lockHeaderFile) File.AppendAllText("headers.txt", sb.ToString());
                }
                if (task.Server.Settings.Debug_LogConnections)
                {
                    sb = new StringBuilder();
                    sb.AppendLine(WebServerHelper.GetDateString(DateTime.Now) + "  " +
                        task.Session.NetworkCient.Client.RemoteEndPoint.ToString());
                    var host = header.HeaderParameter.ContainsKey("Host") ? header.HeaderParameter["Host"] : "";
                    sb.AppendLine("    " + host + task.Document.RequestHeader.Location.DocumentPath);
                    sb.AppendLine();
                    lock (lockRequestFile) File.AppendAllText("requests.txt", sb.ToString());
                }
            }

            public override bool CanWorkWith(WebProgressTask task)
            {
                return true;
            }
        }
        /// <summary>
        /// WebServiceType.PostParseRequest: Ließt alle Informationen aus dem Header aus und analysiert diese. 
        /// Die Headerklasse wird für die weitere Verwendung vorbereitet.
        /// </summary>
        public class HttpHeaderPostParser : WebService
        {
            /// <summary>
            /// WebServiceType.PostParseRequest: Ließt alle Informationen aus dem Header aus und analysiert diese. 
            /// Die Headerklasse wird für die weitere Verwendung vorbereitet.
            /// </summary>
            public HttpHeaderPostParser()
                : base(WebServiceType.PostParseRequest)
            {
                Importance = WebProgressImportance.High;
            }

            public override void ProgressTask(WebProgressTask task)
            {
                var header = task.Document.RequestHeader;
                //Accept
                if (header.HeaderParameter.ContainsKey("Accept"))
                {
                    var tiles = header.HeaderParameter["Accept"].Split(
                        new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    header.FieldAccept.AddRange(tiles);
                }
                //Accept-Encoding
                if (header.HeaderParameter.ContainsKey("Accept-Encoding"))
                {
                    var tiles = header.HeaderParameter["Accept-Encoding"].Split(
                        new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    header.FieldAcceptEncoding.AddRange(tiles);
                }
                //Connection
                if (header.HeaderParameter.ContainsKey("Connection"))
                {
                    var text = header.HeaderParameter["Connection"].ToLower();
                    if (text == "keep-alive") header.FieldConnection =
                        HttpConnectionType.KeepAlive;
                }
                //Host
                if (header.HeaderParameter.ContainsKey("Host"))
                {
                    header.Host = header.HeaderParameter["Host"];
                }
                //Cookie
                if (header.HeaderParameter.ContainsKey("Cookie"))
                {
                    header.Cookie.SetRequestCookieString(header.HeaderParameter["Cookie"]);
                }
                //Session
                Session.SessionManager.Register(task);
                //WebServerInfo.Add(InfoType.Debug, GetType(), "Header", "Post-Parsed Header");
            }

            public override bool CanWorkWith(WebProgressTask task)
            {
                return true;
            }
        }
        /// <summary>
        /// WebServiceType.PostParseRequest: Verarbeitet die Aktion HEAD oder OPTIONS, die vom Browser angefordert wurde
        /// </summary>
        public class HttpHeaderSpecialAction : WebService
        {
            /// <summary>
            /// WebServiceType.PostParseRequest: Verarbeitet die Aktion HEAD oder OPTIONS, die vom Browser angefordert wurde
            /// </summary>
            public HttpHeaderSpecialAction() : base(WebServiceType.PostParseRequest) { }

            public override void ProgressTask(WebProgressTask task)
            {
                switch (task.Document.RequestHeader.ProtocolMethod)
                {
                    case HttpProtocollMethods.Head:
                        task.Document.Information["Only Header"] = true;
                        break;
                    case HttpProtocollMethods.Options:
                        {
                            var source = new HttpStringDataSource("GET\r\nPOST\r\nHEAD\r\nOPTIONS\r\nTRACE");
                            source.MimeType = MimeTypes.TextPlain;
                            source.TransferCompleteData = true;
                            task.Document.DataSources.Add(source);
                            task.NextTask = WebServiceType.PreParseRequest;
                        }
                        break;
                }
            }

            public override bool CanWorkWith(WebProgressTask task)
            {
                var method = task.Document.RequestHeader.ProtocolMethod;
                switch (method)
                {
                    case HttpProtocollMethods.Head: return true;
                    case HttpProtocollMethods.Options: return true;
                    //case HttpProtocollMethods.Trace: return true;
                    default: return false;
                }
            }
        }
        /// <summary>
        /// WebServiceType.PostParseRequest: Analysiert die Dokumentabfrage und versucht mit den definierten Regeln 
        /// dieses Dokument auf der Festplatte zu finden.
        /// </summary>
        public class HttpDocumentFinder : WebService
        {
            /// <summary>
            /// WebServiceType.PostParseRequest: Analysiert die Dokumentabfrage und versucht mit den definierten Regeln 
            /// dieses Dokument auf der Festplatte zu finden.
            /// </summary>
            public HttpDocumentFinder() : base(WebServiceType.PostParseRequest) { }

            public class Rule
            {
                public string[] UrlMappedPath { get; private set; }

                public string LocalMappedPath { get; private set; }

                public bool DenyAccess { get; private set; }

                public bool File { get; private set; }

                public Rule(string urlPath, string localPath, bool denyAccess, bool file = true)
                {
                    UrlMappedPath = urlPath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!Directory.Exists(localPath)) Directory.CreateDirectory(localPath);
                    LocalMappedPath = localPath;
                    DenyAccess = denyAccess;
                    File = file;
                }

                public virtual bool SameUrlBasePath(string[] url)
                {
                    if (url.Length == 1 && url[0] == "" && UrlMappedPath.Length == 0) return true;
                    for (int i = 0; i < Math.Min(UrlMappedPath.Length, url.Length); ++i)
                    {
                        if (url[i] != UrlMappedPath[i]) return false;
                    }
                    return url.Length >= UrlMappedPath.Length;
                }
            }

            private List<Rule> rules = new List<Rule>();
            public List<Rule> Rules
            {
                get { return rules; }
            }

            public void Add(Rule rule)
            {
                rules.Add(rule);
            }

            public void Add(string urlPath, string localPath, bool denyAccess, bool file = true)
            {
                Add(new Rule(urlPath, localPath, denyAccess, file));
            }

            public override void ProgressTask(WebProgressTask task)
            {
                var path = task.Document.RequestHeader.Location.DocumentPathTiles;
                var rule = new List<Rule>();
                var level = -1;
                for (int i = 0; i < Rules.Count; ++i)
                {
                    if (!Rules[i].SameUrlBasePath(path)) continue;
                    if (Rules[i].UrlMappedPath.Length < level) continue;
                    if (Rules[i].UrlMappedPath.Length > level)
                    {
                        rule.Clear();
                        level = Rules[i].UrlMappedPath.Length;
                    }
                    if (!Rules[i].DenyAccess) rule.Add(Rules[i]);
                }
                foreach (var r in rule)
                {
                    var url = task.Document.RequestHeader.Location.DocumentPathTiles;
                    var p = r.LocalMappedPath;
                    for (int i = r.UrlMappedPath.Length; i < url.Length; ++i) p += "\\" + url[i];
                    if (r.File)
                    {
                        if (File.Exists(p))
                        {
                            task.Document.Information["HttpDocumentFile"] = p;
                            return;
                        }
                    }
                    else
                    {
                        if (Directory.Exists(p))
                        {
                            task.Document.Information["HttpDocumentFolder"] = p;
                        }
                    }
                }
            }

            public override bool CanWorkWith(WebProgressTask task)
            {
                return true;
            }
        }
        /// <summary>
        /// WebServiceType.PreCreateDocument: Stellt ein festdefiniertes Dokument bereit. Dies ist unabhängig vom 
        /// angeforderten Pfad.
        /// </summary>
        public class StandartDocumentLoader : WebService
        {
            /// <summary>
            /// WebServiceType.PreCreateDocument: Stellt ein festdefiniertes Dokument bereit. Dies ist unabhängig vom 
            /// angeforderten Pfad.
            /// </summary>
            public StandartDocumentLoader()
                : base(WebServiceType.PreCreateDocument)
            {
                Importance = WebProgressImportance.VeryLow;
                Document = "<html><head><meta charset=\"utf-8\" /></head><body>Kein Dokument gefunden.</body></html>";
            }

            public string Document { get; set; }

            public override void ProgressTask(WebProgressTask task)
            {
                var source = new HttpStringDataSource(Document);
                source.MimeType = MimeTypes.TextHtml;
                task.Document.ResponseHeader.StatusCode = HttpStateCode.OK;
                task.Document.DataSources.Add(source);
                task.Document.PrimaryEncoding = "utf-8";
                //WebServerInfo.Add(InfoType.Debug, GetType(), "Document", "Loaded Document");
            }

            public override bool CanWorkWith(WebProgressTask task)
            {
                return true;
            }
        }
        /// <summary>
        /// WebServiceType.PreCreateDocument: Lädt das Dokument, welches vorher von <see cref="HttpDocumentFinder"/> 
        /// gefunden wurde.
        /// </summary>
        public class HttpDirectoryMapper : WebService
        {
            public bool MapFolderToo { get; set; }
            /// <summary>
            /// WebServiceType.PreCreateDocument: Lädt das Dokument, welches vorher von <see cref="HttpDocumentFinder"/> 
            /// gefunden wurde.
            /// </summary>
            public HttpDirectoryMapper(bool mapFolderToo)
                : base(WebServiceType.PreCreateDocument)
            {
                MapFolderToo = mapFolderToo;
            }

            protected string GetMime(string extension, WebProgressTask task)
            {
                switch (extension.ToLower())
                {
                    case ".html": return MimeTypes.TextHtml;
                    case ".htm": return MimeTypes.TextHtml;
                    case ".js": return MimeTypes.TextJs;
                    case ".css": return MimeTypes.TextCss;
                    case ".jpg": return MimeTypes.ImageJpeg;
                    case ".jpeg": return MimeTypes.ImageJpeg;
                    case ".png": return MimeTypes.ImagePng;
                    case ".pneg": return MimeTypes.ImagePng;
                    case ".gif": return MimeTypes.ImageGif;
                    case ".ico": return MimeTypes.ImageIcon;
                    case ".txt": return MimeTypes.TextPlain;
                    case ".xml": return MimeTypes.TextXml;
                    case ".rtf": return MimeTypes.ApplicationRtf;
                    default:
                        var set = task.Server.Settings;
                        if (set.DefaultFileMimeAssociation.ContainsKey(extension.ToLower()))
                            return set.DefaultFileMimeAssociation[extension.ToLower()];
                        return MimeTypes.TextPlain;
                }
            }

            public override void ProgressTask(WebProgressTask task)
            {
                if (task.Document.Information.ContainsKey("HttpDocumentFile"))
                {
                    var path = task.Document.Information["HttpDocumentFile"].ToString();
                    var source = new HttpFileDataSource(path);
                    source.TransferCompleteData = true;
                    source.MimeType = GetMime(Path.GetExtension(path), task);
                    task.Document.DataSources.Add(source);
                    task.Document.ResponseHeader.StatusCode = HttpStateCode.OK;
                }
                if (MapFolderToo && task.Document.Information.ContainsKey("HttpDocumentFolder"))
                {
                    var path = task.Document.Information["HttpDocumentFolder"].ToString();
                    var url = task.Document.RequestHeader.Location.DocumentPath.TrimEnd('/');
                    var d = new DirectoryInfo(path);
                    var html = "<html lang=\"de\"><head><title>" + d.Name + "</title></head><body>";
                    html += "<h1>" + path + "</h1><a href=\"../\">Eine Ebene h&ouml;her</a><ul>";
                    foreach (var di in d.GetDirectories())
                        html += "<li>DIRECTORY: <a href=\"" + url + "/" + WebServerHelper.EncodeUri(di.Name) + "\">" + di.Name + "</a></li>";
                    foreach (var fi in d.GetFiles())
                        html += "<li>FILE: <a href=\"" + url + "/" + WebServerHelper.EncodeUri(fi.Name) + "\">" +
                            fi.Name + "</a> [" + WebServerHelper.GetVolumeString(fi.Length, true, 4) + "]</li>";
                    html += "</ul>Ende der Ausgabe.</body></html>";
                    var source = new HttpStringDataSource(html);
                    source.TransferCompleteData = true;
                    source.MimeType = MimeTypes.TextHtml;
                    task.Document.DataSources.Add(source);
                    task.Document.ResponseHeader.StatusCode = HttpStateCode.OK;
                }
            }

            public override bool CanWorkWith(WebProgressTask task)
            {
                if (MapFolderToo && task.Document.Information.ContainsKey("HttpDocumentFolder")) return true;
                return task.Document.Information.ContainsKey("HttpDocumentFile");
            }
        }
        /// <summary>
        /// WebServiceType.PreCreateResponse: Erstellt den Response-Header und füllt diesen mit den wichtigsten Daten.
        /// </summary>
        public class HttpResponseCreator : WebService
        {
            /// <summary>
            /// WebServiceType.PreCreateResponse: Erstellt den Response-Header und füllt diesen mit den wichtigsten Daten.
            /// </summary>
            public HttpResponseCreator() : base(WebServiceType.PreCreateResponse) { }

            public override void ProgressTask(WebProgressTask task)
            {
                var request = task.Document.RequestHeader;
                var response = task.Document.ResponseHeader;
                response.FieldContentType = task.Document.PrimaryMime;
                response.SetActualDate();
                response.HttpProtocol = request.HttpProtocol;
                response.HeaderParameter["Connection"] = "keep-alive";
                response.HeaderParameter["X-UA-Compatible"] = "IE=Edge";
                response.HeaderParameter["Content-Length"] =
                    task.Document.DataSources.Sum((s) => s.AproximateLength()).ToString();
                if (task.Document.PrimaryEncoding != null)
                    response.HeaderParameter["Content-Type"] += "; charset=" +
                        task.Document.PrimaryEncoding;
                //WebServerInfo.Add(InfoType.Debug, GetType(), "Response", "Created Response");
            }

            public override bool CanWorkWith(WebProgressTask task)
            {
                return true;
            }
        }
        /// <summary>
        /// WebServiceType.SendResponse: Sendet Response und Dokument, wenn vorhanden, an den Clienten.
        /// </summary>
        public class HttpSender : WebService
        {
            /// <summary>
            /// WebServiceType.SendResponse: Sendet Response und Dokument, wenn vorhanden, an den Clienten.
            /// </summary>
            public HttpSender() : base(WebServiceType.SendResponse) { }

            public virtual string StatusCodeText(HttpStateCode code)
            {
                switch ((int)code)
                {
                    case 100: return "Continue";
                    case 101: return "Switching Protcols";
                    case 102: return "Processing";
                    case 200: return "OK";
                    case 201: return "Created";
                    case 202: return "Accepted";
                    case 203: return "Non-Authoritative Information";
                    case 204: return "No Content";
                    case 205: return "Reset Content";
                    case 206: return "Partial Content";
                    case 207: return "Multi-Status";
                    case 208: return "IM Used";
                    case 300: return "Multiple Choises";
                    case 301: return "Moved Permanently";
                    case 302: return "Found";
                    case 303: return "See Other";
                    case 304: return "Not Modified";
                    case 305: return "Use Proxy";
                    case 307: return "Temporary Redirect";
                    case 308: return "Permanent Redirect";
                    case 400: return "Bad Request";
                    case 401: return "Unathorized";
                    case 402: return "Payment Required";
                    case 403: return "Forbidden";
                    case 404: return "Not Found";
                    case 405: return "Method Not Allowed";
                    case 406: return "Not Acceptable";
                    case 407: return "Proxy Authendtication Required";
                    case 408: return "Request Time-out";
                    case 409: return "Conflict";
                    case 410: return "Gone";
                    case 411: return "Length Required";
                    case 412: return "Precondition Failed";
                    case 413: return "Request Entity Too Large";
                    case 414: return "Request-URL Too Long";
                    case 415: return "Unsupported Media Type";
                    case 416: return "Requested range not satisfiable";
                    case 417: return "Expectation Failed";
                    case 418: return "I'm a teapot";
                    case 420: return "Policy Not Fulfilled";
                    case 421: return "There are too many connections from your internet address";
                    case 422: return "Unprocessable Entity";
                    case 423: return "Locked";
                    case 424: return "Failed Dependency";
                    case 425: return "Unordered Collection";
                    case 426: return "Upgrade Required";
                    case 428: return "Precondition Required";
                    case 429: return "Too Many Requests";
                    case 431: return "Request Header Fields Too Large";
                    case 500: return "Internal Server Error";
                    case 501: return "Not Implemented";
                    case 502: return "Bad Gateway";
                    case 503: return "Service Unavailable";
                    case 504: return "Gateway Time-out";
                    case 505: return "HTTP Version not supported";
                    case 506: return "Variant Also Negotiates";
                    case 507: return "Insufficient Storage";
                    case 508: return "Loop Detected";
                    case 509: return "Bandwidth Limit Exceeded";
                    case 510: return "Not Extended";
                    default:
                        WebServerInfo.Add(InfoType.Information, GetType(), "StatusCode",
                            "Cant get status string from {0} ({1}).", code, (int)code);
                        return "";
                }
            }

            public override void ProgressTask(WebProgressTask task)
            {
                //WebServerInfo.Add(InfoType.Debug, GetType(), "Sender", "Start Send Message");
                var header = task.Document.ResponseHeader;
                var stream = task.NetworkStream;
                var writer = new StreamWriter(stream);
                writer.Write(header.HttpProtocol);
                writer.Write(" ");
                writer.Write((int)header.StatusCode);
                writer.Write(" ");
                writer.WriteLine(StatusCodeText(header.StatusCode));
                for (int i = 0; i < header.HeaderParameter.Count; ++i) //Parameter
                {
                    var e = header.HeaderParameter.ElementAt(i);
                    writer.Write(e.Key);
                    writer.Write(": ");
                    writer.WriteLine(e.Value);
                }
                foreach (var cookie in task.Document.RequestHeader.Cookie.AddedCookies) //Cookies
                {
                    writer.Write("Set-Cookie: ");
                    writer.WriteLine(cookie.ToString());
                }
                writer.WriteLine();
                try { writer.Flush(); }
                catch (ObjectDisposedException)
                {
                    WebServerInfo.Add(InfoType.Error, GetType(), "Send", "Connection closed by remote host.");
                    return;
                }
                catch (IOException)
                {
                    WebServerInfo.Add(InfoType.Error, GetType(), "Send", "Connection closed by remote host.");
                    return;
                }
                //Daten senden
                if (!(task.Document.Information.ContainsKey("Only Header") && (bool)task.Document.Information["Only Header"]))
                    for (int i = 0; i < task.Document.DataSources.Count; ++i)
                    {
                        task.Document.DataSources[i].WriteToStream(stream);
                    }
                try { stream.Flush(); }
                catch (IOException)
                {
                    WebServerInfo.Add(InfoType.Error, GetType(), "Send", "Connection closed by remote host.");
                    return;
                }
                //WebServerInfo.Add(InfoType.Debug, GetType(), "Sender", "Finish Send Message");
            }

            public override bool CanWorkWith(WebProgressTask task)
            {
                return true;
            }
        }
    }

    #endregion

    #region Information Definition

    public enum InfoType
    {
        Information,
        Debug,
        Error,
        FatalError
    }

    public class InfoTile
    {
        public InfoType Type { get; private set; }

        public Type Sender { get; private set; }

        public string InfoType { get; private set; }

        public string Information { get; private set; }

        public object AdditionalData { get; private set; }

        public InfoTile(InfoType type, Type sender, string infoType, string information)
        {
            Type = type;
            Sender = sender;
            InfoType = infoType;
            Information = information;
        }

        public InfoTile(InfoType type, Type sender, string infoType, object additionlData, string information)
            : this(type, sender, infoType, information)
        {
            AdditionalData = additionlData;
        }

        public InfoTile(InfoType type, Type sender, string infoType, string mask, params object[] data)
            : this(type, sender, infoType, string.Format(mask, data))
        { }

        public InfoTile(InfoType type, Type sender, string infoType, object additionlData, string mask, params object[] data)
            : this(type, sender, infoType, additionlData, string.Format(mask, data))
        { }

        public override string ToString()
        {
            return string.Format("[{0}] {1} : {2} -> {3}", Type, Sender.FullName, InfoType, Information);
        }
    }

    public delegate void InformationReceivedHandler(InfoTile tile);

    public static class WebServerInfo
    {
        private static List<InfoTile> information = new List<InfoTile>();
        public static List<InfoTile> Information
        {
            get { return WebServerInfo.information; }
        }

        private static List<Type> ignoreSenderEvents = new List<Type>();
        public static List<Type> IgnoreSenderEvents
        {
            get { return WebServerInfo.ignoreSenderEvents; }
        }

        public static event InformationReceivedHandler InformationReceived;

        static object lockObjekt = new object();
        public static void Add(InfoTile tile)
        {
            lock (lockObjekt) { Information.Add(tile); }
            if (InformationReceived != null)
            {
                if (IgnoreSenderEvents.Exists((type) => type.AssemblyQualifiedName == tile.Sender.AssemblyQualifiedName)) return;
                //new Task(() => InformationReceived(tile)).Start();
                InformationReceived(tile);
            }
        }

        public static void Add(InfoType type, Type sender, string infoType, string information)
        {
            Add(new InfoTile(type, sender, infoType, information));
        }

        public static void Add(InfoType type, Type sender, string infoType, object additionlData, string information)
        {
            Add(new InfoTile(type, sender, infoType, additionlData, information));
        }

        public static void Add(InfoType type, Type sender, string infoType, string mask, params object[] data)
        {
            Add(new InfoTile(type, sender, infoType, mask: mask, data: data));
        }

        public static void Add(InfoType type, Type sender, string infoType, object additionlData, string mask, params object[] data)
        {
            Add(new InfoTile(type, sender, infoType, additionlData, mask, data));
        }

        public static void Clear()
        {
            Information.Clear();
        }
    }

    #endregion

    #region Header Definition

    public abstract class HttpHeader
    {
        private string httpProtocol = HttpProtocollDefinitions.HttpVersion1_1;
        public string HttpProtocol
        {
            get { return httpProtocol; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("HttpProtocol cannot contain an empty Protocol", "HttpProtocol");
                httpProtocol = value;
            }
        }

        private Dictionary<string, string> headerParameter = new Dictionary<string, string>();
        public Dictionary<string, string> HeaderParameter
        {
            get { return headerParameter; }
        }

        private string protocolMethod = HttpProtocollMethods.Get;
        public string ProtocolMethod
        {
            get { return protocolMethod; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("ProtocolMethod cannot be empty", "ProtocolMethod");
                protocolMethod = value;
            }
        }
    }

    public class HttpRequestHeader : HttpHeader
    {
        private string url = "/";
        public string Url
        {
            get { return url; }
            set
            {
                if (url == null) throw new ArgumentNullException("Url");
                url = value ?? "/";
                Location.SetLocation(url);
            }
        }

        private HttpLocation location = new HttpLocation("/");
        public HttpLocation Location
        {
            get { return location; }
        }

        private string host = "";
        public string Host
        {
            get { return host; }
            set
            {
                host = value ?? throw new ArgumentNullException("Host");
            }
        }

        private HttpPost post = new HttpPost("");
        public HttpPost Post
        {
            get { return post; }
        }

        private List<string> fieldAccept = new List<string>();
        public List<string> FieldAccept
        {
            get { return fieldAccept; }
        }

        private List<string> fieldAcceptCharset = new List<string>();
        public List<string> FieldAcceptCharset
        {
            get { return fieldAcceptCharset; }
        }

        private List<string> fieldAcceptEncoding = new List<string>();
        public List<string> FieldAcceptEncoding
        {
            get { return fieldAcceptEncoding; }
        }

        private HttpConnectionType fieldConnection = HttpConnectionType.Close;
        public HttpConnectionType FieldConnection
        {
            get { return fieldConnection; }
            set { fieldConnection = value; }
        }

        private HttpCookie cookie = new HttpCookie("");
        public HttpCookie Cookie
        {
            get { return cookie; }
        }

        public string FieldUserAgent
        {
            get { return HeaderParameter["User-Agent"]; }
            set { HeaderParameter["User-Agent"] = value; }
        }
    }

    public class HttpResponseHeader : HttpHeader
    {
        private HttpStateCode statusCode = HttpStateCode.OK;
        public HttpStateCode StatusCode
        {
            get { return statusCode; }
            set { statusCode = value; }
        }

        public string FieldLocation
        {
            get { return HeaderParameter["Location"]; }
            set { HeaderParameter["Location"] = value; }
        }

        public string FieldDate
        {
            get { return HeaderParameter["Date"]; }
            set { HeaderParameter["Date"] = value; }
        }

        public string FieldLastModified
        {
            get { return HeaderParameter["Last-Modified"]; }
            set { HeaderParameter["Last-Modified"] = value; }
        }

        public string FieldContentType
        {
            get { return HeaderParameter["Content-Type"]; }
            set { HeaderParameter["Content-Type"] = value; }
        }

        public virtual void SetActualDate()
        {
            FieldDate = WebServerHelper.GetDateString(DateTime.UtcNow);
        }
    }

    public class HttpLocation
    {
        public string Url { get; private set; }

        public string DocumentPath { get; private set; }

        public string[] DocumentPathTiles { get; private set; }

        public string CompleteGet { get; private set; }

        public Dictionary<string, string> GetParameter { get; private set; }

        public virtual void SetLocation(string url)
        {
            Url = url ?? throw new ArgumentNullException("url");
            var ind = url.IndexOf('?');
            if (ind == -1)
            {
                DocumentPath = url;
                CompleteGet = "";
            }
            else
            {
                DocumentPath = url.Remove(ind);
                CompleteGet = ind + 1 == url.Length ? "" : url.Substring(ind + 1);
            }
            var path = DocumentPath.Trim('/');
            DocumentPathTiles = path.Split('/');
            for (int i = 0; i < DocumentPathTiles.Length; ++i) DocumentPathTiles[i] = WebServerHelper.DecodeUri(DocumentPathTiles[i]);
            GetParameter.Clear();
            if (CompleteGet != "")
            {
                var tiles = CompleteGet.Split('&');
                foreach (var tile in tiles)
                {
                    ind = tile.IndexOf('=');
                    if (ind == -1)
                    {
                        var key = WebServerHelper.DecodeUri(tile);
                        if (!GetParameter.ContainsKey(key)) GetParameter.Add(key, "");
                    }
                    else
                    {
                        var key = WebServerHelper.DecodeUri(tile.Remove(ind));
                        var value = ind + 1 == tile.Length ? "" : tile.Substring(ind+1);
                        if (!GetParameter.ContainsKey(key)) GetParameter.Add(key, WebServerHelper.DecodeUri(value));
                    }
                }
            }
        }

        public HttpLocation(string url)
        {
            if (url == null) throw new ArgumentNullException("url");
            GetParameter = new Dictionary<string, string>();
            SetLocation(url);
        }

        public override string ToString()
        {
            return Url;
        }

        public bool IsUrl(string[] urlTiles, bool ignoreCase = false)
        {
            if (urlTiles.Length != DocumentPathTiles.Length) return false;
            for (int i = 0; i < urlTiles.Length; ++i)
                if (ignoreCase)
                {
                    if (urlTiles[i].ToLower() != DocumentPathTiles[i].ToLower()) return false;
                }
                else
                {
                    if (urlTiles[i] != DocumentPathTiles[i]) return false;
                }
            return true;
        }

        public bool StartsUrlWith(string[] urlTiles, bool ignoreCase = false)
        {
            if (urlTiles.Length > DocumentPathTiles.Length) return false;
            for (int i = 0; i<urlTiles.Length; ++i)
                if (ignoreCase)
                {
                    if (urlTiles[i].ToLower() != DocumentPathTiles[i].ToLower()) return false;
                }
            else
                {
                    if (urlTiles[i] != DocumentPathTiles[i]) return false;
                }
            return true;
        }
    }

    public class HttpCookie
    {
        public class Cookie
        {
            public string Name { get; private set; }
            public string Value { get; private set; }
            public DateTime Expires { get; private set; }
            public int MaxAge { get; private set; }
            public string Path { get; private set; }

            public Cookie(string name, string value)
            {
                Name = name ?? throw new ArgumentNullException("name");
                Value = value ?? throw new ArgumentNullException("value");
                Expires = new DateTime(9999, 12, 31);
                MaxAge = -1;
                Path = "";
            }

            public Cookie(string name, string value, DateTime expires)
            {
                Name = name ?? throw new ArgumentNullException("name");
                Value = value ?? throw new ArgumentNullException("value");
                Expires = expires;
                MaxAge = -1;
                Path = "";
            }

            public Cookie(string name, string value, int maxAge)
            {
                Name = name ?? throw new ArgumentNullException("name");
                Value = value ?? throw new ArgumentNullException("value");
                Expires = new DateTime(9999, 12, 31);
                MaxAge = maxAge;
                Path = "";
            }

            public Cookie(string name, string value, string path)
            {
                Name = name ?? throw new ArgumentNullException("name");
                Value = value ?? throw new ArgumentNullException("value");
                Expires = new DateTime(9999, 12, 31);
                MaxAge = -1;
                Path = path ?? throw new ArgumentNullException("path");
            }

            public Cookie(string name, string value, DateTime expires, int maxAge, string path)
            {
                Name = name ?? throw new ArgumentNullException("name");
                Value = value ?? throw new ArgumentNullException("value");
                Expires = expires;
                MaxAge = maxAge;
                Path = path ?? throw new ArgumentNullException("path");
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append(WebServerHelper.EncodeUri(Name));
                sb.Append('=');
                sb.Append(WebServerHelper.EncodeUri(Value));
                if (Expires != new DateTime(9999, 12, 31))
                {
                    sb.Append(";expires=");
                    sb.Append(WebServerHelper.GetDateString(Expires));
                }
                if (MaxAge != -1)
                {
                    sb.Append(";Max-Age=");
                    sb.Append(MaxAge);
                }
                sb.Append(";Path=");
                sb.Append(Path);
                return sb.ToString();
            }
        }

        public string CompleteRequestCookie { get; private set; }

        public List<Cookie> AddedCookies { get; private set; }

        public Cookie[] RequestedCookies { get; private set; }

        public HttpCookie(string cookie)
        {
            if (cookie == null) throw new ArgumentNullException("Cookie");
            AddedCookies = new List<Cookie>();
            RequestedCookies = new Cookie[0];
            SetRequestCookieString(cookie);
        }

        public Cookie Get(string name)
        {
            var cookie = AddedCookies.Find((c) => c.Name == name);
            if (cookie != null) return cookie;
            return RequestedCookies.ToList().Find((c) => c.Name == name);
        }

        public virtual void SetRequestCookieString(string cookie)
        {
            CompleteRequestCookie = cookie ?? throw new ArgumentNullException("Cookie");
            AddedCookies.Clear();
            var l = new List<Cookie>();
            var rck = new List<string>();
            if (CompleteRequestCookie != "")
            {
                var tiles = CompleteRequestCookie.Split('&',';');
                foreach (var tile in tiles)
                {
                    var ind = tile.IndexOf('=');
                    if (ind == -1)
                    {
                        var key = WebServerHelper.DecodeUri(tile.Trim());
                        if (!rck.Contains(key))
                        {
                            rck.Add(key);
                            l.Add(new Cookie(key, ""));
                        }
                    }
                    else
                    {
                        var key = WebServerHelper.DecodeUri(tile.Remove(ind).Trim());
                        var value = ind + 1 == tile.Length ? "" : tile.Substring(ind + 1);
                        if (!rck.Contains(key))
                        {
                            rck.Add(key);
                            l.Add(new Cookie(key, value));
                        }
                    }
                }
            }
            RequestedCookies = l.ToArray();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(CompleteRequestCookie);
            return sb.ToString();
        }
    }

    public class HttpPost
    {
        public string CompletePost { get; private set; }

        public Dictionary<string, string> PostParameter { get; private set; }

        public virtual void SetPost(string post)
        {
            CompletePost = post ?? throw new ArgumentNullException("Post");
            PostParameter.Clear();
            if (CompletePost != "")
            {
                var tiles = CompletePost.Split('&');
                foreach (var tile in tiles)
                {
                    var ind = tile.IndexOf('=');
                    if (ind == -1)
                    {
                        var t = WebServerHelper.DecodeUri(tile);
                        if (!PostParameter.ContainsKey(t)) PostParameter.Add(t, "");
                    }
                    else
                    {
                        var key = WebServerHelper.DecodeUri(tile.Remove(ind));
                        var value = ind + 1 == tile.Length ? "" : tile.Substring(ind + 1);
                        if (!PostParameter.ContainsKey(key)) PostParameter.Add(key, WebServerHelper.DecodeUri(value));
                    }
                }
            }
        }

        public HttpPost(string post)
        {
            if (post == null) throw new ArgumentNullException("Post");
            PostParameter = new Dictionary<string, string>();
            SetPost(post);
        }

        public override string ToString()
        {
            return CompletePost;
        }
    }

    public enum HttpConnectionType
    {
        Close,
        KeepAlive
    }

    public enum HttpStateCode
    {
        /// <summary>
        /// 100 - Die laufende Anfrage an den Server wurde noch nicht zurückgewiesen. 
        /// (Wird im Zusammenhang mit dem „Expect 100-continue“-Header-Feld verwendet.) 
        /// Der Client kann nun mit der potentiell sehr großen Anfrage fortfahren.
        /// </summary>
        Continue = 100,
        /// <summary>
        /// 101 - Wird verwendet, wenn der Server eine Anfrage mit gesetztem 
        /// „Upgrade“-Header-Feld empfangen hat und mit dem Wechsel zu einem anderen 
        /// Protokoll einverstanden ist. Anwendung findet dieser Status-Code beispielsweise 
        /// im Wechsel von HTTP zu WebSocket.
        /// </summary>
        SwitchingProtocols = 101,
        /// <summary>
        /// 102 - Wird verwendet, um ein Timeout zu vermeiden, während der Server eine 
        /// zeitintensive Anfrage bearbeitet.
        /// </summary>
        Processing = 102,
        /// <summary>
        /// 200 - Die Anfrage wurde erfolgreich bearbeitet und das Ergebnis der Anfrage 
        /// wird in der Antwort übertragen.
        /// </summary>
        OK = 200,
        /// <summary>
        /// 201 - Die Anfrage wurde erfolgreich bearbeitet. Die angeforderte Ressource 
        /// wurde vor dem Senden der Antwort erstellt. Das „Location“-Header-Feld enthält 
        /// eventuell die Adresse der erstellten Ressource.
        /// </summary>
        Created = 201,
        /// <summary>
        /// 202 - Die Anfrage wurde akzeptiert, wird aber zu einem späteren Zeitpunkt 
        /// ausgeführt. Das Gelingen der Anfrage kann nicht garantiert werden.
        /// </summary>
        Accepted = 202,
        /// <summary>
        /// 203 - Die Anfrage wurde bearbeitet, das Ergebnis ist aber nicht unbedingt 
        /// vollständig und aktuell.
        /// </summary>
        NonAuthoritativeInformation = 203,
        /// <summary>
        /// 204 - Die Anfrage wurde erfolgreich durchgeführt, die Antwort enthält 
        /// jedoch bewusst keine Daten.
        /// </summary>
        NoContent = 204,
        /// <summary>
        /// 205 - Die Anfrage wurde erfolgreich durchgeführt; der Client soll das 
        /// Dokument neu aufbauen und Formulareingaben zurücksetzen.
        /// </summary>
        ResetContent = 205,
        /// <summary>
        /// 206 - Der angeforderte Teil wurde erfolgreich übertragen (wird im 
        /// Zusammenhang mit einem „Content-Range“-Header-Feld oder dem Content-Type 
        /// multipart/byteranges verwendet). Kann einen Client über Teil-Downloads 
        /// informieren (wird zum Beispiel von Wget genutzt, um den Downloadfortschritt 
        /// zu überwachen oder einen Download in mehrere Streams aufzuteilen).
        /// </summary>
        PartialContent = 206,
        /// <summary>
        /// 207 - Die Antwort enthält ein XML-Dokument, das mehrere Statuscodes zu 
        /// unabhängig voneinander durchgeführten Operationen enthält.
        /// </summary>
        MultiStatus = 207,
        /// <summary>
        /// 208 - WebDAV RFC 5842 – Die Mitglieder einer WebDAV-Bindung wurden bereits 
        /// zuvor aufgezählt und sind in dieser Anfrage nicht mehr vorhanden.
        /// </summary>
        AlreadyReported = 208,
        /// <summary>
        /// 226 - RFC 3229 – Der Server hat eine GET-Anforderung für die Ressource 
        /// erfüllt, die Antwort ist eine Darstellung des Ergebnisses von einem oder 
        /// mehreren Instanz-Manipulationen, bezogen auf die aktuelle Instanz.
        /// </summary>
        IMUsed = 226,
        /// <summary>
        /// 300 - Die angeforderte Ressource steht in verschiedenen Arten zur Verfügung. 
        /// Die Antwort enthält eine Liste der verfügbaren Arten. Das „Location“-Header-Feld 
        /// enthält eventuell die Adresse der vom Server bevorzugten Repräsentation.
        /// </summary>
        MultipleChoises = 300,
        /// <summary>
        /// 301 - Die angeforderte Ressource steht ab sofort unter der im 
        /// „Location“-Header-Feld angegebenen Adresse bereit (auch Redirect genannt). 
        /// Die alte Adresse ist nicht länger gültig.
        /// </summary>
        MovedPermanently = 301,
        /// <summary>
        /// 302 - Die angeforderte Ressource steht vorübergehend unter der im 
        /// „Location“-Header-Feld angegebenen Adresse bereit. Die alte Adresse 
        /// bleibt gültig. Die Browser folgen meist mit einem GET, auch wenn der 
        /// ursprüngliche Request ein POST war. Wird in HTTP/1.1 je nach Anwendungsfall 
        /// durch die Statuscodes 303 bzw. 307 ersetzt. 
        /// </summary>
        Found = 302,
        /// <summary>
        /// 303 - Die Antwort auf die durchgeführte Anfrage lässt sich unter der im 
        /// „Location“-Header-Feld angegebenen Adresse beziehen. Der Browser soll mit 
        /// einem GET folgen, auch wenn der ursprüngliche Request ein POST war.
        /// </summary>
        SeeOther = 303,
        /// <summary>
        /// 304 - Der Inhalt der angeforderten Ressource hat sich seit der letzten 
        /// Abfrage des Clients nicht verändert und wird deshalb nicht übertragen. 
        /// Zu den Einzelheiten siehe Browser-Cache-Versionsvergleich.
        /// </summary>
        NotModified = 304,
        /// <summary>
        /// 305 - Die angeforderte Ressource ist nur über einen Proxy erreichbar. 
        /// Das „Location“-Header-Feld enthält die Adresse des Proxy.
        /// </summary>
        UseProxy = 305,
        //306 ist reserviert und wird nicht mehr verwendet.
        /// <summary>
        /// 307 - Die angeforderte Ressource steht vorübergehend unter der im 
        /// „Location“-Header-Feld angegebenen Adresse bereit. Die alte Adresse 
        /// bleibt gültig. Der Browser soll mit derselben Methode folgen wie beim 
        /// ursprünglichen Request (d. h. einem POST folgt ein POST). Dies ist der 
        /// wesentliche Unterschied zu 302/303.
        /// </summary>
        TemporaryRedirect = 307,
        /// <summary>
        /// 308 - Experimentell eingeführt via RFC; die angeforderte Ressource steht ab 
        /// sofort unter der im „Location“-Header-Feld angegebenen Adresse bereit, die 
        /// alte Adresse ist nicht länger gültig. Der Browser soll mit derselben Methode 
        /// folgen wie beim ursprünglichen Request (d. h. einem POST folgt ein POST). Dies 
        /// ist der wesentliche Unterschied zu 302/303.
        /// </summary>
        PermanentRedirect = 308,
        /// <summary>
        /// 400 - Die Anfrage-Nachricht war fehlerhaft aufgebaut.
        /// </summary>
        BadRequest = 400,
        /// <summary>
        /// 401 - Die Anfrage kann nicht ohne gültige Authentifizierung durchgeführt 
        /// werden. Wie die Authentifizierung durchgeführt werden soll, wird im 
        /// „WWW-Authenticate“-Header-Feld der Antwort übermittelt.
        /// </summary>
        Unauthorized = 401,
        /// <summary>
        /// 402 - Übersetzt: Bezahlung benötigt. Dieser Status ist für zukünftige 
        /// HTTP-Protokolle reserviert.
        /// </summary>
        PaymentRequired = 402,
        /// <summary>
        /// 403 - Die Anfrage wurde mangels Berechtigung des Clients nicht durchgeführt. 
        /// Diese Entscheidung wurde – anders als im Fall des Statuscodes 401 – unabhängig 
        /// von Authentifizierungsinformationen getroffen, auch etwa wenn eine als HTTPS 
        /// konfigurierte URL nur mit HTTP aufgerufen wurde.
        /// </summary>
        Forbidden = 403,
        /// <summary>
        /// 404 - Die angeforderte Ressource wurde nicht gefunden. Dieser Statuscode 
        /// kann ebenfalls verwendet werden, um eine Anfrage ohne näheren Grund abzuweisen. 
        /// Links, welche auf solche Fehlerseiten verweisen, werden auch als Tote Links 
        /// bezeichnet.
        /// </summary>
        NotFound = 404,
        /// <summary>
        /// 405 - Die Anfrage darf nur mit anderen HTTP-Methoden (zum Beispiel GET statt 
        /// POST) gestellt werden. Gültige Methoden für die betreffende Ressource werden 
        /// im „Allow“-Header-Feld der Antwort übermittelt.
        /// </summary>
        MethodNotAllowed = 405,
        /// <summary>
        /// 406 - Die angeforderte Ressource steht nicht in der gewünschten Form zur 
        /// Verfügung. Gültige „Content-Type“-Werte können in der Antwort übermittelt werden.
        /// </summary>
        NotAcceptable = 406,
        /// <summary>
        /// 407 - Analog zum Statuscode 401 ist hier zunächst eine Authentifizierung des 
        /// Clients gegenüber dem verwendeten Proxy erforderlich. Wie die Authentifizierung 
        /// durchgeführt werden soll, wird im „Proxy-Authenticate“-Header-Feld der Antwort 
        /// übermittelt.
        /// </summary>
        ProxyAuthenticationRequired = 407,
        /// <summary>
        /// 408 - Innerhalb der vom Server erlaubten Zeitspanne wurde keine vollständige 
        /// Anfrage des Clients empfangen.
        /// </summary>
        RequestTimeOut = 408,
        /// <summary>
        /// 409 - Die Anfrage wurde unter falschen Annahmen gestellt. Im Falle einer 
        /// PUT-Anfrage kann dies zum Beispiel auf eine zwischenzeitliche Veränderung 
        /// der Ressource durch Dritte zurückgehen.
        /// </summary>
        Conflict = 409,
        /// <summary>
        /// 410 - Die angeforderte Ressource wird nicht länger bereitgestellt und wurde 
        /// dauerhaft entfernt.
        /// </summary>
        Gone = 410,
        /// <summary>
        /// 411 - Die Anfrage kann ohne ein „Content-Length“-Header-Feld nicht bearbeitet 
        /// werden.
        /// </summary>
        LengthRequired = 411,
        /// <summary>
        /// 412 - Eine in der Anfrage übertragene Voraussetzung, zum Beispiel in Form 
        /// eines „If-Match“-Header-Felds, traf nicht zu.
        /// </summary>
        PreconditionFailed = 412,
        /// <summary>
        /// 413 - Die gestellte Anfrage war zu groß, um vom Server bearbeitet werden zu 
        /// können. Ein „Retry-After“-Header-Feld in der Antwort kann den Client darauf 
        /// hinweisen, dass die Anfrage eventuell zu einem späteren Zeitpunkt bearbeitet 
        /// werden könnte.
        /// </summary>
        RequestEntityTooLarge = 413,
        /// <summary>
        /// 414 - Die URL der Anfrage war zu lang. Ursache ist oft eine Endlosschleife 
        /// aus Redirects.
        /// </summary>
        RequestUrlTooLong = 414,
        /// <summary>
        /// 415 - Der Inhalt der Anfrage wurde mit ungültigem oder nicht erlaubtem 
        /// Medientyp übermittelt.
        /// </summary>
        UnsupportedMediaType = 415,
        /// <summary>
        /// 416 - Der angeforderte Teil einer Ressource war ungültig oder steht auf 
        /// dem Server nicht zur Verfügung.
        /// </summary>
        RequestedRangeNotSatisfiable = 416,
        /// <summary>
        /// 417 - Verwendet im Zusammenhang mit einem „Expect“-Header-Feld. Das im 
        /// „Expect“-Header-Feld geforderte Verhalten des Servers kann nicht erfüllt 
        /// werden.
        /// </summary>
        ExpectationFailed = 417,
        /// <summary>
        /// 418 - Dieser Code ist als Aprilscherz der IETF zu verstehen, welcher 
        /// näher unter RFC 2324, Hyper Text Coffee Pot Control Protocol, beschrieben 
        /// ist. Innerhalb eines scherzhaften Protokolls zum Kaffeekochen zeigt er an, 
        /// dass fälschlicherweise eine Teekanne anstatt einer Kaffeekanne verwendet wurde. 
        /// Dieser Statuscode ist allerdings kein Bestandteil von HTTP, sondern lediglich 
        /// von HTCPCP (Hyper Text Coffee Pot Control Protocol). Trotzdem ist dieser 
        /// Scherz-Statuscode auf einigen Webseiten zu finden, real wird aber der 
        /// Statuscode 200 gesendet.
        /// </summary>
        ImATeapot = 418,
        /// <summary>
        /// 420 - In W3C PEP (Working Draft 21. November 1997) wird dieser Code 
        /// vorgeschlagen, um mitzuteilen, dass eine Bedingung nicht erfüllt wurde.
        /// </summary>
        PolicyNotFulfilled = 420,
        /// <summary>
        /// 421 - Verwendet, wenn die Verbindungshöchstzahl überschritten wird. 
        /// Ursprünglich wurde dieser Code in W3C PEP (Working Draft 21. November 
        /// 1997) vorgeschlagen, um auf den Fehler „Bad Mapping“ hinzuweisen.
        /// </summary>
        ThereAreTooManyConnectionsFromYourInternetAddress = 421,
        /// <summary>
        /// 422 - Verwendet, wenn weder die Rückgabe von Statuscode 415 noch 400 
        /// gerechtfertigt wäre, eine Verarbeitung der Anfrage jedoch zum Beispiel 
        /// wegen semantischer Fehler abgelehnt wird.
        /// </summary>
        UnprocessableEntity = 422,
        /// <summary>
        /// 423 - Die angeforderte Ressource ist zurzeit gesperrt.
        /// </summary>
        Locked = 423,
        /// <summary>
        /// 424 - Die Anfrage konnte nicht durchgeführt werden, weil sie das 
        /// Gelingen einer vorherigen Anfrage voraussetzt.
        /// </summary>
        FailedDependency = 424,
        /// <summary>
        /// 425 - In den Entwürfen von WebDav Advanced Collections definiert, aber 
        /// nicht im „Web Distributed Authoring and Versioning (WebDAV) Ordered 
        /// Collections Protocol“.
        /// </summary>
        UnorderedCollection = 425,
        /// <summary>
        /// 426 - Der Client sollte auf Transport Layer Security (TLS/1.0) umschalten.
        /// </summary>
        UpgradeRequired = 426,
        /// <summary>
        /// 428 - Für die Anfrage sind nicht alle Vorbedingungen erfüllt gewesen. 
        /// Dieser Statuscode soll Probleme durch Race Conditions verhindern, indem 
        /// eine Manipulation oder Löschen nur erfolgt, wenn der Client dies auf Basis 
        /// einer aktuellen Ressource anfordert (Beispielsweise durch Mitliefern eines 
        /// aktuellen ETag-Header).
        /// </summary>
        PreconditionRequired = 428,
        /// <summary>
        /// 429 - Der Client hat zu viele Anfragen in einem bestimmten Zeitraum gesendet.
        /// </summary>
        TooManyRequests = 429,
        /// <summary>
        /// 430 - Die Maximallänge eines Headerfelds oder des Gesamtheaders wurde 
        /// überschritten.
        /// </summary>
        RequestHeaderFieldsTooLarge = 430,
        /// <summary>
        /// 500 - Dies ist ein „Sammel-Statuscode“ für unerwartete Serverfehler.
        /// </summary>
        InternalServerError = 500,
        /// <summary>
        /// 501 - Die Funktionalität, um die Anfrage zu bearbeiten, wird von diesem 
        /// Server nicht bereitgestellt. Ursache ist zum Beispiel eine unbekannte oder 
        /// nicht unterstützte HTTP-Methode.
        /// </summary>
        NotImplemented = 501,
        /// <summary>
        /// 502 - Der Server konnte seine Funktion als Gateway oder Proxy nicht erfüllen, 
        /// weil er seinerseits eine ungültige Antwort erhalten hat.
        /// </summary>
        BadGateway = 502,
        /// <summary>
        /// 503 - Der Server steht temporär nicht zur Verfügung, zum Beispiel wegen 
        /// Überlastung oder Wartungsarbeiten. Ein „Retry-After“-Header-Feld in der 
        /// Antwort kann den Client auf einen Zeitpunkt hinweisen, zu dem die Anfrage 
        /// eventuell bearbeitet werden könnte.
        /// </summary>
        ServiceUnavaible = 503,
        /// <summary>
        /// 504 - Der Server konnte seine Funktion als Gateway oder Proxy nicht 
        /// erfüllen, weil er innerhalb einer festgelegten Zeitspanne keine Antwort 
        /// von seinerseits benutzten Servern oder Diensten erhalten hat.
        /// </summary>
        GatewayTimeOut = 504,
        /// <summary>
        /// 505 - Die benutzte HTTP-Version (gemeint ist die Zahl vor dem Punkt) wird 
        /// vom Server nicht unterstützt oder abgelehnt.
        /// </summary>
        HttpVersionNotSupported = 505,
        /// <summary>
        /// 506 - Die Inhaltsvereinbarung der Anfrage ergibt einen Zirkelbezug.
        /// </summary>
        VariantAlsoNegotiates = 506,
        /// <summary>
        /// 507 - Die Anfrage konnte nicht bearbeitet werden, weil der Speicherplatz 
        /// des Servers dazu zurzeit nicht mehr ausreicht
        /// </summary>
        InsufficientStorage = 507,
        /// <summary>
        /// 508 -  	Die Operation wurde nicht ausgeführt, weil die Ausführung in 
        /// eine Endlosschleife gelaufen wäre. Definiert in der Binding-Erweiterung 
        /// für WebDAV gemäß RFC 5842, weil durch Bindings zyklische Pfade zu
        /// WebDAV-Ressourcen entstehen können.
        /// </summary>
        LoopDetected = 508,
        /// <summary>
        /// 509 - Die Anfrage wurde verworfen, weil sonst die verfügbare Bandbreite 
        /// überschritten würde (inoffizielle Erweiterung einiger Server).
        /// </summary>
        BandwidthLimitExceeded = 509,
        /// <summary>
        /// 510 - Die Anfrage enthält nicht alle Informationen, die die angefragte 
        /// Server-Extension zwingend erwartet.
        /// </summary>
        NotExtended = 510,
    }

    public static class HttpProtocollDefinitions
    {
        public const string HttpVersion1_0 = "HTTP/1.0";
        public const string HttpVersion1_1 = "HTTP/1.1";
        public const string HttpVersion2_0 = "HTTP/2";

        public static bool IsSupported(string Version, string[] SupportedVersions)
        {
            if (Version == null) throw new ArgumentNullException("Version");
            if (SupportedVersions == null) throw new ArgumentNullException("SupportedVersions");
            if (SupportedVersions.Length == 0) return false;
            if (SupportedVersions.Contains(Version)) return true;
            var ind = Version.IndexOf('.');
            if (ind != -1) Version = Version.Remove(ind);
            for (int i = 0; i < SupportedVersions.Length; ++i)
                if (SupportedVersions[i].StartsWith(Version)) return true;
            return false;
        }
    }

    public static class HttpProtocollMethods
    {
        /// <summary>
        /// ist die gebräuchlichste Methode. Mit ihr wird eine Ressource (zum 
        /// Beispiel eine Datei) unter Angabe eines URI vom Server angefordert. 
        /// Als Argumente in dem URI können also auch Inhalte zum Server übertragen 
        /// werden, allerdings soll laut Standard eine GET-Anfrage nur Daten abrufen 
        /// und sonst keine Auswirkungen haben (wie Datenänderungen auf dem Server 
        /// oder ausloggen). Die Länge des URIs ist je nach eingesetztem Server 
        /// begrenzt und sollte aus Gründen der Abwärtskompatibilität nicht länger 
        /// als 255 Bytes sein.
        /// </summary>
        public const string Get = "GET";
        /// <summary>
        /// schickt unbegrenzte, je nach physischer Ausstattung des eingesetzten 
        /// Servers, Mengen an Daten zur weiteren Verarbeitung zum Server, diese 
        /// werden als Inhalt der Nachricht übertragen und können beispielsweise 
        /// aus Name-Wert-Paaren bestehen, die aus einem HTML-Formular stammen. 
        /// Es können so neue Ressourcen auf dem Server entstehen oder bestehende 
        /// modifiziert werden. POST-Daten werden im Allgemeinen nicht von Caches 
        /// zwischengespeichert. Zusätzlich können bei dieser Art der Übermittlung 
        /// auch Daten wie in der GET-Methode an den URI gehängt werden.
        /// </summary>
        public const string Post = "POST";
        /// <summary>
        /// weist den Server an, die gleichen HTTP-Header wie bei GET, nicht jedoch 
        /// den Nachrichtenrumpf mit dem eigentlichen Dokumentinhalt zu senden. So 
        /// kann zum Beispiel schnell die Gültigkeit einer Datei im Browser-Cache 
        /// geprüft werden.
        /// </summary>
        public const string Head = "HEAD";
        /// <summary>
        /// dient dazu eine Ressource (zum Beispiel eine Datei) unter Angabe des 
        /// Ziel-URIs auf einen Webserver hochzuladen. 
        /// Es können so neue Ressourcen auf dem Server entstehen oder bestehende 
        /// modifiziert werden.
        /// </summary>
        public const string Put = "PUT";
        /// <summary>
        /// löscht die angegebene Ressource auf dem Server. Heute ist das, ebenso 
        /// wie PUT, kaum implementiert beziehungsweise in der Standardkonfiguration 
        /// von Webservern abgeschaltet. Beides erlangt jedoch mit RESTful Web 
        /// Services und der HTTP-Erweiterung WebDAV neue Bedeutung.
        /// </summary>
        public const string Delete = "DELETE";
        /// <summary>
        /// liefert die Anfrage so zurück, wie der Server sie empfangen hat. So kann 
        /// überprüft werden, ob und wie die Anfrage auf dem Weg zum Server verändert 
        /// worden ist – sinnvoll für das Debugging von Verbindungen.
        /// </summary>
        public const string Trace = "TRACE";
        /// <summary>
        /// liefert eine Liste der vom Server unterstützen Methoden und Merkmale.
        /// </summary>
        public const string Options = "OPTIONS";
        /// <summary>
        /// wird von Proxyservern implementiert, die in der Lage sind, 
        /// SSL-Tunnel zur Verfügung zu stellen.
        /// </summary>
        public const string Connect = "CONNECT";
        /// <summary>
        /// RFC 5789 definiert zusätzlich eine PATCH-Methode, um Ressourcen zu 
        /// modifizieren – in Abgrenzung zur PUT-Methode, deren Intention das 
        /// Hochladen der kompletten Ressource ist.
        /// </summary>
        public const string Patch = "PATCH";
    }

    #endregion

    #region Document Definition

    public class HttpDocument : IDisposable
    {
        private List<HttpDataSource> dataSources = new List<HttpDataSource>();
        public List<HttpDataSource> DataSources
        {
            get { return dataSources; }
        }

        public string PrimaryMime
        {
            get { return dataSources.Count == 0 ? null : dataSources[0].MimeType; }
        }

        private string primaryEncoding = null;
        public string PrimaryEncoding
        {
            get { return primaryEncoding; }
            set { primaryEncoding = value; }
        }

        public HttpRequestHeader RequestHeader { get; set; }

        public HttpResponseHeader ResponseHeader { get; set; }

        private Dictionary<object, object> information = new Dictionary<object, object>();
        public Dictionary<object, object> Information
        {
            get { return information; }
        }

        public object this[object identifer]
        {
            get { return Information[identifer]; }
            set { Information[identifer] = value; }
        }

        public HttpSession Session { get; set; }

        public void Dispose()
        {
            foreach (var ds in dataSources.ToArray()) ds.Dispose();
            dataSources.Clear();
            foreach (var kvp in information.ToArray())
            {
                if (kvp.Key is IDisposable) ((IDisposable)kvp.Key).Dispose();
                if (kvp.Value is IDisposable) ((IDisposable)kvp.Value).Dispose();
            }
            information.Clear();
        }
    }

    public class HttpSession
    {
        public long InternalSessionKey { get; set; }

        public byte[] PublicSessionKey { get; set; }

        public string Ip { get; set; }

        public TcpClient NetworkCient { get; set; }

        public int LastWorkTime { get; set; }

        private Dictionary<object, object> sessionInformation = new Dictionary<object, object>();
        public Dictionary<object, object> SessionInformation
        {
            get { return sessionInformation; }
        }

        public void AlwaysSyncSessionInformation(Dictionary<object, object> information)
        {
            sessionInformation = information;
        }
    }

    /// <summary>
    /// Hier ist eine kleine Auswahl der MIME-Typen.
    /// </summary>
    public static class MimeTypes
    {
        /// <summary>
        /// Microsoft Excel Dateien (*.xls *.xla)
        /// </summary>
        public const string ApplicationMsexcel = "application/msexcel";
        /// <summary>
        /// Microsoft Powerpoint Dateien (*.ppt *.ppz *.pps *.pot)
        /// </summary>
        public const string ApplicationMspowerpoint = "application/mspowerpoint";
        /// <summary>
        /// Microsoft Word Dateien (*.doc *.dot)
        /// </summary>
        public const string ApplicationMsword = "application/msword";
        /// <summary>
        /// GNU Zip-Dateien (*.gz)
        /// </summary>
        public const string ApplicationGzip = "application/gzip";
        /// <summary>
        /// JSON Dateien (*.json)
        /// </summary>
        public const string ApplicationJson = "application/json";
        /// <summary>
        /// Nicht näher spezifizierte Daten, z.B. ausführbare Dateien (*.bin *.exe *.com *.dll *.class)
        /// </summary>
        public const string ApplicationOctetStream = "application/octet-stream";
        /// <summary>
        /// PDF-Dateien (*.pdf)
        /// </summary>
        public const string ApplicationPdf = "application/pdf";
        /// <summary>
        /// RTF-Dateien (*.rtf)
        /// </summary>
        public const string ApplicationRtf = "application/rtf";
        /// <summary>
        /// XHTML-Dateien (*.htm *.html *.shtml *.xhtml)
        /// </summary>
        public const string ApplicationXhtml = "application/xhtml+xml";
        /// <summary>
        /// XML-Dateien (*.xml)
        /// </summary>
        public const string ApplicationXml = "application/xml";
        /// <summary>
        /// PHP-Dateien (*.php *.phtml)
        /// </summary>
        public const string ApplicationPhp = "application/x-httpd-php";
        /// <summary>
        /// serverseitige JavaScript-Dateien (*.js)
        /// </summary>
        public const string ApplicationJs = "application/x-javascript";
        /// <summary>
        /// ZIP-Archivdateien (*.zip)
        /// </summary>
        public const string ApplicationZip = "application/zip";
        /// <summary>
        /// MPEG-Audiodateien (*.mp2)
        /// </summary>
        public const string AudioMpeg = "audio/x-mpeg";
        /// <summary>
        /// WAV-Dateien (*.wav)
        /// </summary>
        public const string AudioWav = "audio/x-wav";
        /// <summary>
        /// GIF-Dateien (*.gif)
        /// </summary>
        public const string ImageGif = "image/gif";
        /// <summary>
        /// JPEG-Dateien (*.jpeg *.jpg *.jpe)
        /// </summary>
        public const string ImageJpeg = "image/jpeg";
        /// <summary>
        /// PNG-Dateien (*.png *.pneg)
        /// </summary>
        public const string ImagePng = "image/png";
        /// <summary>
        /// Icon-Dateien (z.B. Favoriten-Icons) (*.ico)
        /// </summary>
        public const string ImageIcon = "image/x-icon";
        /// <summary>
        /// mehrteilige Daten; jeder Teil ist eine zu den anderen gleichwertige Alternative 
        /// </summary>
        public const string MultipartAlternative = "multipart/alternative";
        /// <summary>
        /// mehrteilige Daten mit Byte-Angaben 
        /// </summary>
        public const string MultipartByteranges = "multipart/byteranges";
        /// <summary>
        /// mehrteilige Daten verschlüsselt 
        /// </summary>
        public const string MultipartEncrypted = "multipart/encrypted";
        /// <summary>
        /// mehrteilige Daten aus HTML-Formular (z.B. File-Upload) 
        /// </summary>
        public const string MultipartFormData = "multipart/form-Data";
        /// <summary>
        /// mehrteilige Daten ohne Bezug der Teile untereinander 
        /// </summary>
        public const string MultipartMixed = "multipart/mixed";
        /// <summary>
        /// CSS Stylesheet-Dateien (*.css)
        /// </summary>
        public const string TextCss = "text/css";
        /// <summary>
        /// HTML-Dateien (*.htm *.html *.shtml)
        /// </summary>
        public const string TextHtml = "text/html";
        /// <summary>
        /// JavaScript-Dateien (*.js)
        /// </summary>
        public const string TextJs = "text/javascript";
        /// <summary>
        /// reine Textdateien (*.txt)
        /// </summary>
        public const string TextPlain = "text/plain";
        /// <summary>
        /// RTF-Dateien (*.rtf)
        /// </summary>
        public const string TextRtf = "text/rtf";
        /// <summary>
        /// XML-Dateien (*.xml)
        /// </summary>
        public const string TextXml = "text/xml";
        /// <summary>
        /// MPEG-Videodateien (*.mpeg *.mpg *.mpe)
        /// </summary>
        public const string VideoMpeg = "video/mpeg";
        /// <summary>
        /// Microsoft AVI-Dateien (*.avi)
        /// </summary>
        public const string VideoAvi = "video/x-msvideo";
        /// <summary>
        /// Überprüft ob ein Mimetyp mit einen Muster übereinstimmt.
        /// </summary>
        /// <param name="mime">Der Mimetypstring</param>
        /// <param name="pattern">
        /// Das Muster. Es hat dasselbe Format wie ein Mimetyp, kann aber * als 
        /// Platthalter haben (z.B. "text/plain", "text/*", "*/plain", "*/*").
        /// </param>
        /// <returns>Ergebnis der Überprüfung</returns>
        public static bool Check(string mime, string pattern)
        {
            if (mime == null) throw new ArgumentNullException("mime");
            if (pattern == null) throw new ArgumentNullException("pattern");
            var ind = mime.IndexOf('/');
            if (ind == -1) throw new ArgumentException("no Mime", "mime");
            var ml = mime.Remove(ind).ToLower();
            var mh = mime.Substring(ind + 1).ToLower();
            ind = pattern.IndexOf('/');
            if (ind == -1) throw new ArgumentException("no Mime", "pattern");
            var pl = pattern.Remove(ind).ToLower();
            var ph = pattern.Substring(ind + 1).ToLower();
            return (pl == "*" || pl == ml) && (ph == "*" || ph == mh);
        }
    }

    public abstract class HttpDataSource : IDisposable
    {
        public abstract void Dispose();

        public abstract long AproximateLength();

        private string mimeType = MimeTypes.TextHtml;
        public string MimeType
        {
            get { return mimeType; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    value = "text/html";
                mimeType = value;
            }
        }

        public abstract long WriteToStream(System.IO.Stream networkStream);

        public abstract long ReadFromStream(System.IO.Stream networkStream, long readlength);

        public bool NeedBufferManagement { get; protected set; }

        public abstract byte[] GetSourcePart(long start, long length);

        public abstract int WriteSourcePart(byte[] source, long start, long length);

        public abstract long ReserveExtraMemory(long bytes);

        private long rangeStart;
        public long RangeStart
        {
            get
            {
                if (TransferCompleteData) rangeStart = 0;
                return rangeStart;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("RangeStart");
                if (value > 0) TransferCompleteData = false;
                else if (rangeEnd == AproximateLength() - 1) TransferCompleteData = true;
                rangeStart = value;
            }
        }

        private long rangeEnd;
        public long RangeEnd
        {
            get
            {
                if (TransferCompleteData) rangeEnd = AproximateLength() - 1;
                return rangeEnd;
            }
            set
            {
                var length = AproximateLength();
                if (value != length - 1) TransferCompleteData = false;
                else if (rangeStart == 0) TransferCompleteData = true;
                rangeEnd = value;
            }
        }

        private bool transferCompleteData;
        public bool TransferCompleteData
        {
            get { return transferCompleteData; }
            set
            {
                if (transferCompleteData = value)
                {
                    rangeStart = 0;
                    rangeEnd = AproximateLength();
                }
            }
        }
    }

    public class HttpStringDataSource : HttpDataSource
    {
        private string data = "";
        public string Data
        {
            get { return data; }
            set
            {
                data = value ?? throw new ArgumentNullException("Data");
            }
        }

        private string encoding;
        public string TextEncoding
        {
            get { return encoding; }
            set
            {
                encoding = value;
                Encoder = Encoding.GetEncoding(value);
            }
        }

        Encoding Encoder;

        public HttpStringDataSource(string data)
        {
            Data = data ?? throw new ArgumentNullException("data");
            NeedBufferManagement = false;
            Encoder = Encoding.UTF8;
            encoding = Encoder.WebName;
            TransferCompleteData = true;
        }

        public override void Dispose()
        {
        }

        public override long AproximateLength()
        {
            return Encoder.GetByteCount(Data);
        }

        public override long WriteToStream(System.IO.Stream networkStream)
        {
            var data = Encoder.GetBytes(Data);
            long length = 0;
            try
            {
                if (TransferCompleteData) networkStream.Write(data, 0, (int)(length = data.Length));
                else networkStream.Write(data, (int)RangeStart,
                    (int)(length = Math.Min(RangeEnd, data.Length) - RangeStart));
            }
            catch (IOException)
            {
                WebServerInfo.Add(InfoType.Error, GetType(), "Send", "Connection closed by remote Host");
                return -1;
            }
            return length;
        }

        public override long ReadFromStream(System.IO.Stream networkStream, long readlength)
        {
            var l = new List<byte>();
            var buffer = new byte[4 * 1024];
            long readed = 0;
            do
            {
                var length = readlength == 0 ? buffer.Length : readlength - l.Count;
                readed = networkStream.Read(buffer, 0, (int)length);
                if (readed != 0)
                    l.AddRange(buffer.ToList().GetRange(0, (int)readed));
            }
            while (readed == 0);
            Data = Encoder.GetString(l.ToArray());
            return l.Count;
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            var b = Encoder.GetBytes(Data);
            return b.ToList().GetRange((int)start, (int)Math.Min(length, b.Length - start)).ToArray();
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            var b = Encoder.GetBytes(Data);
            for (int i = 0; i < length; ++i) b[start + i] = source[i];
            Data = Encoder.GetString(b);
            return source.Length;
        }

        public override long ReserveExtraMemory(long bytes)
        {
            return 0; //Nicht notwendig
        }
    }

    public class HttpFileDataSource : HttpDataSource
    {
        public System.IO.FileStream File { get; private set; }
        private string path = null;
        public virtual string Path
        {
            get { return path; }
            set
            {
                if (path == value) return;
                if (File != null) File.Dispose();
                if (value == null) File = null;
                else
                {
                    var fi = new System.IO.FileInfo(value);
                    if (!fi.Directory.Exists) fi.Directory.Create();
                    File = new System.IO.FileStream(value, System.IO.FileMode.OpenOrCreate,
                        ReadOnly ? FileAccess.Read : FileAccess.ReadWrite, ReadOnly ? FileShare.Read : FileShare.ReadWrite);
                }
                path = value;
            }
        }

        public bool ReadOnly { get; private set; }

        public HttpFileDataSource(string path, bool readOnly = true)
        {
            ReadOnly = readOnly;
            Path = path;
        }

        public override void Dispose()
        {
            Path = null;
        }

        public override long AproximateLength()
        {
            if (File == null) return 0;
            else return File.Length;
        }

        public override long WriteToStream(System.IO.Stream networkStream)
        {
            File.Position = TransferCompleteData ? 0 : RangeStart;
            var buffer = new byte[4 * 1024];
            long readed = 0;
            do
            {
                var a1 = AproximateLength() - readed;
                var a2 = Math.Min(RangeEnd, AproximateLength());
                var a3 = Math.Min(RangeEnd, AproximateLength()) - RangeStart - readed;
                var a4 = TransferCompleteData ?
                    AproximateLength() - readed :
                    Math.Min(RangeEnd, AproximateLength()) - RangeStart - readed;
                var a5 = Math.Min(buffer.Length, TransferCompleteData ?
                    AproximateLength() - readed :
                    Math.Min(RangeEnd, AproximateLength()) - RangeStart - readed);
                var length = (int)Math.Min(buffer.Length, TransferCompleteData ?
                    AproximateLength() - readed :
                    Math.Min(RangeEnd, AproximateLength()) - RangeStart - readed);
                readed += File.Read(buffer, 0, length);
                try
                {
                    networkStream.Write(buffer, 0, length);
                }
                catch (IOException)
                {
                    WebServerInfo.Add(InfoType.Error, GetType(), "Send", "Connection closed by remote Host");
                    return -1;
                }
            }
            while (readed != (TransferCompleteData ? AproximateLength() :
                Math.Min(RangeEnd, AproximateLength()) - RangeStart));
            //WebServerInfo.Add(InfoType.Debug, GetType(), "Send", "Sended {0:#,#0} Bytes", readed);
            return readed;
        }

        public override long ReadFromStream(System.IO.Stream networkStream, long readlength)
        {
            File.Position = 0;
            var buffer = new byte[4 * 1024];
            long readed = 0;
            int r = 0;
            do
            {
                var length = readlength == 0 ? buffer.Length :
                    (int)Math.Min(buffer.Length, readlength - readed);
                r = networkStream.Read(buffer, 0, length);
                File.Write(buffer, 0, r);
                readed += r;
            }
            while (r != 0);
            File.SetLength(readed);
            return readed;
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            var b = new byte[length];
            File.Position = start;
            File.Read(b, 0, b.Length);
            return b;
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            File.Position = start;
            File.Write(source, 0, (int)length);
            return (int)length;
        }

        public override long ReserveExtraMemory(long bytes)
        {
            File.SetLength(File.Length + bytes);
            return bytes;
        }
    }

    public class HttpStreamDataSource : HttpDataSource
    {
        public Stream Stream { get; set; }

        public bool ReadOnly { get; private set; }

        public HttpStreamDataSource(Stream stream, bool readOnly = true)
        {
            ReadOnly = readOnly;
            Stream = stream;
        }

        public override void Dispose()
        {
            Stream?.Dispose();
        }

        public override long AproximateLength()
        {
            if (Stream == null) return 0;
            else return Stream.Length;
        }

        public override long WriteToStream(Stream networkStream)
        {
            Stream.Position = TransferCompleteData ? 0 : RangeStart;
            var buffer = new byte[4 * 1024];
            long readed = 0;
            do
            {
                var length = (int)Math.Min(buffer.Length, TransferCompleteData ?
                    AproximateLength() - readed :
                    Math.Min(RangeEnd, AproximateLength()) - RangeStart - readed);
                readed += Stream.Read(buffer, 0, length);
                try
                {
                    networkStream.Write(buffer, 0, length);
                }
                catch (IOException)
                {
                    WebServerInfo.Add(InfoType.Error, GetType(), "Send", "Connection closed by remote Host");
                    return -1;
                }
            }
            while (readed != (TransferCompleteData ? AproximateLength() :
                Math.Min(RangeEnd, AproximateLength()) - RangeStart));
            //WebServerInfo.Add(InfoType.Debug, GetType(), "Send", "Sended {0:#,#0} Bytes", readed);
            return readed;
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            Stream.Position = 0;
            var buffer = new byte[4 * 1024];
            long readed = 0;
            int r = 0;
            do
            {
                var length = readlength == 0 ? buffer.Length :
                    (int)Math.Min(buffer.Length, readlength - readed);
                r = networkStream.Read(buffer, 0, length);
                Stream.Write(buffer, 0, r);
                readed += r;
            }
            while (r != 0);
            Stream.SetLength(readed);
            return readed;
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            var b = new byte[length];
            Stream.Position = start;
            Stream.Read(b, 0, b.Length);
            return b;
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            Stream.Position = start;
            Stream.Write(source, 0, (int)length);
            return (int)length;
        }

        public override long ReserveExtraMemory(long bytes)
        {
            Stream.SetLength(Stream.Length + bytes);
            return bytes;
        }
    }
   
    #endregion

    #region Web Service Definition

    public abstract class WebService
    {
        public WebServiceType ServiceType { get; private set; }

        public WebService(WebServiceType type)
        {
            ServiceType = type;
            Importance = WebProgressImportance.Normal;
        }

        public abstract void ProgressTask(WebProgressTask task);

        public abstract bool CanWorkWith(WebProgressTask task);

        public WebProgressImportance Importance { get; protected set; }
    }

    public class WebServiceGroup
    {
        public WebServiceType ServiceType { get; private set; }

        public WebServiceGroup(WebServiceType type)
        {
            ServiceType = type;
        }

        public virtual bool SingleExecution
        {
            get
            {
                switch (ServiceType)
                {
                    case WebServiceType.PostCreateDocument: return false;
                    case WebServiceType.PostCreateResponse: return false;
                    case WebServiceType.PostParseRequest: return false;
                    case WebServiceType.PreCreateDocument: return true;
                    case WebServiceType.PreCreateResponse: return false;
                    case WebServiceType.PreParseRequest: return true;
                    case WebServiceType.SendResponse: return true;
                    default: throw new NotImplementedException("ServiceType: " + ServiceType.ToString() + " is not implemented");
                }
            }
        }

        private List<WebService> webServices = new List<WebService>();
        public List<WebService> WebServices
        {
            get { return webServices; }
        }

        public T Get<T>() where T : WebService
        {
            return WebServices.Find((ws) => ws is T) as T;
        }

        public virtual void Execute(WebProgressTask task)
        {
            var se = SingleExecution;
            var set = false;
            for (var type = WebProgressImportance.God; (int)type >= 0; type = (WebProgressImportance)((int)type - 1))
            {
                for (int i = 0; i < WebServices.Count; ++i)
                {
                    if (WebServices[i].Importance != type) continue;
                    if (task.Session.NetworkCient != null && !task.Session.NetworkCient.Connected) return;
                    if (WebServices[i].CanWorkWith(task))
                    {
                        if (task.Session.NetworkCient != null && !task.Session.NetworkCient.Connected) return;
                        WebServices[i].ProgressTask(task);
                        task.Document[ServiceType] = true;
                        if (se) return;
                        set = true;
                    }
                }
            }
            if (!set) task.Document[ServiceType] = false;
        }
    }

    public class WebProgressTask : IDisposable
    {
        public HttpDocument Document { get; set; }

        public System.IO.Stream NetworkStream { get; set; }

        public WebServiceType NextTask { get; set; }

        public WebServiceType CurrentTask { get; set; }

        public WebServer Server { get; set; }

        public HttpSession Session { get; set; }

        public void Dispose()
        {
            Document.Dispose();
        }
    }

    /// <summary>
    /// Definitiert den Webservice
    /// </summary>
    public enum WebServiceType
    {
        /// <summary>
        /// Diese Gruppe verarbeitet nur die Nachrichten, die hereingekommen sind. Dazu werden nur die Daten aus dem Request geparst.
        /// </summary>
        PreParseRequest = 1,
        /// <summary>
        /// Diese Gruppe verarbeitet die geparsten Anforderungen. Hier wird das weitere Vorgehen bestimmt.
        /// </summary>
        PostParseRequest = 2,
        /// <summary>
        /// Hier wird ein Dokument vorverarbeitet. Dazu wird nur das Dokument geladen und bereitgestellt. Mit dem Dokument selbst wird nicht 
        /// gearbeitet.
        /// </summary>
        PreCreateDocument = 3,
        /// <summary>
        /// Hier wird ein Dokument nachverarbeitet. Hier wird eventuell Code ausgeführt, die das Dokument verändern.
        /// </summary>
        PostCreateDocument = 4,
        /// <summary>
        /// Hier wird die Antwort vorbereitet. Dazu wird ein Header generiert. Dazu dürfen nur Informationen aus dem Request bezogen werden.
        /// </summary>
        PreCreateResponse = 5,
        /// <summary>
        /// Hier wird die Antwort fertig gestellt. Hier werden Informationen aus dem Dokument dem Header ergänzt.
        /// </summary>
        PostCreateResponse = 6,
        /// <summary>
        /// Sended die Nachricht ab. Das ist der letzte Teil einer Abfrage.
        /// </summary>
        SendResponse = 7
    }

    public enum WebProgressImportance
    {
        VeryLow,
        Low,
        Normal,
        High,
        VeryHigh,
        God
    }

    #endregion

    #region Easy Task Creaton

    public class WebServerTaskCreator
    {
        public WebProgressTask Task { get; private set; }

        public WebServiceType TerminationState { get; set; }

        public WebServerTaskCreator()
        {
            Task = new WebProgressTask();
            Task.CurrentTask = WebServiceType.PreParseRequest;
            Task.Document = new HttpDocument();
            Task.Document.RequestHeader = new HttpRequestHeader();
            Task.Document.ResponseHeader = new HttpResponseHeader();
            Task.Document.Session = new HttpSession();
            Task.Document.Session.InternalSessionKey = 0;
            Task.Document.Session.Ip = "127.0.0.1";
            Task.Document.Session.LastWorkTime = -1;
            Task.Document.Session.PublicSessionKey = new byte[0];
            Task.NetworkStream = new MemoryStream();
            Task.Session = Task.Document.Session;
            TerminationState = WebServiceType.SendResponse;
        }

        public void Start(WebServer server)
        {
            Task.Server = server;
            server.ListenFromTaskCreator(Task, TerminationState);
            Task.Server = null;
        }

        public void SetProtocolHeader(string url, string method = "GET", string protocol = "HTTP/1.1")
        {
            Task.Document.RequestHeader.ProtocolMethod = method;
            Task.Document.RequestHeader.Url = url;
            Task.Document.RequestHeader.HttpProtocol = protocol;
        }

        public void SetHeaderParameter(string key, string value)
        {
            if (Task.Document.RequestHeader.HeaderParameter.ContainsKey(key))
                Task.Document.RequestHeader.HeaderParameter[key] = value;
            else Task.Document.RequestHeader.HeaderParameter.Add(key, value);
        }

        public void SetPost(string post)
        {
            Task.Document.RequestHeader.Post.SetPost(post);
        }

        public void SetAccept(string[] acceptTypes = null, string[] encoding = null)
        {
            if (acceptTypes != null) Task.Document.RequestHeader.FieldAccept.AddRange(acceptTypes);
            if (encoding != null) Task.Document.RequestHeader.FieldAcceptEncoding.AddRange(acceptTypes);
        }

        public void SetHost(string host)
        {
            Task.Document.RequestHeader.Host = host;
        }

        public void SetCookie(string cookieString)
        {
            Task.Document.RequestHeader.Cookie.SetRequestCookieString(cookieString);
        }

        public void SetStream(Stream stream)
        {
            Task.NetworkStream = stream;
        }

        public void SetStream(Stream input, Stream output)
        {
            Task.NetworkStream = new BidirectionalStream(input, output);
        }

        public class BidirectionalStream : Stream
        {
            public Stream Input { get; private set; }

            public Stream Output { get; private set; }

            public BidirectionalStream(Stream input, Stream output)
            {
                if (!input.CanRead) throw new ArgumentException("input is not readable");
                if (!output.CanWrite) throw new ArgumentException("output is not writeable");
                Input = input;
                Output = output;
            }

            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return true;
                }
            }

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }

                set
                {
                    throw new NotSupportedException();
                }
            }

            public override void Flush()
            {
                Output.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Input.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                Output.Write(buffer, offset, count);
            }
        }
    }

    #endregion
}
