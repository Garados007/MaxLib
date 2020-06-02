using MaxLib.Collections;
using MaxLib.Net.Webserver.Lazy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IO = System.IO;

namespace MaxLib.Net.Webserver.Files
{
    public abstract class FileSystemService : WebService, IDisposable
    {
        public string[] PathRoot { get; private set; }

        public ContentEnvironment Contents { get; set; }

        public FileSystemService(string[] pathRoot) : base(WebServiceType.PreCreateDocument)
        {
            PathRoot = pathRoot ?? throw new ArgumentNullException("pathRoot");
        }

        public virtual void Dispose()
        {
            Contents?.Dispose();
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            return task.Document.RequestHeader.Location.StartsUrlWith(PathRoot);
        }

        public abstract bool CanDeliverySourceUrlForPath { get; }

        public abstract bool CanDeliverySourceUrlForContent { get; }

        public abstract string GetDeliverySourceUrl(string[] relativePath, ContentInfo content);
    }

    #region Content Provider

    public class ContentProvider : FileSystemService
    {
        public SourceProvider SourceProvider { get; set; }

        public ContentViewer ContentViewer { get; set; }

        public IconFetcher IconFetcher { get; set; }

        public override bool CanDeliverySourceUrlForContent => true;

        public override bool CanDeliverySourceUrlForPath => true;

        public override string GetDeliverySourceUrl(string[] relativePath, ContentInfo content)
        {
            var sb = new StringBuilder();
            sb.Append("/");
            sb.Append(string.Join("/", PathRoot));
            sb.Append("/");
            sb.Append(string.Join("/", relativePath));
            if (content != null)
            {
                sb.Append("/");
                sb.Append(content.Name);
            }
            return sb.ToString();
        }

        public ContentProvider(string[] pathRoot) : base(pathRoot)
        {
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            return base.CanWorkWith(task) && Contents != null &&
                SourceProvider != null && ContentViewer != null;
        }

        public override void ProgressTask(WebProgressTask task)
        {
            var tp = task.Document.RequestHeader.Location.DocumentPathTiles;
            var rp = new string[tp.Length - PathRoot.Length];
            Array.Copy(tp, PathRoot.Length, rp, 0, rp.Length);
            var content = Contents.GetContents(rp, task).ToArray();
            if (IconFetcher != null)
                foreach (var c in content)
                    LoadIcons(c, task);
            var result = new ContentResult()
            {
                CurrentDir = rp,
                CurrentUrl = "/" + 
                    string.Join("/", tp.Select((p) => WebServerHelper.EncodeUri(p)).ToArray()),
                DirRoot = "/" + string.Join("/",
                    PathRoot.Select((p) => WebServerHelper.EncodeUri(p)).ToArray()) + "/",
                FirstRessourceName = content.Length > 0 ? content[0].Name : null,
                Infos = content,
                ParentUrl = "/" + 
                    string.Join("/", tp.Select((p) => WebServerHelper.EncodeUri(p)).ToArray(),
                        0, rp.Length > 0 ? tp.Length - 1 : tp.Length),
                UrlName = rp.Length == 0 ? "" : rp[rp.Length - 1]
            };
            ContentViewer.Show(result, task, SourceProvider);
        }

        void LoadIcons(ContentInfo content, WebProgressTask task)
        {
            IconFetcher.TryGetIcon(content, SourceProvider, task);
            if (content.Type == ContentType.Directory &&
                (content as DirectoryInfo).Contents != null)
                foreach (var c in (content as DirectoryInfo).Contents)
                    LoadIcons(c, task);
        }
    }

    #region Content Grabber

    public class ContentEnvironment : IDisposable
    {
        public List<ContentSource> Sources { get; private set; }

        public void Dispose()
        {
            Sources.ForEach((e) => e.Dispose());
        }

        bool Start(string[] needle, string[] haystack)
        {
            if (needle.Length > haystack.Length) return false;
            for (int i = 0; i < needle.Length; ++i)
                if (needle[i] != haystack[i])
                    return false;
            return true;
        }

        public ContentEnvironment()
        {
            Sources = new List<ContentSource>();
        }

        IEnumerable<ContentSource> GetSources(string[] url)
        {
            foreach (var s in Sources)
                if (Start(s.RootUrl, url))
                    yield return s;
        }

        IEnumerable<ContentSource> FilteredSources(string[] url)
        {
            var sources = GetSources(url).ToArray();
            if (sources.Any((s) => s.Strict))
            {
                var max = sources.Max((s) => s.Strict ? s.RootUrl.Length : 0);
                foreach (var s in sources)
                    if (s.RootUrl.Length >= max)
                        yield return s;
            }
            else foreach (var s in sources) yield return s;
        }

