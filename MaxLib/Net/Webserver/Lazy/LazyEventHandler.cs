using System.Collections.Generic;

namespace MaxLib.Net.Webserver.Lazy
{
    public delegate IEnumerable<HttpDataSource> LazyEventHandler(LazyTask task);
}
