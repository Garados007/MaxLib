using System;

namespace MaxLib.Net.Webserver.Files
{
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
}
