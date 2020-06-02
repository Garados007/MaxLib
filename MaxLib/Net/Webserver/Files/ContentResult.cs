namespace MaxLib.Net.Webserver.Files
{
    public class ContentResult
    {
        public ContentInfo[] Infos { get; set; }

        public string UrlName { get; set; }

        public string FirstRessourceName { get; set; }

        public string CurrentUrl { get; set; }

        public string ParentUrl { get; set; }

        public string[] CurrentDir { get; set; }

        public string DirRoot { get; set; }
    }
}
