using MaxLib.Net.Webserver.Files.Content.Grabber.Info;
using MaxLib.Net.Webserver.Lazy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IO = System.IO;

namespace MaxLib.Net.Webserver.Files.Source
{
    public class SourceProviderFactory
    {
        public SimpleIOClass SimpleIO(string[] pathRoot)
        {
            return new SimpleIOClass(pathRoot);
        }

        public ZipClass Zip(string[] pathRoot)
        {
            return new ZipClass(pathRoot);
        }

        public class SimpleIOClass : SourceProvider
        {
            readonly Dictionary<string, Token> TempTokens = new Dictionary<string, Token>();
            readonly Dictionary<string, Token> HandleTokens = new Dictionary<string, Token>();
            readonly HashSet<string> GeneratingTokens = new HashSet<String>();

            public SimpleIOClass(string[] pathRoot) : base(pathRoot)
            {
            }

            string GetId()
            {
                var r = new Random();
                var b = new byte[16];
                while (true)
                {
                    r.NextBytes(b);
                    var k = ToBase32(b);

                    if (!TempTokens.ContainsKey(k) && !GeneratingTokens.Contains(k))
                        return k;
                }
            }

            static string ToBase32(byte[] bytes)
            {
                const string alphabet = "0123456789abcdefghjkmnpqrstvwxyz";
                var sb = new StringBuilder(bytes.Length * 5 / 2);
                var ind = 0;
                int num = 0;
                int bits = 0;
                while (ind < bytes.Length)
                {
                    int next = bytes[ind];
                    ind++;
                    num |= next << bits;
                    bits += 8;
                    while (bits >= 5)
                    {
                        var digit = num & 0x1f;
                        num >>= 5;
                        bits -= 5;
                        sb.Append(alphabet[digit]);
                    }
                }
                if (bits > 0)
                {
                    var digit = num & 0x1f;
                    sb.Append(alphabet[digit]);
                }
                return sb.ToString();
            }

            public override RessourceToken CreateTempRessource(string ressourceHandle)
            {
                if (HandleTokens.TryGetValue(ressourceHandle, out Token t))
                    return t;
                string key;
                do key = GetId();
                while (!GeneratingTokens.Add(key));

                if (!IO.Directory.Exists("Temp\\ContentProvider"))
                    IO.Directory.CreateDirectory("Temp\\ContentProvider");

                var token = new Token(key, "Temp\\ContentProvider\\" + key + ".temp", ressourceHandle);
                token.OnDiscard += (s, e) =>
                {
                    HandleTokens.Remove(ressourceHandle);
                    if (IO.File.Exists(token.LocalPath))
                        IO.File.Delete(token.LocalPath);
                    GeneratingTokens.Remove(key);
                };
                token.OnNotify += (s, e) =>
                {
                    TempTokens.Add(key, token);
                    GeneratingTokens.Remove(key);
                    token.MakeReady(null, GetPath(key));
                    if (IO.File.Exists(token.LocalPath))
                    {
                        var info = new IO.FileInfo(token.LocalPath);
                        info.Attributes |= IO.FileAttributes.Temporary;
                    }
                };
                HandleTokens.Add(ressourceHandle, token);
                return token;
            }

            public override string NotifyRessource(string localPath)
            {
                var r = Contents.ReverseSearch(localPath).Where((ci) => ci.Item1.Type == ContentType.File).ToArray();
                if (r.Length == 0) return null;
                var pb = r[0].Item2;
                return GetPath(pb);
            }

            public override string NotifyRessource(string localPath, ContentInfo content)
            {
                var r = Contents.ReverseSearch(localPath).Where((ci) => ci.Item1.Type == ContentType.File).ToList();
                if (r.Count == 0) return null;
                var ind = r.FindIndex((t) => t.Item1 == content);
                if (ind == -1) return GetPath(r[0].Item2);
                else return GetPath(r[0].Item2) + "/@" + ind.ToString();
            }