        public IEnumerable<ContentInfo> GetContents(string[] url, WebProgressTask task)
        {
            foreach (var s in FilteredSources(url))
            {
                var rp = new string[url.Length - s.RootUrl.Length];
                Array.Copy(url, s.RootUrl.Length, rp, 0, rp.Length);
                var c = s.TryGetContent(rp, task);
                if (c != null) yield return c;
            }
        }

        IEnumerable<Tuple<ContentSource, Tuple<ContentInfo, string[]>>> GetReverse(string localPath)
        {
            foreach (var s in Sources)
            {
                var c = s.ReverseSearch(localPath);
                if (c != null) yield return new Tuple<ContentSource, Tuple<ContentInfo, string[]>>(s, c);
            }
        }

        IEnumerable<Tuple<ContentSource, Tuple<ContentInfo, string[]>>> FilteredReverse(string localPath)
        {
            var sources = GetReverse(localPath).ToArray();
            if (sources.Any((s) => s.Item1.Strict))
            {
                var max = sources.Max((s) => s.Item1.Strict ? s.Item1.RootUrl.Length : 0);
                foreach (var s in sources)
                    if (s.Item1.RootUrl.Length >= max)
                        yield return s;
            }
            else foreach (var s in sources) yield return s;
        }

        public IEnumerable<Tuple<ContentInfo, string[]>> ReverseSearch(string localPath)
        {
            foreach (var s in FilteredReverse(localPath))
                yield return s.Item2;
        }
    }

    #region ContentSource

    public abstract class ContentSource : IDisposable
    {
        public abstract string[] RootUrl { get; }

        public abstract bool Strict { get; } //enable merge content or not

        public abstract void Dispose();

        public abstract ContentInfo TryGetContent(string[] relativePath, WebProgressTask task);

        public abstract Tuple<ContentInfo, string[]> ReverseSearch(string localPath);
    }

    public class IOContentSource : ContentSource
    {
        readonly string[] rootUrl;
        readonly bool strict;

        public override string[] RootUrl => rootUrl;

        public string LocalRoot { get; }

        public override bool Strict => strict;

        public override void Dispose()
        {
        }

        public override ContentInfo TryGetContent(string[] relativePath, WebProgressTask task)
        {
            var local = LocalRoot + "\\" + string.Join("\\", relativePath);
            if (IO.Directory.Exists(local))
            {
                var di = new IODirectoryInfo(new IO.DirectoryInfo(local));
                if (di.Directory.Attributes.HasFlag(IO.FileAttributes.Hidden) ||
                    di.Directory.Attributes.HasFlag(IO.FileAttributes.System)) return null;
                di.LoadContents(task);
                return di;
            }
            if (IO.File.Exists(local))
            {
                var fi = new IOFileInfo(new IO.FileInfo(local));
                if (fi.File.Attributes.HasFlag(IO.FileAttributes.Hidden) ||
                    fi.File.Attributes.HasFlag(IO.FileAttributes.System)) return null;
                fi.LoadContents(task);
                return fi;
            }
            return null;
        }

        public override Tuple<ContentInfo, string[]> ReverseSearch(string localPath)
        {
            if (localPath.StartsWith(LocalRoot))
            {
                var rpath = localPath.Length != LocalRoot.Length ? localPath.Substring(LocalRoot.Length) : "";
                var path = rpath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var urlpath = new string[path.Length + rootUrl.Length];
                Array.Copy(rootUrl, urlpath, rootUrl.Length);
                Array.Copy(path, 0, urlpath, rootUrl.Length, path.Length);
                if (IO.Directory.Exists(localPath))
                    return new Tuple<ContentInfo, string[]>(new IODirectoryInfo(new IO.DirectoryInfo(localPath)), urlpath);
                if (IO.File.Exists(localPath))
                    return new Tuple<ContentInfo, string[]>(new IOFileInfo(new IO.FileInfo(localPath)), urlpath);
                return null;
            }
            else return null;
        }

        public IOContentSource(string[] rootUrl, string localRoot, bool strict)
        {
            this.rootUrl = rootUrl ?? throw new ArgumentNullException("rootUrl");
            this.LocalRoot = localRoot ?? throw new ArgumentNullException("localRoot");
            if (!IO.Directory.Exists(localRoot)) throw new IO.DirectoryNotFoundException("localRoot not found");
            this.strict = strict;
        }
    }

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

    #endregion

    #region ContentInfo

    public abstract class ContentInfo
    {
        public abstract ContentType Type { get; }

        public abstract string Name { get; }

        public abstract bool Exists { get; }

        public abstract DateTime Created { get; }

        public abstract DateTime Modified { get; }

        public abstract DateTime Access { get; }

        public IconInfo Icon { get; set; }

        public abstract string LocalPath { get; }

        public abstract void LoadContents(WebProgressTask task);

        public ContentInfo()
        {
            Icon = new IconInfo();
        }
    }

