using MaxLib.Net.Webserver.Lazy;
using System.Collections.Generic;
using MaxLib.Net.Webserver.Files.Source;
using MaxLib.Net.Webserver.Files.Content.Grabber.Info;

namespace MaxLib.Net.Webserver.Files.Content.Viewer
{
    public abstract class ContentInfoViewer
    {
        public abstract IEnumerable<HttpDataSource> ViewContent(string path,
            ContentInfo info, LazyTask task, SourceProvider source);
    }
}
