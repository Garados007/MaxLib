using System;

namespace MaxLib.Net.Webserver.Files
{
    public abstract class IconFetcher : IDisposable
    {
        public abstract void Dispose();

        public abstract bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task);

        public static IconFetcher All(params IconFetcher[] iconFetcher)
        {
            if (iconFetcher == null) throw new ArgumentNullException("iconFetcher");
            foreach (var e in iconFetcher) if (e == null) throw new ArgumentNullException("iconFetcher");
            return new AllClass() { List = iconFetcher };
        }

        public static IconFetcher Any(params IconFetcher[] iconFetcher)
        {
            if (iconFetcher == null) throw new ArgumentNullException("iconFetcher");
            foreach (var e in iconFetcher) if (e == null) throw new ArgumentNullException("iconFetcher");
            return new AnyClass() { List = iconFetcher };
        }

        public static IconFetcher While(IconFetcher iconFetcher)
        {
            if (iconFetcher == null) throw new ArgumentNullException("iconFetcher");
            return new WhileClass() { fetcher = iconFetcher };
        }

        public static IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target,
            Func<ContentInfo, String> newContentId, bool result)
        {
            return new FallbackClass()
            {
                source = source,
                target = target,
                contentId = newContentId ?? throw new ArgumentNullException("newContentId"),
                result = result
            };
        }

        public static IconFactory Factory => new IconFactory();

        class AllClass : IconFetcher
        {
            public IconFetcher[] List;

            public override void Dispose()
            {
                foreach (var e in List) e.Dispose();
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                var iconInfo = contentInfo.Icon;
                var id = iconInfo.ContentId;
                var type = iconInfo.Type;
                foreach (var e in List)
                    if (!e.TryGetIcon(contentInfo, provider, task))
                    {
                        iconInfo.ContentId = id;
                        iconInfo.Type = type;
                        return false;
                    }
                return List.Length != 0;
            }
        }

        class AnyClass : IconFetcher
        {
            public IconFetcher[] List;

            public override void Dispose()
            {
                foreach (var e in List) e.Dispose();
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                var iconInfo = contentInfo.Icon;
                var id = iconInfo.ContentId;
                var type = iconInfo.Type;
                foreach (var e in List)
                    if (e.TryGetIcon(contentInfo, provider, task))
                        return true;
                    else
                    {
                        iconInfo.ContentId = id;
                        iconInfo.Type = type;
                    }
                return false;
            }
        }

        class WhileClass : IconFetcher
        {
            public IconFetcher fetcher;

            public override void Dispose()
            {
                fetcher.Dispose();
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                bool result, any = false;
                do
                {
                    result = fetcher.TryGetIcon(contentInfo, provider, task);
                    any |= result;
                }
                while (result);
                return any;
            }
        }

        class FallbackClass : IconFetcher
        {
            public bool result;
            public IconInfo.ContentIdType source;
            public IconInfo.ContentIdType target;
            public Func<ContentInfo, String> contentId;

            public override void Dispose()
            {
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                if (contentInfo.Icon.Type != source) return result;
                contentInfo.Icon.ContentId = contentId(contentInfo);
                contentInfo.Icon.Type = target;
                return result;
            }
        }
    }
}
