using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpPost
    {
        public string CompletePost { get; private set; }

        public string MimeType { get; private set; }

        public Dictionary<string, string> PostParameter { get; }

        public virtual void SetPost(string post, string mime)
        {
            CompletePost = post ?? throw new ArgumentNullException("Post");

            PostParameter.Clear();
            string args = "";
            if (mime != null)
            {
                var ind = mime.IndexOf(';');
                if (ind >= 0)
                {
                    args = mime.Substring(ind + 1);
                    mime = mime.Remove(ind);
                }
            }

            switch (MimeType = mime)
            {
                case Webserver.MimeType.ApplicationXWwwFromUrlencoded:
                    SetPostFormUrlencoded(post);
                    break;
                case Webserver.MimeType.MultipartFormData:
                    {
                        var regex = new Regex("boundary\\s*=\\s*\"([^\"]*)\"");
                        var match = regex.Match(args);
                        var boundary = match.Success ? match.Groups[1].Value : "";
                        SetPostFormData(post, boundary);
                    } break;
            }
            PostParameter.Clear();
        }

        protected virtual void SetPostFormUrlencoded(string post)
        {
            if (CompletePost != "")
            {
                var tiles = CompletePost.Split('&');
                foreach (var tile in tiles)
                {
                    var ind = tile.IndexOf('=');
                    if (ind == -1)
                    {
                        var t = WebServerUtils.DecodeUri(tile);
                        if (!PostParameter.ContainsKey(t)) PostParameter.Add(t, "");
                    }
                    else
                    {
                        var key = WebServerUtils.DecodeUri(tile.Remove(ind));
                        var value = ind + 1 == tile.Length ? "" : tile.Substring(ind + 1);
                        if (!PostParameter.ContainsKey(key)) PostParameter.Add(key, WebServerUtils.DecodeUri(value));
                    }
                }
            }
        }

        protected virtual void SetPostFormData(string post, string boundary)
        {

        }

        public HttpPost(string post, string mime)
        {
            _ = post ?? throw new ArgumentNullException(nameof(post));
            PostParameter = new Dictionary<string, string>();
            SetPost(post, mime);
        }

        public override string ToString()
        {
            return CompletePost;
        }
    }
}
