﻿using System;

namespace MaxLib.Net.Webserver.Files
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
            Func<ContentInfo, String> newContentId)
        {
            return IconFetcher.Fallback(source, target, newContentId, true);
        }

        public IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target,
            bool result)
        {
            return IconFetcher.Fallback(source, target, (c) => c.Icon.ContentId, result);
        }

        public IconFetcher Fallback(IconInfo.ContentIdType source, IconInfo.ContentIdType target,
            Func<ContentInfo, String> newContentId, bool result)
        {
            return IconFetcher.Fallback(source, target, newContentId, result);
        }
    }
}
