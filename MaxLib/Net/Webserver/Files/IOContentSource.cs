using System;
using IO = System.IO;

namespace MaxLib.Net.Webserver.Files
{
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
}