            protected override bool GetRessource(string id, WebProgressTask task)
            {
                if (TempTokens.TryGetValue(id, out Token token))
                {
                    task.Document.DataSources.Add(new HttpFileDataSource(token.LocalPath)
                    {
                        MimeType = token.Mime,
                        TransferCompleteData = true
                    });
                    task.Document.ResponseHeader.HeaderParameter["Expires"] =
                        DateTime.Now.AddDays(7).ToString("r");
                    task.Document.ResponseHeader.HeaderParameter["Cache-Control"] =
                        "public, max-age=604800";
                    return true;
                }
                else return false;
            }

            protected override bool GetRessource(byte[] hash, WebProgressTask task)
            {
                return false;
            }

            protected override bool GetRessource(string[] path, WebProgressTask task)
            {
                if (Contents == null) return false;
                int ind = 0;
                if (path.Length > 0 && path[path.Length - 1].StartsWith("@"))
                {
                    var k = path[path.Length - 1];
                    if (!int.TryParse(k.Length > 1 ? k.Substring(1) : "", out ind))
                        ind = 0;
                    var npath = new string[path.Length - 1];
                    Array.Copy(path, npath, npath.Length);
                    path = npath;
                }
                var r = Contents.GetContents(path, task).Where((ci) => ci.Type == ContentType.File).ToArray();
                if (r.Length == 0 || ind < 0 || ind >= r.Length) return false;
                task.Document.DataSources.Add(new HttpStreamDataSource((r[ind] as FileInfo).GetStream())
                {
                    MimeType = (r[ind] as FileInfo).MimeType,
                    TransferCompleteData = true,
                });
                return true;
            }

            public override void Dispose()
            {
                foreach (var t in TempTokens)
                {
                    if (IO.File.Exists(t.Value.LocalPath))
                        IO.File.Delete(t.Value.LocalPath);
                }
                TempTokens.Clear();
                HandleTokens.Clear();
                GeneratingTokens.Clear();
            }

            class Token : RessourceToken
            {
                public event EventHandler OnDiscard, OnNotify;

                public override void Discard()
                {
                    if (ContentReady) throw new InvalidOperationException("content is ready");
                    if (LocalPath == null) throw new InvalidOperationException("already discarded");
                    LocalPath = null;
                    OnDiscard?.Invoke(this, EventArgs.Empty);
                }

                public override void NotifyContentReady()
                {
                    if (ContentReady) throw new InvalidOperationException("content is ready");
                    ContentReady = true;
                    OnNotify?.Invoke(this, EventArgs.Empty);
                }

                public override void SetMime(string mime)
                {
                    if (ContentReady) throw new InvalidOperationException("content is ready");
                    Mime = mime ?? throw new ArgumentNullException("mime");
                }

                public Token(string id, string localPath, string ressourceHandle)
                {
                    Id = id;
                    Hash = null;
                    Mime = null;
                    LocalPath = localPath;
                    Url = null;
                    ContentReady = false;
                    RessourceHandle = ressourceHandle;
                }

                public void MakeReady(byte[] hash, string url)
                {
                    Hash = hash;
                    Url = url;
                }
            }
        }

        public class ZipClass : SourceProvider
        {
            public ZipClass(string[] pathRoot) : base(pathRoot)
            {
            }

            public override RessourceToken CreateTempRessource(string ressourceHandle)
            {
                throw new NotSupportedException();
            }

            public override string NotifyRessource(string localPath)
            {
                var r = Contents.ReverseSearch(localPath).Where((ci) => ci.Item1.Type == ContentType.File).ToArray();
                if (r.Length == 0) return null;
                var pb = r[0].Item2;
                return GetPath(pb);
            }

