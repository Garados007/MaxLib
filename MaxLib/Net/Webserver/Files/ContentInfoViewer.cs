using MaxLib.Net.Webserver.Lazy;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver.Files
{
    public abstract class ContentInfoViewer
    {
        public abstract IEnumerable<HttpDataSource> ViewContent(string path,
            ContentInfo info, LazyTask task, SourceProvider source);
    }
}
