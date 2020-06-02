using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Net.Webserver.Files
{
    public class ContentEnvironment : IDisposable
    {
        public List<ContentSource> Sources { get; private set; }

        public void Dispose()
        {
            Sources.ForEach((e) => e.Dispose());
        }

        bool Start(string[] needle, string[] haystack)
        {
            if (needle.Length > haystack.Length) return false;
            for (int i = 0; i < needle.Length; ++i)
                if (needle[i] != haystack[i])
                    return false;
            return true;
        }

        public ContentEnvironment()
        {
            Sources = new List<ContentSource>();
        }

        IEnumerable<ContentSource> GetSources(string[] url)
        {
            foreach (var s in Sources)
                if (Start(s.RootUrl, url))
                    yield return s;
        }

        IEnumerable<ContentSource> FilteredSources(string[] url)
        {
            var sources = GetSources(url).ToArray();
            if (sources.Any((s) => s.Strict))
            {
                var max = sources.Max((s) => s.Strict ? s.RootUrl.Length : 0);
                foreach (var s in sources)
                    if (s.RootUrl.Length >= max)
                        yield return s;
            }
            else foreach (var s in sources) yield return s;
        }

        public IEnumerable<ContentInfo> GetContents(string[] url, WebProgressTask task)
        {
            foreach (var s in FilteredSources(url))
            {
                var rp = new string[url.Length - s.RootUrl.Length];
                Array.Copy(url, s.RootUrl.Length, rp, 0, rp.Length);
                var c = s.TryGetContent(rp, task);
                if (c != null) yield return c;
            }
        }

        IEnumerable<Tuple<ContentSource, Tuple<ContentInfo, string[]>>> GetReverse(string localPath)
        {
            foreach (var s in Sources)
            {
                var c = s.ReverseSearch(localPath);
                if (c != null) yield return new Tuple<ContentSource, Tuple<ContentInfo, string[]>>(s, c);
            }
        }

        IEnumerable<Tuple<ContentSource, Tuple<ContentInfo, string[]>>> FilteredReverse(string localPath)
        {
            var sources = GetReverse(localPath).ToArray();
            if (sources.Any((s) => s.Item1.Strict))
            {
                var max = sources.Max((s) => s.Item1.Strict ? s.Item1.RootUrl.Length : 0);
                foreach (var s in sources)
                    if (s.Item1.RootUrl.Length >= max)
                        yield return s;
            }
            else foreach (var s in sources) yield return s;
        }

        public IEnumerable<Tuple<ContentInfo, string[]>> ReverseSearch(string localPath)
        {
            foreach (var s in FilteredReverse(localPath))
                yield return s.Item2;
        }
    }
}
