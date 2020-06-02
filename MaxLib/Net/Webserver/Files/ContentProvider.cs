using System;
using System.Linq;
using System.Text;
using MaxLib.Net.Webserver.Files.Content.Grabber.Icons;
using MaxLib.Net.Webserver.Files.Content.Grabber.Info;
using MaxLib.Net.Webserver.Files.Content.Viewer;
using MaxLib.Net.Webserver.Files.Source;

namespace MaxLib.Net.Webserver.Files
{
    public class ContentProvider : FileSystemService
    {
        public SourceProvider SourceProvider { get; set; }

        public ContentViewer ContentViewer { get; set; }

        public IconFetcher IconFetcher { get; set; }

        public override bool CanDeliverySourceUrlForContent => true;

        public override bool CanDeliverySourceUrlForPath => true;

        public override string GetDeliverySourceUrl(string[] relativePath, ContentInfo content)
        {
            var sb = new StringBuilder();
            sb.Append("/");
            sb.Append(string.Join("/", PathRoot));
            sb.Append("/");
            sb.Append(string.Join("/", relativePath));
            if (content != null)
            {
                sb.Append("/");
                sb.Append(content.Name);
            }
            return sb.ToString();
        }

        public ContentProvider(string[] pathRoot) : base(pathRoot)
        {
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            return base.CanWorkWith(task) && Contents != null &&
                SourceProvider != null && ContentViewer != null;
        }

        public override void ProgressTask(WebProgressTask task)
        {
            var tp = task.Document.RequestHeader.Location.DocumentPathTiles;
            var rp = new string[tp.Length - PathRoot.Length];
            Array.Copy(tp, PathRoot.Length, rp, 0, rp.Length);
            var content = Contents.GetContents(rp, task).ToArray();
            if (IconFetcher != null)
                foreach (var c in content)
                    LoadIcons(c, task);
            var result = new ContentResult()
            {
                CurrentDir = rp,
                CurrentUrl = "/" + 
                    string.Join("/", tp.Select((p) => WebServerUtils.EncodeUri(p)).ToArray()),
                DirRoot = "/" + string.Join("/",
                    PathRoot.Select((p) => WebServerUtils.EncodeUri(p)).ToArray()) + "/",
                FirstRessourceName = content.Length > 0 ? content[0].Name : null,
                Infos = content,
                ParentUrl = "/" + 
                    string.Join("/", tp.Select((p) => WebServerUtils.EncodeUri(p)).ToArray(),
                        0, rp.Length > 0 ? tp.Length - 1 : tp.Length),
                UrlName = rp.Length == 0 ? "" : rp[rp.Length - 1]
            };
            ContentViewer.Show(result, task, SourceProvider);
        }

        void LoadIcons(ContentInfo content, WebProgressTask task)
        {
            IconFetcher.TryGetIcon(content, SourceProvider, task);
            if (content.Type == ContentType.Directory &&
                (content as DirectoryInfo).Contents != null)
                foreach (var c in (content as DirectoryInfo).Contents)
                    LoadIcons(c, task);
        }
    }
}
