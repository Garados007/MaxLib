using MaxLib.Collections;
using MaxLib.Net.Webserver.Lazy;
using System.Collections.Generic;
using System.Text;
using MaxLib.Net.Webserver.Files.Source;
using MaxLib.Net.Webserver.Files.Content.Grabber.Info;
using MaxLib.Net.Webserver.Files.Content.Grabber.Icons;

namespace MaxLib.Net.Webserver.Files.Content.Viewer
{
    public class ContentViewerFactory
    {
        static HttpDataSource Stringer(params object[] tiles)
        {
            var sb = new StringBuilder();
            foreach (var t in tiles)
                sb.Append(t);
            return new HttpStringDataSource(sb.ToString())
            {
                MimeType = MimeTypes.TextHtml,
                TransferCompleteData = true
            };
        }

        public ContentViewer SimpleHtml => new SimpleHtmlClass();

        public ContentViewer StyledHtml => new StyledHtmlClass();

        class SimpleHtmlClass : ContentViewer
        {
            protected override IEnumerable<HttpDataSource> AddStartSequence(ContentResult info, LazyTask task, SourceProvider source)
            {
                yield return new HttpStringDataSource(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0 /"">
    <title>" + (info.FirstRessourceName ?? "no ressource") + @"</title>
</head>
<body>
    <p>
        Parent dir: <a href=""" + info.ParentUrl + @""">" + info.ParentUrl + @"</a>
    </p>
    <ul>
")
                {
                    MimeType = MimeTypes.TextHtml,
                    TransferCompleteData = true,

                };
            }

            protected override IEnumerable<HttpDataSource> AddEndSequence(ContentResult info, LazyTask task, SourceProvider source)
            {
                yield return new HttpStringDataSource(@"
    </ul>
</body>
</html>
")
                {
                    MimeType = MimeTypes.TextHtml,
                    TransferCompleteData = true,

                };
            }

            protected override IEnumerable<HttpDataSource> InsertEmptyContent(ContentInfo info, LazyTask task, SourceProvider source)
            {
                yield return Stringer("<li>No visualizer found for ",
                    info.Type, ":", info.Name, "!</li>");
            }

            protected override IEnumerable<HttpDataSource> NoContent(LazyTask task, SourceProvider source)
            {
                yield return Stringer("<li>No content exists in this Directory!</li>");
            }

            protected override IEnumerable<HttpDataSource> WrapAndInsertContent(ContentInfo info, IEnumerable<HttpDataSource> content, LazyTask task, SourceProvider source)
            {
                var enb = new EnumeratorBuilder<HttpDataSource>();
                enb.Yield(Stringer("<li>"));
                enb.Yield(content);
                enb.Yield(Stringer("</li>"));
                return enb;
            }

            class EntryViewer : ContentInfoViewer
            {
                public override IEnumerable<HttpDataSource> ViewContent(string path, ContentInfo info, LazyTask task, SourceProvider source)
                {
                    switch (info.Type)
                    {
                        case ContentType.File:
                            yield return Stringer("<a href=\"", source.NotifyRessource((info as FileInfo).LocalPath),
                                "\">", info.Name, "</a> <b>[",
                                WebServerHelper.GetVolumeString((info as FileInfo).Length, true, 3),
                                "]</b>");
                            break;
                        case ContentType.Directory:
                            yield return Stringer("<a href=\"", path, "\">", info.Name, "</a>");
                            if ((info as DirectoryInfo).Contents != null)
                            {
                                yield return Stringer("<br/><ul>");
                                foreach (var e in (info as DirectoryInfo).Contents)
                                {
                                    yield return Stringer("<li>");
                                    var p = path + "/" + WebServerHelper.EncodeUri(e.Name);
                                    foreach (var c in ViewContent(p, e, task, source))
                                        yield return c;
                                    yield return Stringer("</li>");
                                }
                                yield return Stringer("</ul>");
                            }
                            break;
                        default:
                            yield return Stringer(info.Name);
                            break;
                    }
                }
            }

            public SimpleHtmlClass()
            {
                InfoViewer.Add(new EntryViewer());
            }
        }

        class StyledHtmlClass : ContentViewer
        {
            readonly string cssCode = Properties.Resources.Net_Webserver_Files_ViewerHtmlCss;

            protected override IEnumerable<HttpDataSource> AddStartSequence(ContentResult info, LazyTask task, SourceProvider source)
            {
                task["StyledHtmlClass.info"] = info;
                return AddStartSequenceInternal(info);
            }

            IEnumerable<HttpDataSource> AddStartSequenceInternal(ContentResult info)
            {
                yield return Stringer("<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><title>");
                if (info.Infos.Length == 0)
                    yield return Stringer("no Contents");
                else for (int i = 0; i < info.Infos.Length; ++i)
                    {
                        if (i > 0) yield return Stringer(",");
                        yield return Stringer(info.Infos[i].Name);
                    }
                yield return Stringer("</title><style rel=\"stylesheet\">", cssCode, "</style></head>",
                    "<body>", "<div class=\"title-bar\">", "<div class=\"go-back-field\">",
                    "<a class=\"dir-back root\" href=\"", info.DirRoot, "\">",
                    info.DirRoot, "</a>");
                var sb = new StringBuilder();
                sb.Append(info.DirRoot);
                for (int i = 0; i < info.CurrentDir.Length; ++i)
                {
                    if (i != 0)
                    {
                        sb.Append("/");
                        yield return Stringer("<span class=\"split\">/</span>");
                    }
                    sb.Append(WebServerHelper.EncodeUri(info.CurrentDir[i]));
                    yield return Stringer("<a href=\"", sb, "\">", info.CurrentDir[i], "</a>");
                }
                yield return Stringer("</div></div>", "<div class=\"content-area\">");
                yield return Stringer("<svg width=\"0\" height=\"", 0,
                    "\" xmlns=\"http://www.w3.org/2000/svg\">",
                    "<filter id=\"drop-shadow\">",
                    "<feGaussianBlur in=\"SourceAlpha\" stdDeviation=\"4\"/>",
                    "<feOffset dx=\"4.8\" dy=\"4.8\" result=\"offsetblur\"/>",
                    "<feFlood flood-color=\"rgba(0,0,0,0.5)\"/>",
                    "<feComposite in2=\"offsetblur\" operator=\"in\"/>",
                    "<feMerge>", "<feMergeNode/>",
                    "<feMergeNode in=\"SourceGraphic\"/>",
                    "</feMerge>", "</filter>", "</svg>");
            }

            protected override IEnumerable<HttpDataSource> AddEndSequence(ContentResult info, LazyTask task, SourceProvider source)
            {
                return AddEndSequenceInternal();
            }

            IEnumerable<HttpDataSource> AddEndSequenceInternal()
            {
                yield return Stringer("</div>", "</body></html>");
            }

            protected override IEnumerable<HttpDataSource> InsertEmptyContent(ContentInfo info, LazyTask task, SourceProvider source)
            {
                yield return Stringer(
                    "<div class=\"content-box unknown\"></div>"
                    );
            }

            protected override IEnumerable<HttpDataSource> NoContent(LazyTask task, SourceProvider source)
            {
                yield return Stringer("<div class=\"no-content\">no content in this directory</div>");
            }

            protected override IEnumerable<HttpDataSource> WrapAndInsertContent(ContentInfo info, IEnumerable<HttpDataSource> content, LazyTask task, SourceProvider source)
            {
                return content;
            }

            public StyledHtmlClass()
            {
                InfoViewer.Add(new Viewer());
            }

            class Viewer : ContentInfoViewer
            {
                public override IEnumerable<HttpDataSource> ViewContent(string path, ContentInfo info, LazyTask task, SourceProvider source)
                {
                    var complete = task["StyledHtmlClass.info"] as ContentResult;
                    if (complete.Infos.Length > 1)
                        yield return GetTitle(info);
                    switch (info.Type)
                    {
                        case ContentType.Directory:
                            foreach (var c in (info as DirectoryInfo).Contents)
                                foreach (var e in ShowContent(path + "/" + WebServerHelper.EncodeUri(c.Name), c, source))
                                    yield return e;
                            break;
                        case ContentType.File:
                            foreach (var e in ShowContent(path, info, source))
                                yield return e;
                            break;
                    }
                }

                HttpDataSource GetTitle(ContentInfo info)
                {
                    return Stringer("<div class=\"info-bar\">", info.Name, "</div>");
                }

                IEnumerable<HttpDataSource> ShowContent(string path, ContentInfo info, SourceProvider source)
                {
                    switch (info.Type)
                    {
                        case ContentType.Directory:
                            yield return Stringer("<a class=\"content-box directory\" data-name=\"",
                                info.Name, "\" data-exists=\"", info.Exists, "\" data-created=\"",
                                info.Created.ToString("s"), "\" data-modified=\"",
                                info.Modified.ToString("s"), "\" data-access=\"",
                                info.Access.ToString("s"), "\" href=\"", path,
                                "\" title=\"", info.Name, "\">",
                                "<div class=\"content-top\">", "<div class=\"content-icon\">");
                            foreach (var e in ShowIcon(info.Icon))
                                yield return e;
                            yield return Stringer("</div></div>", "<div class=\"content-description\">",
                                "<div class=\"content-name\">", info.Name, "</div></div></a>");
                            break;
                        case ContentType.File:
                            var file = info as FileInfo;
                            yield return Stringer("<a class=\"content-box file\" data-name=\"",
                                info.Name, "\" data-exists=\"", info.Exists, "\" data-created=\"",
                                info.Created.ToString("s"), "\" data-modified=\"",
                                info.Modified.ToString("s"), "\" data-access=\"",
                                info.Access.ToString("s"), "\" data-length=\"", file.Length,
                                "\" data-length-text=\"", WebServerHelper.GetVolumeString(file.Length, true, 3),
                                "\" data-mime=\"", file.MimeType, "\" data-extension=\"", file.Extension,
                                "\" href=\"", source.NotifyRessource((info as FileInfo).LocalPath),
                                "\" title=\"", info.Name,
                                "\">", "<div class=\"content-top\">", "<div class=\"content-icon\">");
                            foreach (var e in ShowIcon(info.Icon))
                                yield return e;
                            yield return Stringer("</div></div>", "<div class=\"content-description\">",
                                "<div class=\"content-name\">", info.Name, "</div></div></a>");
                            break;
                        default:
                            yield return Stringer("<div class=\"content-box unknown\"></div>");
                            break;
                    }
                }

                IEnumerable<HttpDataSource> ShowIcon(IconInfo icon)
                {
                    yield return Stringer("<div class=\"icon\" data-type=\"", icon.Type,
                        "\" data-id=\"", icon.ContentId, "\">");
                    if (icon.Type == IconInfo.ContentIdType.Url)
                        yield return Stringer("<img src=\"", icon.ContentId, "\"></img>");
                    yield return Stringer("</div>");
                }
            }
        }
    }
}
