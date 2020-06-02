using System;
using IO = System.IO;

namespace MaxLib.Net.Webserver.Files
{
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
}
