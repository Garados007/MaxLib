using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class HttpCookie
    {
        [Serializable]
        public readonly struct Cookie
        {
            public ReadOnlyMemory<char> Name { get; }

            public string NameString => Name.ToString();
            public ReadOnlyMemory<char> Value { get; }

            public string ValueString => Value.ToString();
            public DateTime Expires { get; }
            public int MaxAge { get; }
            public ReadOnlyMemory<char> Path { get; }

            public Cookie(string name, string value)
                : this(name, value, new DateTime(9999, 12, 31), -1, "")
            { }

            public Cookie(string name, string value, DateTime expires)
                : this(name, value, expires, -1, "")
            { }

            public Cookie(string name, string value, int maxAge)
                : this(name, value, new DateTime(9999, 12, 31), maxAge, "")
            { }

            public Cookie(string name, string value, string path)
                : this(name, value, new DateTime(9999, 12, 31), -1, path) 
            { }

            public Cookie(string name, string value, DateTime expires, int maxAge, string path)
            {
                Name = name?.AsMemory() ?? throw new ArgumentNullException(nameof(name));
                Value = value?.AsMemory() ?? throw new ArgumentNullException(nameof(value));
                Expires = expires;
                MaxAge = maxAge;
                Path = path?.AsMemory() ?? throw new ArgumentNullException(nameof(path));
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append(WebServerUtils.EncodeUri(Name.ToString()));
                sb.Append('=');
                sb.Append(WebServerUtils.EncodeUri(Value.ToString()));
                if (Expires != new DateTime(9999, 12, 31))
                {
                    sb.Append(";expires=");
                    sb.Append(WebServerUtils.GetDateString(Expires));
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

        public Dictionary<string, Cookie> AddedCookies { get; }

        public ReadOnlyDictionary<string, Cookie> RequestedCookies { get; private set; }

        public HttpCookie(string cookie)
        {
            if (cookie == null) throw new ArgumentNullException("Cookie");
            AddedCookies = new Dictionary<string, Cookie>();
            RequestedCookies = new ReadOnlyDictionary<string, Cookie>(new Dictionary<string, Cookie>());
            SetRequestCookieString(cookie);
        }

        public Cookie? Get(string name)
        {
            if (AddedCookies.TryGetValue(name, out Cookie cookie))
                return cookie;
            if (RequestedCookies.TryGetValue(name, out Cookie rcookie))
                return rcookie;
            return null;
        }

        public virtual void SetRequestCookieString(string cookie)
        {
            CompleteRequestCookie = cookie ?? throw new ArgumentNullException(nameof(cookie));
            AddedCookies.Clear();
            var reqCookie = new Dictionary<string, Cookie>();
            if (CompleteRequestCookie != "")
            {
                var tiles = CompleteRequestCookie.Split('&', ';');
                foreach (var tile in tiles)
                {
                    var ind = tile.IndexOf('=');
                    if (ind == -1)
                    {
                        var key = WebServerUtils.DecodeUri(tile.Trim());
                        if (!reqCookie.ContainsKey(key))
                            reqCookie.Add(key, new Cookie(key, ""));
                    }
                    else
                    {
                        var key = WebServerUtils.DecodeUri(tile.Remove(ind).Trim());
                        var value = ind + 1 == tile.Length ? "" : tile.Substring(ind + 1);
                        if (!reqCookie.ContainsKey(key))
                            reqCookie.Add(key, new Cookie(key, value));
                    }
                }
            }
            RequestedCookies = new ReadOnlyDictionary<string, Cookie>(reqCookie);
        }

        public override string ToString()
            => CompleteRequestCookie;
    }
}