    public enum ContentType
    {
        Directory,
        File
    }

    public abstract class DirectoryInfo : ContentInfo
    {
        public override ContentType Type => ContentType.Directory;

        public abstract ContentInfo[] Contents { get; }

        public DirectoryInfo()
        {
            Icon.Type = IconInfo.ContentIdType.DetectDirectory;
        }
    }

    public abstract class FileInfo : ContentInfo
    {
        public override ContentType Type => ContentType.File;

        public abstract long Length { get; }

        public string MimeType { get; protected set; }

        public abstract string Extension { get; } //with dot

        public abstract IO.Stream GetStream();

        public FileInfo()
        {
            Icon.Type = IconInfo.ContentIdType.DetectFile;
            MimeType = MimeTypes.ApplicationOctetStream;
        }
    }

    #region System.IO Implementation (direct Access)

    public class IODirectoryInfo : DirectoryInfo
    {
        public IO.DirectoryInfo Directory { get; private set; }

        private ContentInfo[] contents = null;
        public override ContentInfo[] Contents => contents;

        public override string Name => Directory.Name;

        public override bool Exists => Directory.Exists;

        public override DateTime Created => Directory.CreationTime;

        public override DateTime Modified => Directory.LastWriteTime;

        public override DateTime Access => Directory.LastAccessTime;

        public override string LocalPath => Directory.FullName;

        public override void LoadContents(WebProgressTask task)
        {
            if (contents != null) return;
            var l = new List<ContentInfo>();
            foreach (var d in Directory.EnumerateDirectories())
                if (!d.Attributes.HasFlag(IO.FileAttributes.Hidden) && !d.Attributes.HasFlag(IO.FileAttributes.System))
                    l.Add(new IODirectoryInfo(d));
            foreach (var f in Directory.EnumerateFiles())
                if (!f.Attributes.HasFlag(IO.FileAttributes.Hidden) && !f.Attributes.HasFlag(IO.FileAttributes.System))
                {
                    var fi = new IOFileInfo(f);
                    l.Add(fi);
                    fi.LoadContents(task);
                }
            contents = l.ToArray();
        }

        public IODirectoryInfo(IO.DirectoryInfo directory)
        {
            Directory = directory ?? throw new ArgumentNullException("directory");
        }

        public static bool operator ==(IODirectoryInfo d1, IODirectoryInfo d2)
        {
            if (d1 is null && d2 is null) return true;
            if (d1 is null || d2 is null) return false;
            return d1.Directory.FullName == d2.Directory.FullName;
        }

        public static bool operator !=(IODirectoryInfo d1, IODirectoryInfo d2)
        {
            return !(d1 == d2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is IODirectoryInfo)) return false;
            return Directory.FullName == (obj as IODirectoryInfo).Directory.FullName;
        }

