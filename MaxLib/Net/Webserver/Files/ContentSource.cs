using System;

namespace MaxLib.Net.Webserver.Files
{
    public abstract class ContentSource : IDisposable
    {
        public abstract string[] RootUrl { get; }

        public abstract bool Strict { get; } //enable merge content or not

        public abstract void Dispose();

        public abstract ContentInfo TryGetContent(string[] relativePath, WebProgressTask task);

        public abstract Tuple<ContentInfo, string[]> ReverseSearch(string localPath);
    }
}
