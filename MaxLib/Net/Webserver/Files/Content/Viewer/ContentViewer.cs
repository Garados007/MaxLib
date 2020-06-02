using MaxLib.Collections;
using MaxLib.Net.Webserver.Lazy;
using System.Collections.Generic;
using MaxLib.Net.Webserver.Files.Source;
using MaxLib.Net.Webserver.Files.Content.Grabber.Info;

namespace MaxLib.Net.Webserver.Files.Content.Viewer
{
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
}