        public override int GetHashCode()
        {
            return Directory.GetHashCode();
        }
    }

    public class IOFileInfo : FileInfo
    {
        public IO.FileInfo File { get; private set; }

        public override long Length => File.Length;

        public override string Name => File.Name;

        public override bool Exists => File.Exists;

        public override DateTime Created => File.CreationTime;

        public override DateTime Modified => File.LastWriteTime;

        public override DateTime Access => File.LastAccessTime;

        public override string LocalPath => File.FullName;

        public override string Extension => File.Extension;

        public IOFileInfo(IO.FileInfo file)
        {
            File = file ?? throw new ArgumentNullException("file");
        }

        public override void LoadContents(WebProgressTask task)
        {
            if (Extension != null &&
                task.Server.Settings.DefaultFileMimeAssociation.TryGetValue(Extension.ToLower(), out string mime))
                MimeType = mime;
        }

        public override IO.Stream GetStream()
        {
            return File.OpenRead();
        }

        public static bool operator ==(IOFileInfo f1, IOFileInfo f2)
        {
            if (f1 is null && f2 is null) return true;
            if (f1 is null || f2 is null) return false;
            return f1.File.FullName == f2.File.FullName;
        }

        public static bool operator !=(IOFileInfo f1, IOFileInfo f2)
        {
            return !(f1 == f2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is IOFileInfo)) return false;
            return File.FullName == (obj as IOFileInfo).File.FullName;
        }

        public override int GetHashCode()
        {
            return File.GetHashCode();
        }
    }

    #endregion

    #endregion

    #region Icons

    public abstract class IconFetcher : IDisposable
    {
        public abstract void Dispose();

        public abstract bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task);

        public static IconFetcher All(params IconFetcher[] iconFetcher)
        {
            if (iconFetcher == null) throw new ArgumentNullException("iconFetcher");
            foreach (var e in iconFetcher) if (e == null) throw new ArgumentNullException("iconFetcher");
            return new AllClass() { List = iconFetcher };
        }

        public static IconFetcher Any(params IconFetcher[] iconFetcher)
        {
            if (iconFetcher == null) throw new ArgumentNullException("iconFetcher");
            foreach (var e in iconFetcher) if (e == null) throw new ArgumentNullException("iconFetcher");
            return new AnyClass() { List = iconFetcher };
        }

        public static IconFetcher While(IconFetcher iconFetcher)
        {
            if (iconFetcher == null) throw new ArgumentNullException("iconFetcher");
            return new WhileClass() { fetcher = iconFetcher };
        }

        public static IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target, 
            Func<ContentInfo, String> newContentId, bool result)
        {
            return new FallbackClass()
            {
                source = source,
                target = target,
                contentId = newContentId ?? throw new ArgumentNullException("newContentId"),
                result = result
            };
        }

        public static IconFactory Factory => new IconFactory();

        class AllClass : IconFetcher
        {
            public IconFetcher[] List;

            public override void Dispose()
            {
                foreach (var e in List) e.Dispose();
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                var iconInfo = contentInfo.Icon;
                var id = iconInfo.ContentId;
                var type = iconInfo.Type;
                foreach (var e in List)
                    if (!e.TryGetIcon(contentInfo, provider, task))
                    {
                        iconInfo.ContentId = id;
                        iconInfo.Type = type;
                        return false;
                    }
                return List.Length != 0;
            }
        }

        class AnyClass : IconFetcher
        {
            public IconFetcher[] List;

            public override void Dispose()
            {
                foreach (var e in List) e.Dispose();
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                var iconInfo = contentInfo.Icon;
                var id = iconInfo.ContentId;
                var type = iconInfo.Type;
                foreach (var e in List)
                    if (e.TryGetIcon(contentInfo, provider, task))
                        return true;
                    else
                    {
                        iconInfo.ContentId = id;
                        iconInfo.Type = type;
                    }
                return false;
            }
        }

        class WhileClass : IconFetcher
        {
            public IconFetcher fetcher;

            public override void Dispose()
            {
                fetcher.Dispose();
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                bool result, any = false;
                do
                {
                    result = fetcher.TryGetIcon(contentInfo, provider, task);
                    any |= result;
                }
                while (result);
                return any;
            }
        }

        class FallbackClass : IconFetcher
        {
            public bool result;
            public IconInfo.ContentIdType source;
            public IconInfo.ContentIdType target;
            public Func<ContentInfo, String> contentId;

            public override void Dispose()
            {
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                if (contentInfo.Icon.Type != source) return result;
                contentInfo.Icon.ContentId = contentId(contentInfo);
                contentInfo.Icon.Type = target;
                return result;
            }
        }
    }

    public class IconFactory
    {
        public IconFetcher All(params IconFetcher[] iconFetcher)
        {
            return IconFetcher.All(iconFetcher);
        }

        public IconFetcher Any(params IconFetcher[] iconFetcher)
        {
            return IconFetcher.Any(iconFetcher);
        }

        public IconFetcher While (IconFetcher iconFetcher)
        {
            return IconFetcher.While(iconFetcher);
        }

        public IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target)
        {
            return IconFetcher.Fallback(source, target, (c) => c.Icon.ContentId, true);
        }

        public IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target,
            Func<ContentInfo, String> newContentId)
        {
            return IconFetcher.Fallback(source, target, newContentId, true);
        }

        public IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target, 
            bool result)
        {
            return IconFetcher.Fallback(source, target, (c) => c.Icon.ContentId, result);
        }

        public IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target,
            Func<ContentInfo, String> newContentId, bool result)
        {
            return IconFetcher.Fallback(source, target, newContentId, result);
        }
    }

    public class IconInfo
    {
        public string ContentId { get; set; }

        public ContentIdType Type { get; set; }

        public IconInfo()
        {
            Type = ContentIdType.None;
        }

        public enum ContentIdType
        {
            None =              0,
            CommonDirectory =   1,
            CommonFile =        2,
            IcoFile =           3,
            IcoInBinFile =      4,
            ImgFile =           5,
            UnknownFile =       6,
            DetectDirectory =   7,
            DetectFile =        8,
            Url =               9,
            /// <summary>
            /// Special Flag for extra states. If you want to define an extra state the 4 lowest bits must be 1.
            /// (collision protection)
            /// </summary>
            Special =           0x0f,
        }
    }

    #endregion

    #endregion

    #region Content Viewer

    public class ContentResult
    {
        public ContentInfo[] Infos { get; set; }

        public string UrlName { get; set; }

        public string FirstRessourceName { get; set; }

        public string CurrentUrl { get; set; }

        public string ParentUrl { get; set; }

        public string[] CurrentDir { get; set; }

        public string DirRoot { get; set; }
    }

    public abstract class ContentViewer
    {
        public static ContentViewerFactory Factory => new ContentViewerFactory();

        protected abstract IEnumerable<HttpDataSource> AddStartSequence(ContentResult info, LazyTask task, SourceProvider source);

        protected abstract IEnumerable<HttpDataSource> NoContent(LazyTask task, SourceProvider source);

        protected abstract IEnumerable<HttpDataSource> WrapAndInsertContent(ContentInfo info, IEnumerable<HttpDataSource> content, LazyTask task, SourceProvider source);

        protected abstract IEnumerable<HttpDataSource> InsertEmptyContent(ContentInfo info, LazyTask task, SourceProvider source);

        protected abstract IEnumerable<HttpDataSource> AddEndSequence(ContentResult info, LazyTask task, SourceProvider source);

        public List<ContentInfoViewer> InfoViewer { get; private set; }

        public virtual void Show(ContentResult info, WebProgressTask task, SourceProvider source)
        {
            var lazy = new LazySource(task, (t) => ShowInternal(info, t, source))
            {
                MimeType = MimeTypes.TextHtml
            };
            task.Document.DataSources.Add(lazy);
        }

        protected virtual IEnumerable<HttpDataSource> ShowInternal(ContentResult info, LazyTask task, SourceProvider source)
        {
            var enb = new EnumeratorBuilder<HttpDataSource>();
            enb.Yield(() => AddStartSequence(info, task, source));
            if (info.Infos.Length == 0)
                enb.Yield(() => NoContent(task, source));
            else foreach (var c in info.Infos)
                    enb.Yield(() =>
                    {
                        var sen = new EnumeratorBuilder<HttpDataSource>();
                        foreach (var v in InfoViewer)
                            sen.Yield(v.ViewContent(info.CurrentUrl, c, task, source));
                        var backup = new EnumeratorBackup<HttpDataSource>(sen,
                            InsertEmptyContent(c, task, source));
                        return WrapAndInsertContent(c, backup, task, source);
                    });
            enb.Yield(() => AddEndSequence(info, task, source));
            return enb;
        }

        public ContentViewer()
        {
            InfoViewer = new List<ContentInfoViewer>();
        }
    }

    public abstract class ContentInfoViewer
    {
        public abstract IEnumerable<HttpDataSource> ViewContent(string path,
            ContentInfo info, LazyTask task, SourceProvider source);
    }

    public class ContentViewerFactory
    {
        static HttpDataSource Stringer(params object[] tiles)
        {
            var sb = new StringBuilder();
            foreach (var t in tiles)
                sb.Append(t);
            return new HttpStringDataSource(sb.ToString())
            {
                MimeType = MimeTypes.TextHtml,
                TransferCompleteData = true
            };
        }

        public ContentViewer SimpleHtml => new SimpleHtmlClass();

        public ContentViewer StyledHtml => new StyledHtmlClass();

        class SimpleHtmlClass : ContentViewer
        {
            protected override IEnumerable<HttpDataSource> AddStartSequence(ContentResult info, LazyTask task, SourceProvider source)
            {
                yield return new HttpStringDataSource(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0 /"">
    <title>" + (info.FirstRessourceName ?? "no ressource") + @"</title>
</head>
<body>
    <p>
        Parent dir: <a href=""" + info.ParentUrl + @""">" + info.ParentUrl + @"</a>
    </p>
    <ul>
")
                {
                    MimeType = MimeTypes.TextHtml,
                    TransferCompleteData = true,

                };
            }

            protected override IEnumerable<HttpDataSource> AddEndSequence(ContentResult info, LazyTask task, SourceProvider source)
            {
                yield return new HttpStringDataSource(@"
    </ul>
</body>
</html>
")
                {
                    MimeType = MimeTypes.TextHtml,
                    TransferCompleteData = true,

                };
            }

            protected override IEnumerable<HttpDataSource> InsertEmptyContent(ContentInfo info, LazyTask task, SourceProvider source)
            {
                yield return Stringer("<li>No visualizer found for ",
                    info.Type, ":", info.Name, "!</li>");
            }

            protected override IEnumerable<HttpDataSource> NoContent(LazyTask task, SourceProvider source)
            {
                yield return Stringer("<li>No content exists in this Directory!</li>");
            }

            protected override IEnumerable<HttpDataSource> WrapAndInsertContent(ContentInfo info, IEnumerable<HttpDataSource> content, LazyTask task, SourceProvider source)
            {
                var enb = new EnumeratorBuilder<HttpDataSource>();
                enb.Yield(Stringer("<li>"));
                enb.Yield(content);
                enb.Yield(Stringer("</li>"));
                return enb;
            }

            class EntryViewer : ContentInfoViewer
            {
                public override IEnumerable<HttpDataSource> ViewContent(string path, ContentInfo info, LazyTask task, SourceProvider source)
                {
                    switch (info.Type)
                    {
                        case ContentType.File:
                            yield return Stringer("<a href=\"", source.NotifyRessource((info as FileInfo).LocalPath),
                                "\">", info.Name, "</a> <b>[",
                                WebServerHelper.GetVolumeString((info as FileInfo).Length, true, 3),
                                "]</b>");
                            break;
                        case ContentType.Directory:
                            yield return Stringer("<a href=\"", path, "\">", info.Name, "</a>");
                            if ((info as DirectoryInfo).Contents != null)
                            {
                                yield return Stringer("<br/><ul>");
                                foreach (var e in (info as DirectoryInfo).Contents)
                                {
                                    yield return Stringer("<li>");
                                    var p = path + "/" + WebServerHelper.EncodeUri(e.Name);
                                    foreach (var c in ViewContent(p, e, task, source))
                                        yield return c;
                                    yield return Stringer("</li>");
                                }
                                yield return Stringer("</ul>");
                            }
                            break;
                        default:
                            yield return Stringer(info.Name);
                            break;
                    }
                }
            }

            public SimpleHtmlClass()
            {
                InfoViewer.Add(new EntryViewer());
            }
        }

        class StyledHtmlClass : ContentViewer
        {
            readonly string cssCode = Properties.Resources.Net_Webserver_Files_ViewerHtmlCss;

            protected override IEnumerable<HttpDataSource> AddStartSequence(ContentResult info,  LazyTask task, SourceProvider source)
            {
                task["StyledHtmlClass.info"] = info;
                return AddStartSequenceInternal(info);
            }

            IEnumerable<HttpDataSource> AddStartSequenceInternal(ContentResult info)
            {
                yield return Stringer("<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><title>");
                if (info.Infos.Length == 0)
                    yield return Stringer("no Contents");
                else for (int i = 0; i<info.Infos.Length; ++i)
                    {
                        if (i > 0) yield return Stringer(",");
                        yield return Stringer(info.Infos[i].Name);
                    }
                yield return Stringer("</title><style rel=\"stylesheet\">", cssCode, "</style></head>",
                    "<body>", "<div class=\"title-bar\">", "<div class=\"go-back-field\">",
                    "<a class=\"dir-back root\" href=\"", info.DirRoot, "\">",
                    info.DirRoot, "</a>");
                var sb = new StringBuilder();
                sb.Append(info.DirRoot);
                for (int i = 0; i<info.CurrentDir.Length; ++i)
                {
                    if (i != 0)
                    {
                        sb.Append("/");
                        yield return Stringer("<span class=\"split\">/</span>");
                    }
                    sb.Append(WebServerHelper.EncodeUri(info.CurrentDir[i]));
                    yield return Stringer("<a href=\"", sb, "\">", info.CurrentDir[i], "</a>");
                }
                yield return Stringer("</div></div>", "<div class=\"content-area\">");
                yield return Stringer("<svg width=\"0\" height=\"", 0,
                    "\" xmlns=\"http://www.w3.org/2000/svg\">",
                    "<filter id=\"drop-shadow\">",
                    "<feGaussianBlur in=\"SourceAlpha\" stdDeviation=\"4\"/>",
                    "<feOffset dx=\"4.8\" dy=\"4.8\" result=\"offsetblur\"/>",
                    "<feFlood flood-color=\"rgba(0,0,0,0.5)\"/>",
                    "<feComposite in2=\"offsetblur\" operator=\"in\"/>",
                    "<feMerge>", "<feMergeNode/>",
                    "<feMergeNode in=\"SourceGraphic\"/>",
                    "</feMerge>", "</filter>", "</svg>");
            }

            protected override IEnumerable<HttpDataSource> AddEndSequence(ContentResult info, LazyTask task, SourceProvider source)
            {
                return AddEndSequenceInternal();
            }

            IEnumerable<HttpDataSource> AddEndSequenceInternal()
            {
                yield return Stringer("</div>", "</body></html>");
            }

            protected override IEnumerable<HttpDataSource> InsertEmptyContent(ContentInfo info, LazyTask task, SourceProvider source)
            {
                yield return Stringer(
                    "<div class=\"content-box unknown\"></div>"
                    );
            }

            protected override IEnumerable<HttpDataSource> NoContent(LazyTask task, SourceProvider source)
            {
                yield return Stringer("<div class=\"no-content\">no content in this directory</div>");
            }

            protected override IEnumerable<HttpDataSource> WrapAndInsertContent(ContentInfo info, IEnumerable<HttpDataSource> content, LazyTask task, SourceProvider source)
            {
                return content;
            }

            public StyledHtmlClass()
            {
                InfoViewer.Add(new Viewer());
            }

            class Viewer : ContentInfoViewer
            {
                public override IEnumerable<HttpDataSource> ViewContent(string path, ContentInfo info, LazyTask task, SourceProvider source)
                {
                    var complete = task["StyledHtmlClass.info"] as ContentResult;
                    if (complete.Infos.Length > 1)
                        yield return GetTitle(info);
                    switch (info.Type)
                    {
                        case ContentType.Directory:
                            foreach (var c in (info as DirectoryInfo).Contents)
                                foreach (var e in ShowContent(path + "/" + WebServerHelper.EncodeUri(c.Name), c, source))
                                    yield return e;
                            break;
                        case ContentType.File:
                            foreach (var e in ShowContent(path, info, source))
                                yield return e;
                            break;
                    }
                }
                
                HttpDataSource GetTitle(ContentInfo info)
                {
                    return Stringer("<div class=\"info-bar\">", info.Name, "</div>");
                }

                IEnumerable<HttpDataSource> ShowContent(string path, ContentInfo info, SourceProvider source)
                {
                    switch (info.Type)
                    {
                        case ContentType.Directory:
                            yield return Stringer("<a class=\"content-box directory\" data-name=\"",
                                info.Name, "\" data-exists=\"", info.Exists, "\" data-created=\"",
                                info.Created.ToString("s"), "\" data-modified=\"",
                                info.Modified.ToString("s"), "\" data-access=\"",
                                info.Access.ToString("s"), "\" href=\"", path, 
                                "\" title=\"", info.Name, "\">",
                                "<div class=\"content-top\">", "<div class=\"content-icon\">");
                            foreach (var e in ShowIcon(info.Icon))
                                yield return e;
                            yield return Stringer("</div></div>", "<div class=\"content-description\">",
                                "<div class=\"content-name\">", info.Name, "</div></div></a>");
                            break;
                        case ContentType.File:
                            var file = info as FileInfo;
                            yield return Stringer("<a class=\"content-box file\" data-name=\"",
                                info.Name, "\" data-exists=\"", info.Exists, "\" data-created=\"",
                                info.Created.ToString("s"), "\" data-modified=\"",
                                info.Modified.ToString("s"), "\" data-access=\"",
                                info.Access.ToString("s"), "\" data-length=\"", file.Length,
                                "\" data-length-text=\"", WebServerHelper.GetVolumeString(file.Length, true, 3),
                                "\" data-mime=\"", file.MimeType, "\" data-extension=\"", file.Extension,
                                "\" href=\"", source.NotifyRessource((info as FileInfo).LocalPath),
                                "\" title=\"", info.Name,
                                "\">", "<div class=\"content-top\">", "<div class=\"content-icon\">");
                            foreach (var e in ShowIcon(info.Icon))
                                yield return e;
                            yield return Stringer("</div></div>", "<div class=\"content-description\">",
                                "<div class=\"content-name\">", info.Name, "</div></div></a>");
                            break;
                        default:
                            yield return Stringer("<div class=\"content-box unknown\"></div>");
                            break;
                    }
                }

                IEnumerable<HttpDataSource> ShowIcon(IconInfo icon)
                {
                    yield return Stringer("<div class=\"icon\" data-type=\"", icon.Type,
                        "\" data-id=\"", icon.ContentId, "\">");
                    if (icon.Type == IconInfo.ContentIdType.Url)
                        yield return Stringer("<img src=\"", icon.ContentId, "\"></img>");
                    yield return Stringer("</div>");
                }
            }
        }
    }

    #endregion

    #endregion

    #region Source Provider

    public abstract class SourceProvider : FileSystemService
    {
        readonly string prefix;

        public int DefaultIconSize { get; set; }

        public override bool CanDeliverySourceUrlForPath => true;

        public override bool CanDeliverySourceUrlForContent => true;

        public override string GetDeliverySourceUrl(string[] relativePath, ContentInfo content)
        {
            if (content != null && content.Type != ContentType.File) return null;
            var sb = new StringBuilder();
            sb.Append("/");
            sb.Append(string.Join("/", PathRoot));
            sb.Append("/");
            sb.Append(string.Join("/", relativePath));
            if (content != null)
            {
                sb.Append("/");
                sb.Append(content.Name);
            }
            return sb.ToString();
        }

        public SourceProvider(string[] pathRoot) : base(pathRoot)
        {
            DefaultIconSize = 64;
            prefix = "/" + string.Join("/", pathRoot.Select((s) => WebServerHelper.EncodeUri(s)).ToArray());
        }

        public sealed override bool CanWorkWith(WebProgressTask task)
        {
            return base.CanWorkWith(task);
        }

        public sealed override void ProgressTask(WebProgressTask task)
        {
            var tp = task.Document.RequestHeader.Location.DocumentPathTiles;
            var rp = new string[tp.Length - PathRoot.Length];
            Array.Copy(tp, PathRoot.Length, rp, 0, rp.Length);
            bool success;
            if (rp.Length > 0 && rp[0].StartsWith("@"))
            {
                var id = rp[0].Length > 1 ? rp[0].Substring(1) : "";
                success = GetRessource(id, task);
            }
            else if (rp.Length > 0 && rp[0].StartsWith("~"))
            {
                var code = string.Join("/", rp);
                code = code.Length > 1 ? code.Substring(1) : "";
                code = code.Replace(' ', '+');
                byte[] hash;
                try { hash = Convert.FromBase64String(code); }
                catch { hash = null; }
                success = hash != null && GetRessource(hash, task);
            }
            else
            {
                success = GetRessource(rp, task);
            }
            if (!success)
            {
                var r = task.Document.ResponseHeader;
                r.StatusCode = HttpStateCode.NotFound;
            }
        }

        protected string GetPath(string id)
        {
            return prefix + "/@" + id;
        }

        protected string GetPath(byte[] hash)
        {
            return prefix + "/~" + Convert.ToBase64String(hash);
        }

        protected string GetPath(string[] path)
        {
            return prefix + "/" + 
                string.Join("/", path.Select((s)=>WebServerHelper.EncodeUri(s)).ToArray());
        }

        protected abstract bool GetRessource(string id, WebProgressTask task);

        protected abstract bool GetRessource(byte[] hash, WebProgressTask task);

        protected abstract bool GetRessource(string[] path, WebProgressTask task);

        /// <summary>
        /// Notifies a ressource could be loaded in the next time.
        /// Returns a unique url for ressource access
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public abstract string NotifyRessource(string localPath);

        /// <summary>
        /// Notifies a specific ressource at a local path. It has a high possibility to
        /// be loaded next time. It returns a unique url for this ressource access depends
        /// on the content info.
        /// </summary>
        /// <param name="localPath">the local path where this ressource is stores</param>
        /// <param name="content">specify the content and provider</param>
        /// <returns>a unique url</returns>
        public abstract string NotifyRessource(string localPath, ContentInfo content);

        /// <summary>
        /// Creates a ressource handler for a new temporary file. After the file is successfully
        /// loaded the method <see cref="RessourceToken.NotifyContentReady()"/> should be called. 
        /// Then the ressource is accessible through this access.
        /// 
        /// If the content is already loaded, then <see cref="RessourceToken.ContentReady"/>
        /// is true.
        /// </summary>
        /// <param name="ressourceHandle">a unique ressource handle</param>
        /// <returns>a ressource handler</returns>
        public abstract RessourceToken CreateTempRessource(string ressourceHandle);
    }

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
                if (path.Length > 0 && path[path.Length-1].StartsWith("@"))
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

    public abstract class RessourceToken
    {
        /// <summary>
        /// The unique id of this ressource. Its provided by <see cref="SourceProvider"/>.
        /// </summary>
        public string Id { get; protected set; }

        /// <summary>
        /// The hash sum of this ressource.
        /// </summary>
        public byte[] Hash { get; protected set; }

        /// <summary>
        /// The Mime type of this ressource. If no mime-type is given, its guessed automaticly
        /// through the server config.
        /// </summary>
        public string Mime { get; protected set; }

        /// <summary>
        /// The local path of this ressource. If this class is created through 
        /// <see cref="SourceProvider.CreateTempRessource(string)"/> then this shows the target
        /// path of the temp file
        /// </summary>
        public string LocalPath { get; protected set; }

        /// <summary>
        /// The unique url of this ressource. Its only provided if <see cref="ContentReady"/>
        /// is true.
        /// </summary>
        public string Url { get; protected set; }

        /// <summary>
        /// Explains if the content is ready or not
        /// </summary>
        public bool ContentReady { get; protected set; }

        /// <summary>
        /// If this <see cref="RessourceToken"/> is created through 
        /// <see cref="SourceProvider.CreateTempRessource(string)"/> then this shows the 
        /// provided ressource handle. Otherwise its null.
        /// </summary>
        public string RessourceHandle { get; protected set; }

        /// <summary>
        /// Notifies the <see cref="SourceProvider"/> this content is ready. <see cref="SourceProvider"/>
        /// adds this file to its accessible ressources.
        /// </summary>
        public abstract void NotifyContentReady();

        /// <summary>
        /// Overrides the local <see cref="Mime"/>.
        /// </summary>
        /// <param name="mime">the new mime type</param>
        public abstract void SetMime(string mime);

        /// <summary>
        /// Discards the temporary file. Its only possible to call, when this class is created with
        /// <see cref="SourceProvider.CreateTempRessource(string)"/> and <see cref="NotifyContentReady"/>
        /// was never called.
        /// </summary>
        public abstract void Discard();
    }

    #endregion
}
