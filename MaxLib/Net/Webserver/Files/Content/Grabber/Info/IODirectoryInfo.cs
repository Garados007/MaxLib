using System;
using System.Collections.Generic;
using IO = System.IO;

namespace MaxLib.Net.Webserver.Files.Content.Grabber.Info
{
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

}
