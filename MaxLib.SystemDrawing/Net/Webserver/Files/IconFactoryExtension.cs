using System;
using System.Drawing;
using IO = System.IO;

namespace MaxLib.Net.Webserver.Files
{
    public static class IconFactoryExtension
    {
        /// <summary>
        /// Works with <see cref="IconInfo.ContentIdType.ImgFile"/>. Prepares the img file.
        /// </summary>
        /// <param name="fact"></param>
        /// <returns></returns>
        public static IconFetcher ImgFile(this IconFactory fact)
        {
            _ = fact ?? throw new ArgumentNullException(nameof(fact));
            return new ImgFileClass();
        }

        /// <summary>
        /// Works with <see cref="IconInfo.ContentIdType.IcoFile"/>. Prepares the ico file.
        /// </summary>
        /// <param name="fact"></param>
        /// <returns></returns>
        public static IconFetcher IconFile(this IconFactory fact)
        {
            _ = fact ?? throw new ArgumentNullException(nameof(fact));
            return new IconFileClass();
        }

        /// <summary>
        /// Works with <see cref="IconInfo.ContentIdType.UnknownFile"/>. 
        /// Fetch the ico from system using the extension of file.
        /// </summary>
        /// <param name="fact"></param>
        /// <returns></returns>
        public static IconFetcher IconFromUnknown(this IconFactory fact)
        {
            _ = fact ?? throw new ArgumentNullException(nameof(fact));
            return new IconFromUnknownClass();
        }

        /// <summary>
        /// Works with <see cref="IconInfo.ContentIdType.DetectFile"/>.
        /// Detect ico mode from extension.
        /// </summary>
        /// <param name="fact"></param>
        /// <returns></returns>
        public static IconFetcher FileDivider(this IconFactory fact)
        {
            _ = fact ?? throw new ArgumentNullException(nameof(fact));
            return new FileDividerClass();
        }

        class ImgFileClass : IconFetcher
        {
            public override void Dispose()
            {
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                var iconInfo = contentInfo.Icon;
                if (iconInfo.Type != IconInfo.ContentIdType.ImgFile) return false;
                if (!IO.File.Exists(iconInfo.ContentId)) return false;
                var token = provider.CreateTempRessource(iconInfo.ContentId);
                if (token.ContentReady)
                {
                    contentInfo.Icon.ContentId = token.Url;
                    contentInfo.Icon.Type = IconInfo.ContentIdType.Url;
                    return true;
                }
                try
                {
                    using (var img = new Bitmap(iconInfo.ContentId))
                    {
                        float max = provider.DefaultIconSize;
                        var v = Math.Min(max / img.Width, max / img.Height);
                        using (var thumb = img.GetThumbnailImage((int)(v * img.Width), (int)(v * img.Height), null, IntPtr.Zero))
                        {
                            thumb.Save(token.LocalPath);
                        }
                    }
                }
                catch
                {
                    token.Discard();
                    return false;
                }
                if (task.Server.Settings.DefaultFileMimeAssociation.TryGetValue(
                    new IO.FileInfo(iconInfo.ContentId).Extension.ToLower(), out string mime))
                {
                    token.SetMime(mime);
                }
                token.NotifyContentReady();
                iconInfo.ContentId = token.Url;
                iconInfo.Type = IconInfo.ContentIdType.Url;
                return true;
            }
        }

        class IconFileClass : IconFetcher
        {
            public override void Dispose()
            {
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                var iconInfo = contentInfo.Icon;
                if (iconInfo.Type != IconInfo.ContentIdType.IcoFile) return false;
                if (!IO.File.Exists(iconInfo.ContentId)) return false;
                var token = provider.CreateTempRessource(iconInfo.ContentId);
                try
                {
                    using (var icon = new Icon(iconInfo.ContentId, provider.DefaultIconSize, provider.DefaultIconSize))
                    using (var img = icon.ToBitmap())
                    {
                        img.Save(token.LocalPath);
                    }
                }
                catch
                {
                    token.Discard();
                    return false;
                }
                if (task.Server.Settings.DefaultFileMimeAssociation.TryGetValue(
                    new IO.FileInfo(iconInfo.ContentId).Extension.ToLower(), out string mime))
                {
                    token.SetMime(mime);
                }
                token.NotifyContentReady();
                iconInfo.ContentId = token.Url;
                iconInfo.Type = IconInfo.ContentIdType.Url;
                return true;
            }
        }

        class IconFromUnknownClass : IconFetcher
        {
            public override void Dispose()
            {
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                var iconInfo = contentInfo.Icon;
                if (iconInfo.Type != IconInfo.ContentIdType.UnknownFile) return false;
                if (contentInfo.Type != ContentType.File) return false;
                var file = contentInfo as FileInfo;
                var token = provider.CreateTempRessource("extension:"+file.Extension);
                if (token.ContentReady)
                {
                    iconInfo.ContentId = token.Url;
                    iconInfo.Type = IconInfo.ContentIdType.Url;
                    return true;
                }
                if (!IO.File.Exists(iconInfo.ContentId))
                {
                    token.Discard();
                    return false;
                }
                try
                {
                    using (var icon = Icon.ExtractAssociatedIcon(iconInfo.ContentId))
                    using (var img = icon.ToBitmap())
                    {
                        img.Save(token.LocalPath, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                catch
                {
                    token.Discard();
                    return false;
                }
                token.SetMime(MimeTypes.ImagePng);
                token.NotifyContentReady();
                iconInfo.ContentId = token.Url;
                iconInfo.Type = IconInfo.ContentIdType.Url;
                return true;
            }
        }

        class FileDividerClass : IconFetcher
        {
            public override void Dispose()
            {
            }

            public override bool TryGetIcon(ContentInfo contentInfo, SourceProvider provider, WebProgressTask task)
            {
                if (contentInfo.Icon.Type != IconInfo.ContentIdType.DetectFile) return false;
                if (contentInfo.Type != ContentType.File) return false;
                var file = contentInfo as FileInfo;
                contentInfo.Icon.ContentId = file.LocalPath;
                switch (file.Extension.ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".pneg":
                    case ".bmp":
                    case ".gif":
                    case ".tiff":
                        contentInfo.Icon.Type = IconInfo.ContentIdType.ImgFile;
                        return true;
                    case ".ico":
                        contentInfo.Icon.Type = IconInfo.ContentIdType.IcoFile;
                        return true;
                    case ".exe":
                        contentInfo.Icon.Type = IconInfo.ContentIdType.IcoInBinFile;
                        return true;
                    default:
                        contentInfo.Icon.Type = IconInfo.ContentIdType.UnknownFile;
                        return true;
                }
            }
        }
    }
}
