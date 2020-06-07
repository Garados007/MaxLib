using System;
using System.Collections.Generic;
using System.IO;

namespace MaxLib.Net.Webserver.Services
{
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
            public string[] UrlMappedPath { get; }

            public string LocalMappedPath { get; }

            public bool DenyAccess { get; }

            public bool File { get; }

            public Rule(string urlPath, string localPath, bool denyAccess, bool file = true)
            {
                _ = urlPath ?? throw new ArgumentNullException(nameof(urlPath));
                _ = localPath ?? throw new ArgumentNullException(nameof(localPath));

                UrlMappedPath = urlPath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (!Directory.Exists(localPath)) Directory.CreateDirectory(localPath);
                LocalMappedPath = localPath;
                DenyAccess = denyAccess;
                File = file;
            }

            public virtual bool SameUrlBasePath(string[] url)
            {
                _ = url ?? throw new ArgumentNullException(nameof(url));

                if (url.Length == 1 && url[0] == "" && UrlMappedPath.Length == 0) return true;
                for (int i = 0; i < Math.Min(UrlMappedPath.Length, url.Length); ++i)
                {
                    if (url[i] != UrlMappedPath[i]) return false;
                }
                return url.Length >= UrlMappedPath.Length;
            }
        }

        public List<Rule> Rules { get; } = new List<Rule>();

        public void Add(Rule rule)
            => Rules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

        public void Add(string urlPath, string localPath, bool denyAccess, bool file = true)
        {
            _ = urlPath ?? throw new ArgumentNullException(nameof(urlPath));
            _ = localPath ?? throw new ArgumentNullException(nameof(localPath));

            Add(new Rule(urlPath, localPath, denyAccess, file));
        }

        public override void ProgressTask(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));

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
            => true;
    }
}
