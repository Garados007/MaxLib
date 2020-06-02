using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpPost
    {
        public string CompletePost { get; private set; }

        public Dictionary<string, string> PostParameter { get; private set; }

        public virtual void SetPost(string post)
        {
            CompletePost = post ?? throw new ArgumentNullException("Post");
            PostParameter.Clear();
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

        public HttpPost(string post)
        {
            if (post == null) throw new ArgumentNullException("Post");
            PostParameter = new Dictionary<string, string>();
            SetPost(post);
        }

        public override string ToString()
        {
            return CompletePost;
        }
    }
}
