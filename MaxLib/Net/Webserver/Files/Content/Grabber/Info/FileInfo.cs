﻿using MaxLib.Net.Webserver.Files.Content.Grabber.Icons;
using IO = System.IO;

namespace MaxLib.Net.Webserver.Files.Content.Grabber.Info
{
    public abstract class FileInfo : ContentInfo
    {
        public override ContentType Type => ContentType.File;

        public abstract long Length { get; }

        public string MimeType { get; protected set; }

        public abstract string Extension { get; } //with dot

        public abstract IO.Stream GetStream();

        public FileInfo()
        {
            Icon.Type = IconInfo.ContentIdType.DetectFile;
            MimeType = Webserver.MimeType.ApplicationOctetStream;
        }
    }
}
