using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpCookie
    {
        [Serializable]
        public class Cookie
        {
            public string Name { get; private set; }
            public string Value { get; private set; }
            public DateTime Expires { get; private set; }
            public int MaxAge { get; private set; }
            public string Path { get; private set; }

            public Cookie(string name, string value)
            {
                Name = name ?? throw new ArgumentNullException("name");
                Value = value ?? throw new ArgumentNullException("value");
                Expires = new DateTime(9999, 12, 31);
                MaxAge = -1;
                Path = "";
            }

            public Cookie(string name, string value, DateTime expires)
            {
                Name = name ?? throw new ArgumentNullException("name");
                Value = value ?? throw new ArgumentNullException("value");
                Expires = expires;
                MaxAge = -1;
                Path = "";
            }

            public Cookie(string name, string value, int maxAge)
            {
                Name = name ?? throw new ArgumentNullException("name");
                Value = value ?? throw new ArgumentNullException("value");
                Expires = new DateTime(9999, 12, 31);
                MaxAge = maxAge;
                Path = "";
            }

            public Cookie(string name, string value, string path)
            {
                Name = name ?? throw new ArgumentNullException("name");
                Value = value ?? throw new ArgumentNullException("value");
                Expires = new DateTime(9999, 12, 31);
                MaxAge = -1;
                Path = path ?? throw new ArgumentNullException("path");
            }

            public Cookie(string name, string value, DateTime expires, int maxAge, string path)
            {
                Name = name ?? throw new ArgumentNullException("name");
                Value = value ?? throw new ArgumentNullException("value");
                Expires = expires;
                MaxAge = maxAge;
                Path = path ?? throw new ArgumentNullException("path");
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append(WebServerHelper.EncodeUri(Name));
                sb.Append('=');
                sb.Append(WebServerHelper.EncodeUri(Value));
                if (Expires != new DateTime(9999, 12, 31))
                {
                    sb.Append(";expires=");
                    sb.Append(WebServerHelper.GetDateString(Expires));
                }
                if (MaxAge != -1)
                {
                    sb.Append(";Max-Age=");
                    sb.Append(MaxAge);
                }
                sb.Append(";Path=");
                sb.Append(Path);
                return sb.ToString();
            }
        }

        public string CompleteRequestCookie { get; private set; }

        public List<Cookie> AddedCookies { get; private set; }

        public Cookie[] RequestedCookies { get; private set; }

        public HttpCookie(string cookie)
        {
            if (cookie == null) throw new ArgumentNullException("Cookie");
            AddedCookies = new List<Cookie>();
            RequestedCookies = new Cookie[0];
            SetRequestCookieString(cookie);
        }

        public Cookie Get(string name)
        {
            var cookie = AddedCookies.Find((c) => c.Name == name);
            if (cookie != null) return cookie;
            return RequestedCookies.ToList().Find((c) => c.Name == name);
        }

        public virtual void SetRequestCookieString(string cookie)
        {
            CompleteRequestCookie = cookie ?? throw new ArgumentNullException("Cookie");
            AddedCookies.Clear();
            var l = new List<Cookie>();
            var rck = new List<string>();
            if (CompleteRequestCookie != "")
            {
                var tiles = CompleteRequestCookie.Split('&', ';');
                foreach (var tile in tiles)
                {
                    var ind = tile.IndexOf('=');
                    if (ind == -1)
                    {
                        var key = WebServerHelper.DecodeUri(tile.Trim());
                        if (!rck.Contains(key))
                        {
                            rck.Add(key);
                            l.Add(new Cookie(key, ""));
                        }
                    }
                    else
                    {
                        var key = WebServerHelper.DecodeUri(tile.Remove(ind).Trim());
                        var value = ind + 1 == tile.Length ? "" : tile.Substring(ind + 1);
                        if (!rck.Contains(key))
                        {
                            rck.Add(key);
                            l.Add(new Cookie(key, value));
                        }
                    }
                }
            }
            RequestedCookies = l.ToArray();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(CompleteRequestCookie);
            return sb.ToString();
        }
    }
}
