using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpLocation
    {
        public string Url { get; private set; }

        public string DocumentPath { get; private set; }

        public string[] DocumentPathTiles { get; private set; }

        public string CompleteGet { get; private set; }

        public Dictionary<string, string> GetParameter { get; }

        static readonly Regex UrlRegex = new Regex(@"^((?:\/+([^\/?]+))*\/?)(?:\?((?:([^&$]*)&?)*))?$", RegexOptions.Compiled);

        static readonly Regex ArgsRegex = new Regex(@"^([^=]*)=(.*)$", RegexOptions.Compiled);

        public virtual void SetLocation(string url)
        {
            Url = url ?? throw new ArgumentNullException(url);
                GetParameter.Clear();
            var match = UrlRegex.Match(url);
            if (!match.Success)
            {
                DocumentPath = url;
                DocumentPathTiles = new[] { url };
                CompleteGet = "";
            }
            DocumentPath = match.Groups[1].Value;
            DocumentPathTiles = match.Groups[2].Captures
                .OfType<Capture>()
                .Select(c => WebServerUtils.DecodeUri(c.Value))
                .ToArray();
            CompleteGet = match.Groups[3].Success ? match.Groups[3].Value ?? "" : "";
            foreach (Capture capture in match.Groups[4].Captures)
            {
                var submatch = ArgsRegex.Match(capture.Value);
                if (submatch.Success)
                {
                    GetParameter[WebServerUtils.DecodeUri(submatch.Groups[1].Value)]
                        = WebServerUtils.DecodeUri(submatch.Groups[2].Value);
                }
                else
                {
                    GetParameter[capture.Value] = "";
                }
            }
        }

        public HttpLocation(string url)
        {
            _ = url ?? throw new ArgumentNullException(nameof(url));
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
