using System;

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
}
