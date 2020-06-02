using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpLocation
    {
        public string Url { get; private set; }

        public string DocumentPath { get; private set; }

        public string[] DocumentPathTiles { get; private set; }

        public string CompleteGet { get; private set; }

        public Dictionary<string, string> GetParameter { get; private set; }

        public virtual void SetLocation(string url)
        {
            Url = url ?? throw new ArgumentNullException("url");
            var ind = url.IndexOf('?');
            if (ind == -1)
            {
                DocumentPath = url;
                CompleteGet = "";
            }
            else
            {
                DocumentPath = url.Remove(ind);
                CompleteGet = ind + 1 == url.Length ? "" : url.Substring(ind + 1);
            }
            var path = DocumentPath.Trim('/');
            DocumentPathTiles = path.Split('/');
            for (int i = 0; i < DocumentPathTiles.Length; ++i) DocumentPathTiles[i] = WebServerUtils.DecodeUri(DocumentPathTiles[i]);
            GetParameter.Clear();
            if (CompleteGet != "")
            {
                var tiles = CompleteGet.Split('&');
                foreach (var tile in tiles)
                {
                    ind = tile.IndexOf('=');
                    if (ind == -1)
                    {
                        var key = WebServerUtils.DecodeUri(tile);
                        if (!GetParameter.ContainsKey(key)) GetParameter.Add(key, "");
                    }
                    else
                    {
                        var key = WebServerUtils.DecodeUri(tile.Remove(ind));
                        var value = ind + 1 == tile.Length ? "" : tile.Substring(ind + 1);
                        if (!GetParameter.ContainsKey(key)) GetParameter.Add(key, WebServerUtils.DecodeUri(value));
                    }
                }
            }
        }

        public HttpLocation(string url)
        {
            if (url == null) throw new ArgumentNullException("url");
            GetParameter = new Dictionary<string, string>();
            SetLocation(url);
        }

        public override string ToString()
        {
            return Url;
        }

        public bool IsUrl(string[] urlTiles, bool ignoreCase = false)
        {
            if (urlTiles.Length != DocumentPathTiles.Length) return false;
            for (int i = 0; i < urlTiles.Length; ++i)
                if (ignoreCase)
                {
                    if (urlTiles[i].ToLower() != DocumentPathTiles[i].ToLower()) return false;
                }
                else
                {
                    if (urlTiles[i] != DocumentPathTiles[i]) return false;
                }
            return true;
        }

        public bool StartsUrlWith(string[] urlTiles, bool ignoreCase = false)
        {
            if (urlTiles.Length > DocumentPathTiles.Length) return false;
            for (int i = 0; i < urlTiles.Length; ++i)
                if (ignoreCase)
                {
                    if (urlTiles[i].ToLower() != DocumentPathTiles[i].ToLower()) return false;
                }
                else
                {
                    if (urlTiles[i] != DocumentPathTiles[i]) return false;
                }
            return true;
        }
    }
}
