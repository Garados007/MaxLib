using MaxLib.Net.Webserver.Files.Content.Grabber.Icons;
using MaxLib.Net.Webserver.Files.Content.Grabber.Info;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Net.Webserver.Files.Content.Grabber.Source
{
    public class VirtualContentSource : ContentSource
    {
        class VirtualDictInfo
        {
            public Dictionary<string, VirtualDictInfo> Dict = new Dictionary<string, VirtualDictInfo>();

            public string Name;

            public DateTime Created = DateTime.Now, Modified, Access;

            public VirtualDictInfo Parent;
        }

        class MainDirectory : DirectoryInfo
        {
            public string name;
            public VirtualDictInfo[] subs;
            public DateTime created, modified, access;

            ContentInfo[] contents = null;
            public override ContentInfo[] Contents => contents;

            public override string Name => name;

            public override bool Exists => true;

            public override DateTime Created => created;

            public override DateTime Modified => modified;

            public override DateTime Access => access;

            public override string LocalPath => null;

            public override void LoadContents(WebProgressTask task)
            {
                contents = subs.Select((s) => new SubDirectory
                {
                    access = s.Access,
                    modified = s.Modified,
                    created = s.Created,
                    name = s.Name,
                    Icon = new IconInfo
                    {
                        ContentId = null,
                        Type = IconInfo.ContentIdType.DetectDirectory
                    }
                }).ToArray();
            }

        }

        class SubDirectory : DirectoryInfo
        {
            public string name;
            public DateTime created, modified, access;

            public override ContentInfo[] Contents => null;

            public override string Name => name;

            public override bool Exists => true;

            public override DateTime Created => created;

            public override DateTime Modified => modified;

            public override DateTime Access => access;

            public override string LocalPath => null;

            public override void LoadContents(WebProgressTask task)
            {
            }
        }

        public bool OnlyLeaves { get; private set; }

        public bool UseDictionary { get; private set; }

        readonly Dictionary<string, VirtualDictInfo> dict;
        readonly HashSet<string[]> list;

        public VirtualContentSource Add(params string[] path)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentOutOfRangeException("path", "path doesn't contains any elements");
            var now = DateTime.Now;
            if (UseDictionary)
            {
                var dict = this.dict;
                VirtualDictInfo parent = null;
                for (int i = 0; i < path.Length; ++i)
                {
                    if (dict.ContainsKey(path[i]))
                        dict = (parent = dict[path[i]]).Dict;
                    else
                    {
                        var vd = new VirtualDictInfo
                        {
                            Name = path[i],
                            Created = now,
                            Modified = now,
                            Parent = parent,
                        };
                        var p = parent;
                        while (p != null)
                        {
                            p.Modified = now;
                            p = p.Parent;
                        }
                        this.modified = now;
                        dict.Add(path[i], vd);
                        parent = vd;
                        dict = vd.Dict;
                    }
                }
            }
            else
            {
                if (list.Add(path))
                    this.modified = now;
            }
            return this;
        }

        public VirtualContentSource Remove(params string[] path)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentOutOfRangeException("path", "path doesn't contains any elements");
            var now = DateTime.Now;
            if (UseDictionary)
            {
                var dict = this.dict;
                Dictionary<string, VirtualDictInfo> parent = dict;
                VirtualDictInfo pdi = null;
                int i;
                for (i = 0; i < path.Length; ++i)
                {
                    parent = dict;
                    if (dict.ContainsKey(path[i]))
                        dict = (pdi = dict[path[i]]).Dict;
                    else break;
                }
                if (i >= path.Length)
                {
                    parent.Remove(path[path.Length - 1]);
                    var p = pdi;
                    while (p != null)
                    {
                        p.Modified = now;
                        p = p.Parent;
                    }
                    this.modified = now;
                }
            }
            else
            {
                if (list.Remove(path))
                    this.modified = now;
            }
            return this;
        }

        public override string[] RootUrl { get; }

        private DateTime access;
        private readonly DateTime created = DateTime.Now;
        private DateTime modified;

        public override bool Strict { get; }

        public override void Dispose()
        {
        }

        public override Tuple<ContentInfo, string[]> ReverseSearch(string localPath)
        {
            return null;
        }

        public override ContentInfo TryGetContent(string[] relativePath, WebProgressTask task)
        {
            var result = GetChilds(relativePath).ToArray();
            Split(result, out VirtualDictInfo first, out VirtualDictInfo[] childs);
            if (childs.Length == 0) return null;
            var now = DateTime.Now;
            if (UseDictionary)
            {
                if (first != null)
                    first.Access = now;
                foreach (var c in childs)
                    c.Access = now;
            }
            this.access = now;
            var md = new MainDirectory
            {
                access = first?.Access ?? this.access,
                created = first?.Created ?? this.created,
                modified = first?.Modified ?? this.modified,
                name = first?.Name ?? "Virtual Root",
                subs = childs,
                Icon = new IconInfo
                {
                    ContentId = null,
                    Type = IconInfo.ContentIdType.DetectDirectory
                }
            };
            md.LoadContents(task);
            return md;
        }

        IEnumerable<VirtualDictInfo> GetChilds(string[] path)
        {
            if (UseDictionary)
            {
                var d = dict;
                VirtualDictInfo parent = null;
                for (int i = 0; i < path.Length; ++i)
                    if (!dict.ContainsKey(path[i]))
                        yield break;
                    else d = (parent = d[path[i]]).Dict;
                yield return parent;
                foreach (var e in d)
                    if (!OnlyLeaves || e.Value.Dict.Count == 0)
                        yield return e.Value;
            }
            else
            {
                yield return new VirtualDictInfo()
                {
                    Name = path.Length == 0 ? "" : path[path.Length - 1],
                    Created = this.created,
                    Access = this.access,
                    Modified = this.modified,
                };
                foreach (var p in list)
                {
                    if (OnlyLeaves ?
                        p.Length != path.Length + 1 :
                        p.Length <= path.Length) continue;
                    var found = true;
                    for (int i = 0; i < path.Length; ++i)
                        if (p[i] != path[i])
                        {
                            found = false;
                            break;
                        }
                    if (!found) continue;
                    yield return new VirtualDictInfo
                    {
                        Name = p[path.Length],
                        Created = this.created,
                        Access = this.access,
                        Modified = this.modified,
                    };

                }
            }
        }

        void Split<T>(IEnumerable<T> e, out T first, out T[] rest)
        {
            first = default;
            foreach (var f in e)
            {
                first = f;
                break;
            }
            rest = e.Skip(1).ToArray();
        }

        public VirtualContentSource(string[] rootUrl = null, bool strict = false,
            bool onlyLeaves = false, bool useDictionary = true)
        {
            RootUrl = rootUrl ?? new string[0];
            Strict = strict;
            OnlyLeaves = onlyLeaves;
            UseDictionary = useDictionary;
            if (UseDictionary) dict = new Dictionary<string, VirtualDictInfo>();
            else list = new HashSet<string[]>();
        }
    }

}
