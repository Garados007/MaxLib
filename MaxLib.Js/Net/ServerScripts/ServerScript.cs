using Jint;
using MaxLib.Net.Webserver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MaxLib.Net.ServerScripts
{
    public class ServerScript
    {
        public static bool IsDebugMode = System.Diagnostics.Debugger.IsAttached;

        Engine js;
        public bool NeedJsTag { get; set; }
        public ServerScriptObject ScriptObject { get; protected set; }
        public Encoding Encoding { get; set; }
        public bool ExportReturnValue { get; set; }
        public bool MergeJS { get; set; }

        public ServerScript(WebServer server)
        {
            NeedJsTag = true;
            Encoding = Encoding.UTF8;
            ExportReturnValue = false;
            ScriptObject = new ServerScriptObject(server ?? throw new ArgumentNullException("server"));
            js = new Engine((cfg) =>
            {
                //Nach 1 Sekunde Ausführung soll das Script abgebrochen werden.
#if DEBUG
                if (!IsDebugMode) cfg.TimeoutInterval(new TimeSpan(0, 0, 1));
                else cfg.DebugMode();
#else
                cfg.TimeoutInterval(new TimeSpan(0, 0, 1));
#endif
            });
            js.SetValue("$", ScriptObject);
        }

        public Stream Parse(Stream source)
        {
            var p = new parser();
            p.js = js;
            p.needJsTag = NeedJsTag;
            p.sso = ScriptObject;
            p.source = source;
            p.encoding = Encoding;
            p.exportReturn = ExportReturnValue;
            p.mergeJS = MergeJS;
            p.Start();
            p.target.Position = 0;
            return p.target;
        }

        class parser
        {
            public Engine js;
            public bool needJsTag;
            public ServerScriptObject sso;
            public Stream source;
            public Stream target;
            public StreamWriter w;
            public Encoding encoding;
            public bool exportReturn;
            public bool mergeJS;

            public void Start()
            {
                w = new StreamWriter(target = new MemoryStream((int)source.Length));
                w.AutoFlush = true;
                sso.Write = (text) => { if (text != null) w.Write(text); };
                readAll();
                w.Flush();
            }

            bool isTag = false;

            void Execute(string task)
            {
                try { js.Execute(task); }
                catch (Jint.Parser.ParserException e)
                {
                    w.Write("# Unhandled Parse Exception: {0} at ({1},{2})", e.Message, e.LineNumber, e.Column);
                    return;
                }
                catch (Jint.Runtime.JavaScriptException e)
                {
                    w.Write("# Unhandled Runtime Exception: {0} at ({1},{2})", e.Error.ToString(), e.LineNumber, e.Column);
                    return;
                }
                if (exportReturn)
                {
                    var result = js.GetCompletionValue();
                    if (result != null)
                    {
                        w.Write(result.ToString());
                    }
                }
            }

            void readAll()
            {
                if (mergeJS)
                {
                    var l = new List<string>();
                    var r = new StringBuilder();
                    while (source.Position < source.Length)
                    {
                        var isTag = this.isTag;
                        var text = readRaw();
                        if (text == "") continue;
                        if (isTag) r.Append(text);
                        else
                        {
                            r.AppendFormat("$.DirectBlockOut({0}); ", l.Count);
                            l.Add(text);
                        }
                    }
                    sso.DirectBlockOut = (num) =>
                    {
                        if (num >= 0 && num < l.Count)
                            w.Write(l[num]);
                    };
                    Execute(r.ToString());
                }
                else
                {
                    while (source.Position < source.Length)
                    {
                        var isTag = this.isTag;
                        var text = readRaw();
                        if (text == "") continue;
                        if (isTag) Execute(text);
                        else w.Write(text);
                    }
                }
            }

            string readRaw()
            {
                var sb = new StringBuilder();
                if (isTag)
                {
                    var level = 0;
                    do
                    {
                        var c = GetNextChar();
                        if (c == 0) break;
                        switch (level)
                        {
                            case 0: if (c == '?') level++; else sb.Append(c); break;
                            case 1: if (c == '>') level++; else { sb.Append("?" + c); level = 0; } break;
                        }
                    }
                    while (level < 2);
                    isTag = false;
                }
                else
                {
                    var level = 0;
                    var buffer = "";
                    do
                    {
                        var c = GetNextChar();
                        if (c == 0) break;
                        switch (level)
                        {
                            case 0: if (c == '<') { level++; buffer = "" + c; } else sb.Append(c); break;
                            case 1: if (c == '?') { level++; buffer += c; } else { sb.Append(buffer + c); level = 0; } break;
                            case 2: if (c == 'j' || c == 'J') { level++; buffer += c; } else { sb.Append(buffer + c); level = 0; } break;
                            case 3: if (c == 's' || c == 'S') { level++; buffer += c; } else { sb.Append(buffer + c); level = 0; } break;
                        }
                    }
                    while (needJsTag ? level < 4 : level < 2);
                    isTag = true;
                }
                return sb.ToString();
            }

            char GetNextChar()
            {
                if (source.Position >= source.Length) return (char)0;
                var buf = new byte[encoding.GetMaxByteCount(1)];
                var length = 1;
                source.Read(buf, 0, length);
                if (encoding.WebName == "utf-8") //UTF-8 FIX
                {
                    const byte mask2byte = 0xC0;
                    const byte mask3byte = 0xE0;
                    const byte mask4byte = 0xF0;
                    const byte mask5byte = 0xF8;
                    const byte mask6byte = 0xFC;
                    if ((buf[0] & mask6byte) == mask6byte)
                    {
                        source.Read(buf, 1, 5);
                        length += 5;
                    }
                    else if ((buf[0] & mask5byte) == mask5byte)
                    {
                        source.Read(buf, 1, 4);
                        length += 4;
                    }
                    else if ((buf[0] & mask4byte) == mask4byte)
                    {
                        source.Read(buf, 1, 3);
                        length += 3;
                    }
                    else if ((buf[0] & mask3byte) == mask3byte)
                    {
                        source.Read(buf, 1, 2);
                        length += 2;
                    }
                    else if ((buf[0] & mask2byte) == mask2byte)
                    {
                        source.Read(buf, 1, 1);
                        length++;
                    }
                }
                var s = encoding.GetString(buf, 0, length);
                return s[0];
            }
        }
    }

    public class ServerScriptObject
    {
        protected WebServer Server;

        public ServerScriptObject(WebServer server)
        {
            Server = server ?? throw new ArgumentNullException("server");
        }

        #region Variablen

        public DateTime Now { get { return DateTime.Now; } }

        public Dictionary<string, string> GetParam { get; set; }

        #endregion

        #region Allgemeine Methoden

        /// <summary>
        /// Schreibt einen Text direkt in den Ausgabestrom
        /// </summary>
        public Action<string> Write;

        /// <summary>
        /// Schreibt einen Block direkt in den Ausgabestrom. Diese Methode
        /// wird nur durch den Compiler verwendet.
        /// </summary>
        public Action<int> DirectBlockOut;

        /// <summary>
        /// Lädt den Inhalt von einer bestimmten URL herunter 
        /// </summary>
        /// <param name="url">die URL</param>
        /// <returns>Der Inhalt von der URL</returns>
        public virtual string WebGet(string url)
        {
            using (var wc = new System.Net.WebClient())
            {
                var uri = new Uri(new Uri("http://localhost:" + Server.Settings.Port.ToString() + "/"), url);
                return wc.DownloadString(uri);
            }
        }

        /// <summary>
        /// Führt einen HTTP-POST mit Argumenten aus und lädt dann die Antwort herunter.
        /// </summary>
        /// <param name="url">die URL</param>
        /// <param name="value">Ein JavaScript Objekt mit Werten.</param>
        /// <returns>Die Antwort von der URL</returns>
        public virtual string WebPost(string url, Jint.Native.JsValue value)
        {
            if (!value.IsObject()) return WebGet(url);
            var obj = value.AsObject();
            using (var wc = new System.Net.WebClient())
            {
                var nvc = new System.Collections.Specialized.NameValueCollection();
                foreach (var param in obj.GetOwnProperties())
                {
                    nvc.Add(param.Key, param.Value.Value.ToString());
                }
                var uri = new Uri(new Uri("http://localhost:" + Server.Settings.Port.ToString() + "/"), url);
                var result = wc.UploadValues(uri, "POST", nvc);
                return wc.Encoding.GetString(result);
            }
        }

        /// <summary>
        /// Lädt den Inhalt zu einer URL von diesem Server herunter. Die Url muss relativ zum Root sein und immer mit
        /// einem / beginnen. Pfad werden NICHT gemixt. Außerdem wird diese Methode empfohlen, da sie deutlich 
        /// schneller als die andere ist (es erfolgt kein Zugriff auf das Netzwerk und alle Daten werden direkt 
        /// verarbeitet).
        /// </summary>
        /// <param name="url">Die lokale URL zur Ressource</param>
        /// <returns>Die verarbeitete Antwort</returns>
        public virtual string LocalGet(string url)
        {
            var task = new WebServerTaskCreator();
            task.SetProtocolHeader(url);
            task.TerminationState = WebServiceType.PostCreateDocument;
            task.Task.CurrentTask = WebServiceType.PostParseRequest;
            task.Task.NextTask = WebServiceType.PreCreateDocument;
            task.Start(Server);
            if (task.Task.Document.DataSources.Count == 0) return "";
            var ds = task.Task.Document.DataSources[0];
            using (var m = new MemoryStream((int)ds.AproximateLength()))
            {
                ds.WriteToStream(m);
                m.Position = 0;
                var bytes = new BinaryReader(m).ReadBytes((int)m.Length);
                return Encoding.UTF8.GetString(bytes);
            }
        }
                
        #endregion

        #region Kurzschreibweisen

        /// <summary>
        /// Schreibt Text direkt in den Ausgabestrom
        /// </summary>
        /// <param name="text">Auszugebender Text</param>
        public void w(string text)
        {
            Write?.Invoke(text);
        }

        /// <summary>
        /// Lädt den Inhalt von einer bestimmten URL herunter 
        /// </summary>
        /// <param name="url">die URL</param>
        /// <returns>Der Inhalt von der URL</returns>
        public string wget(string url)
        {
            return WebGet(url);
        }

        /// <summary>
        /// Führt einen HTTP-POST mit Argumenten aus und lädt dann die Antwort herunter.
        /// </summary>
        /// <param name="url">die URL</param>
        /// <param name="value">Ein JavaScript Objekt mit Werten.</param>
        /// <returns>Die Antwort von der URL</returns>
        public string wpost(string url, Jint.Native.JsValue value)
        {
            return WebPost(url, value);
        }

        /// <summary>
        /// Lädt den Inhalt zu einer URL von diesem Server herunter. Die Url muss relativ zum Root sein und immer mit
        /// einem / beginnen. Pfad werden NICHT gemixt. Außerdem wird diese Methode empfohlen, da sie deutlich 
        /// schneller als die andere ist (es erfolgt kein Zugriff auf das Netzwerk und alle Daten werden direkt 
        /// verarbeitet).
        /// </summary>
        /// <param name="url">Die lokale URL zur Ressource</param>
        /// <returns>Die verarbeitete Antwort</returns>
        public string lget(string url)
        {
            return LocalGet(url);
        }

        #endregion
    }
}
