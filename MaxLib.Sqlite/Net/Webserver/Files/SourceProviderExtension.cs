using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO = System.IO;
using MaxLib.DB;
using MaxLib.Collections;
using MaxLib.Net.Webserver.Files.Source;
using MaxLib.Net.Webserver.Files.Content.Grabber.Info;

namespace MaxLib.Net.Webserver.Files
{
    public static class SourceProviderExtension
    {
#pragma warning disable IDE0060 // Nicht verwendete Parameter entfernen
        public static BufferedTokensClass BufferedTokens(this SourceProviderFactory factory, string[] pathRoot, string db = "temp\\io-tokens.db")
#pragma warning restore IDE0060 // Nicht verwendete Parameter entfernen
        {
            return new BufferedTokensClass(pathRoot, db);
        }

        public class BufferedTokensClass : SourceProvider
        {
            readonly Dictionary<string, Token> TempTokens = new Dictionary<string, Token>();
            readonly Dictionary<string, Token> HandleTokens = new Dictionary<string, Token>();
            readonly HashSet<string> GeneratingTokens = new HashSet<String>();
            readonly ByteTree<Token> HashTree = new ByteTree<Token>();
            readonly Database db;
            readonly DbFactory fact;

            public BufferedTokensClass(string[] pathRoot, string db) : base(pathRoot)
            {
                this.db = new Database(db, Properties.Resources.BufferedTokensCheckDb, Properties.Resources.BufferedTokensCreateDb);
                fact = new DbFactory(this.db);
                var remove = new List<DbToken>();
                var update = new List<DbToken>();
                foreach (var token in fact.LoadAll<DbToken>())
                {
                    var t = new Token(token.Id, token.LocalPath, token.RessourceHandle);
                    t.Fetch(token);
                    TokenEvents(t);
                    if (token.RessourceHandle != null)
                        HandleTokens.Add(token.RessourceHandle, t);
                    if (token.Hash != null)
                    {
                        if (!IO.File.Exists(token.LocalPath))
                        {
                            if (token.Id == null)
                            {
                                remove.Add(token);
                            }
                            else
                            {
                                token.Hash = null;
                                update.Add(token);
                            }
                        }
                        else HashTree.Set(token.Hash, t);
                    }
                    if (token.Id != null)
                        TempTokens.Add(token.Id, t);
                }
                using (var t = this.db.Transaction())
                using (var q = this.db.Create("DELETE FROM BufferedTokens WHERE Hash=?"))
                {
                    foreach (var r in remove)
                    {
                        q.SetValues(r.Hash);
                        q.ExecuteNonQuery();
                    }
                    foreach (var u in update)
                        fact.Update(u);
                }
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
                TokenEvents(token);
                HandleTokens.Add(ressourceHandle, token);
                return token;
            }

            void TokenEvents(Token token)
            {
                var key = token.Id;
                var ressourceHandle = token.RessourceHandle;
                token.OnDiscard += (s, e) =>
                {
                    if (token.Hash == null)
                    {
                        HandleTokens.Remove(ressourceHandle);
                        if (IO.File.Exists(token.LocalPath))
                            IO.File.Delete(token.LocalPath);
                        GeneratingTokens.Remove(key);
                        fact.Delete<DbToken>(new DbValue("Id", key));
                    }
                    else
                    {
                        token.DiscardHandle();
                        HandleTokens.Remove(ressourceHandle);
                        GeneratingTokens.Remove(key);
                        using (var q = db.Create("UPDATE BufferedTokens SET Id=NULL, RessourceHandle=NULL WHERE Hash=?"))
                        {
                            q.SetValues(token.Hash);
                            q.ExecuteNonQuery();
                        }
                    }
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
                    var dbt = new DbToken(token);
                    if (fact.Count<DbToken>(new DbValue("Id", key)) > 0)
                        fact.Update(dbt);
                    else fact.Add(new DbToken(token));
                    CalculateHash(token);
                };
            }

            void CalculateHash(Token token)
            {
                if (!IO.File.Exists(token.LocalPath))
                {
                    token.MakeReady(null, token.Url);
                }
                else Task.Run(() =>
                {
                    using (var f = new IO.FileStream(token.LocalPath, IO.FileMode.Open, IO.FileAccess.Read))
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    {
                        var hash = md5.ComputeHash(f);
                        HashTree.Set(hash, token);
                        token.MakeReady(hash, GetPath(hash));
                        fact.Update(new DbToken(token));
                    }
                });
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
                    if (token.Hash != null)
                    {
                        task.Document.ResponseHeader.HeaderParameter["X-Hash-MD5"] =
                            Convert.ToBase64String(token.Hash);
                        task.Document.ResponseHeader.HeaderParameter["X-Hash-Path"] =
                            GetPath(token.Hash);
                    }
                    return true;
                }
                else return false;
            }

            protected override bool GetRessource(byte[] hash, WebProgressTask task)
            {
                if (HashTree.TryGet(hash, out Token token))
                {
                    task.Document.DataSources.Add(new HttpFileDataSource(token.LocalPath)
                    {
                        MimeType = token.Mime,
                        TransferCompleteData = true
                    });
                    task.Document.ResponseHeader.HeaderParameter["Expires"] =
                        DateTime.Now.AddDays(365).ToString("r");
                    task.Document.ResponseHeader.HeaderParameter["Cache-Control"] =
                        "public, max-age=31536000";
                    if (token.Id != null)
                    {
                        task.Document.ResponseHeader.HeaderParameter["X-Id-Key"] =
                            token.Id;
                        task.Document.ResponseHeader.HeaderParameter["X-Id-Path"] =
                            GetPath(token.Id);
                    }
                    return true;
                }
                else return false;
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
                task.Document.DataSources.Add(new MultipartRanges(
                    (r[ind] as FileInfo).GetStream(),
                    task.Document,
                    (r[ind] as FileInfo).MimeType)
                {
                    TransferCompleteData = true
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
                fact.Dispose();
                db.Dispose();
            }

            [DbClass("BufferedTokens")]
            class DbToken
            {
                public DbToken() { }

                public DbToken(Token token)
                {
                    Id = token.Id;
                    Hash = token.Hash;
                    Mime = token.Mime;
                    LocalPath = token.LocalPath;
                    Url = token.Url;
                    RessourceHandle = token.RessourceHandle;
                }

                [DbProp(primaryKey:true)]
                public string Id { get; set; }

                [DbProp]
                public byte[] Hash { get; set; }

                [DbProp]
                public string Mime { get; set; }

                [DbProp]
                public string LocalPath { get; set; }

                [DbProp]
                public string Url { get; set; }

                [DbProp]
                public string RessourceHandle { get; set; }
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

                public void Fetch(DbToken token)
                {
                    ContentReady = true;
                    Hash = token.Hash;
                    Id = token.Id;
                    LocalPath = token.LocalPath;
                    Mime = token.Mime;
                    RessourceHandle = token.RessourceHandle;
                    Url = token.Url;
                }

                public void DiscardHandle()
                {
                    Id = null;
                    RessourceHandle = null;
                }
            }
        }
    }
}
