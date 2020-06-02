namespace MaxLib.Net.Webserver.Files.Content.Grabber.Icons
{
    public class IconInfo
    {
        public string ContentId { get; set; }

        public ContentIdType Type { get; set; }

        public IconInfo()
        {
            Type = ContentIdType.None;
        }

        public enum ContentIdType
        {
            None = 0,
            CommonDirectory = 1,
            CommonFile = 2,
            IcoFile = 3,
            IcoInBinFile = 4,
            ImgFile = 5,
            UnknownFile = 6,
            DetectDirectory = 7,
            DetectFile = 8,
            Url = 9,
            /// <summary>
            /// Special Flag for extra states. If you want to define an extra state the 4 lowest bits must be 1.
            /// (collision protection)
            /// </summary>
            Special = 0x0f,
        }
    }
}