            public override string NotifyRessource(string localPath, ContentInfo content)
            {
                var r = Contents.ReverseSearch(localPath).Where((ci) => ci.Item1.Type == ContentType.File).ToList();
                if (r.Count == 0) return null;
                var ind = r.FindIndex((t) => t.Item1 == content);
                if (ind == -1) return GetPath(r[0].Item2);
                else return GetPath(r[0].Item2) + "/@" + ind.ToString();
            }

            protected override bool GetRessource(string id, WebProgressTask task)
            {
                return false;
            }

            protected override bool GetRessource(byte[] hash, WebProgressTask task)
            {
                return false;
            }

            protected override bool GetRessource(string[] path, WebProgressTask task)
            {
                if (Contents == null) return false;
                int ind = -1;
                if (path.Length > 0 && path[path.Length - 1].StartsWith("@"))
                {
                    var k = path[path.Length - 1];
                    if (!int.TryParse(k.Length > 1 ? k.Substring(1) : "", out ind))
                        ind = 0;
                    var npath = new string[path.Length - 1];
                    Array.Copy(path, npath, npath.Length);
                    path = npath;
                }
                var r = Contents.GetContents(path, task).ToArray();
                task.Document.ResponseHeader.HeaderParameter["Content-Disposition"] =
                    "attachment; filename=\"" +
                    (path.Length > 0 ? path[path.Length - 1] : "files") + ".zip\"";
                task.Document.DataSources.Add(new LazySource(task, (t) => LazyCore(InitZip(r, path, ind, task)))
                {
                    MimeType = MimeTypes.ApplicationZip
                });
                return true;
            }

            Data.ZipStream.LazyZipTaskHandler InitZip(ContentInfo[] r, string[] path, int ind, WebProgressTask task)
            {
                return (zt) =>
                {
                    if (r.Length == 0 || ind < 0 || ind >= r.Length)
                        foreach (var c in r)
                            Analyse("", path, c, task, zt);
                    else Analyse("", path, r[ind], task, zt);
                    zt.AddFinish("created at " + DateTime.Now.ToString("s"));
                };
            }

            IEnumerable<HttpDataSource> LazyCore(Data.ZipStream.LazyZipTaskHandler init)
            {
                using (var zip = new Data.ZipStream.LazyZipStreamCreator(init))
                {
                    foreach (var stream in zip.Execute())
                    {
                        yield return new HttpChunkedStream(stream);
                    }
                }
            }

            string[] Add(string[] root, string part)
            {
                var l = new List<string>(root.Length + 1);
                l.AddRange(root);
                l.Add(part);
                return l.ToArray();
            }

            void Discover(string root, string[] path, WebProgressTask wt, Data.ZipStream.LazyZipTask task)
            {
                foreach (var content in Contents.GetContents(path, wt))
                {
                    Analyse(root, path, content, wt, task);
                }
            }

            void Analyse(string root, string[] path, ContentInfo content,
                WebProgressTask wt, Data.ZipStream.LazyZipTask task)
            {
                content.LoadContents(wt);
                var name = root + content.Name;
                if (content.Type == ContentType.Directory)
                {
                    name += "/";
                    if (((DirectoryInfo)content).Contents.Length == 0)
                    {
                        AddSingleContent(name, path, content, task);
                    }
                    foreach (var c in ((DirectoryInfo)content).Contents)
                        Discover(name, Add(path, c.Name), wt, task);
                }
                if (content.Type == ContentType.File)
                {
                    AddSingleContent(name, path, content, task);
                }
            }

            void AddSingleContent(string name, string[] path, ContentInfo info, Data.ZipStream.LazyZipTask task)
            {
                IO.Stream stream = null;
                switch (info.Type)
                {
                    case ContentType.Directory:
                        stream = new IO.MemoryStream();
                        break;
                    case ContentType.File:
                        stream = ((FileInfo)info).GetStream();
                        break;
                }
                task.AddTask((t) => t.AddFile(name, stream, info.Modified, GetPath(path)));
            }
        }
    }
}
