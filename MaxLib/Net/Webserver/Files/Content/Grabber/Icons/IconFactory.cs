using MaxLib.Net.Webserver.Files.Content.Grabber.Info;
using System;

namespace MaxLib.Net.Webserver.Files.Content.Grabber.Icons
{
    public class IconFactory
    {
        public IconFetcher All(params IconFetcher[] iconFetcher)
        {
            return IconFetcher.All(iconFetcher);
        }

        public IconFetcher Any(params IconFetcher[] iconFetcher)
        {
            return IconFetcher.Any(iconFetcher);
        }

        public IconFetcher While(IconFetcher iconFetcher)
        {
            return IconFetcher.While(iconFetcher);
        }

        public IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target)
        {
            return IconFetcher.Fallback(source, target, (c) => c.Icon.ContentId, true);
        }

        public IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target,
            Func<ContentInfo, string> newContentId)
        {
            return IconFetcher.Fallback(source, target, newContentId, true);
        }

        public IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target,
            bool result)
        {
            return IconFetcher.Fallback(source, target, (c) => c.Icon.ContentId, result);
        }

        public IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target,
            Func<ContentInfo, string> newContentId, bool result)
        {
            return IconFetcher.Fallback(source, target, newContentId, result);
        }
    }
}
