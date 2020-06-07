using MaxLib.Net.Webserver.Files.Content.Grabber.Icons;

namespace MaxLib.Net.Webserver.Files.Content.Grabber.Info
{
    public abstract class DirectoryInfo : ContentInfo
    {
        public override ContentType Type => ContentType.Directory;

        public abstract ContentInfo[] Contents { get; }

        public DirectoryInfo()
        {
            Icon.Type = IconInfo.ContentIdType.DetectDirectory;
        }
    }
}
