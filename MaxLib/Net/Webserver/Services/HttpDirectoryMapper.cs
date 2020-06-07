using System;
using System.IO;

namespace MaxLib.Net.Webserver.Services
{
    /// <summary>
    /// WebServiceType.PreCreateDocument: Lädt das Dokument, welches vorher von <see cref="HttpDocumentFinder"/> 
    /// gefunden wurde.
    /// </summary>
    public class HttpDirectoryMapper : WebService
    {
        public bool MapFolderToo { get; set; }
        /// <summary>
        /// WebServiceType.PreCreateDocument: Lädt das Dokument, welches vorher von <see cref="HttpDocumentFinder"/> 
        /// gefunden wurde.
        /// </summary>
        public HttpDirectoryMapper(bool mapFolderToo)
            : base(WebServiceType.PreCreateDocument)
        {
            MapFolderToo = mapFolderToo;
        }

        protected string GetMime(string extension, WebProgressTask task)
        {
            _ = extension ?? throw new ArgumentNullException(nameof(extension));
            _ = task ?? throw new ArgumentNullException(nameof(task));
            switch (extension.ToLower())
            {
                case ".html": return MimeType.TextHtml;
                case ".htm": return MimeType.TextHtml;
                case ".js": return MimeType.TextJs;
                case ".css": return MimeType.TextCss;
                case ".jpg": return MimeType.ImageJpeg;
                case ".jpeg": return MimeType.ImageJpeg;
                case ".png": return MimeType.ImagePng;
                case ".pneg": return MimeType.ImagePng;
                case ".gif": return MimeType.ImageGif;
                case ".ico": return MimeType.ImageIcon;
                case ".txt": return MimeType.TextPlain;
                case ".xml": return MimeType.TextXml;
                case ".rtf": return MimeType.ApplicationRtf;
                default:
                    var set = task.Server.Settings;
                    if (set.DefaultFileMimeAssociation.ContainsKey(extension.ToLower()))
                        return set.DefaultFileMimeAssociation[extension.ToLower()];
                    return MimeType.TextPlain;
            }
        }

        public override void ProgressTask(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));

            if (task.Document.Information.ContainsKey("HttpDocumentFile"))
            {
                var path = task.Document.Information["HttpDocumentFile"].ToString();
                var source = new HttpFileDataSource(path)
                {
                    TransferCompleteData = true,
                    MimeType = GetMime(Path.GetExtension(path), task)
                };
                task.Document.DataSources.Add(source);
                task.Document.ResponseHeader.StatusCode = HttpStateCode.OK;
            }
            if (MapFolderToo && task.Document.Information.ContainsKey("HttpDocumentFolder"))
            {
                var path = task.Document.Information["HttpDocumentFolder"].ToString();
                var url = task.Document.RequestHeader.Location.DocumentPath.TrimEnd('/');
                var d = new DirectoryInfo(path);
                var html = "<html lang=\"de\"><head><title>" + d.Name + "</title></head><body>";
                html += "<h1>" + path + "</h1><a href=\"../\">Eine Ebene h&ouml;her</a><ul>";
                foreach (var di in d.GetDirectories())
                    html += "<li>DIRECTORY: <a href=\"" + url + "/" + WebServerUtils.EncodeUri(di.Name) + "\">" + di.Name + "</a></li>";
                foreach (var fi in d.GetFiles())
                    html += "<li>FILE: <a href=\"" + url + "/" + WebServerUtils.EncodeUri(fi.Name) + "\">" +
                        fi.Name + "</a> [" + WebServerUtils.GetVolumeString(fi.Length, true, 4) + "]</li>";
                html += "</ul>Ende der Ausgabe.</body></html>";
                var source = new HttpStringDataSource(html)
                {
                    TransferCompleteData = true,
                    MimeType = MimeType.TextHtml
                };
                task.Document.DataSources.Add(source);
                task.Document.ResponseHeader.StatusCode = HttpStateCode.OK;
            }
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));

            if (MapFolderToo && task.Document.Information.ContainsKey("HttpDocumentFolder")) return true;
            return task.Document.Information.ContainsKey("HttpDocumentFile");
        }
    }
}
