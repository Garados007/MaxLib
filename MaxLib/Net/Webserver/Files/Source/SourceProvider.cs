using MaxLib.Net.Webserver.Files.Content.Grabber.Info;
using System;
using System.Linq;
using System.Text;

namespace MaxLib.Net.Webserver.Files.Source
{
    public abstract class SourceProvider : FileSystemService
    {
        readonly string prefix;

        public int DefaultIconSize { get; set; }

        public override bool CanDeliverySourceUrlForPath => true;

        public override bool CanDeliverySourceUrlForContent => true;

        public override string GetDeliverySourceUrl(string[] relativePath, ContentInfo content)
        {
            if (content != null && content.Type != ContentType.File) return null;
            var sb = new StringBuilder();
            sb.Append("/");
            sb.Append(string.Join("/", PathRoot));
            sb.Append("/");
            sb.Append(string.Join("/", relativePath));
            if (content != null)
            {
                sb.Append("/");
                sb.Append(content.Name);
            }
            return sb.ToString();
        }

        public SourceProvider(string[] pathRoot) : base(pathRoot)
        {
            DefaultIconSize = 64;
            prefix = "/" + string.Join("/", pathRoot.Select((s) => WebServerUtils.EncodeUri(s)).ToArray());
        }

        public sealed override bool CanWorkWith(WebProgressTask task)
        {
            return base.CanWorkWith(task);
        }

        public sealed override void ProgressTask(WebProgressTask task)
        {
            var tp = task.Document.RequestHeader.Location.DocumentPathTiles;
            var rp = new string[tp.Length - PathRoot.Length];
            Array.Copy(tp, PathRoot.Length, rp, 0, rp.Length);
            bool success;
            if (rp.Length > 0 && rp[0].StartsWith("@"))
            {
                var id = rp[0].Length > 1 ? rp[0].Substring(1) : "";
                success = GetRessource(id, task);
            }
            else if (rp.Length > 0 && rp[0].StartsWith("~"))
            {
                var code = string.Join("/", rp);
                code = code.Length > 1 ? code.Substring(1) : "";
                code = code.Replace(' ', '+');
                byte[] hash;
                try { hash = Convert.FromBase64String(code); }
                catch { hash = null; }
                success = hash != null && GetRessource(hash, task);
            }
            else
            {
                success = GetRessource(rp, task);
            }
            if (!success)
            {
                var r = task.Document.ResponseHeader;
                r.StatusCode = HttpStateCode.NotFound;
            }
        }

        protected string GetPath(string id)
        {
            return prefix + "/@" + id;
        }

        protected string GetPath(byte[] hash)
        {
            return prefix + "/~" + Convert.ToBase64String(hash);
        }

        protected string GetPath(string[] path)
        {
            return prefix + "/" +
                string.Join("/", path.Select((s) => WebServerUtils.EncodeUri(s)).ToArray());
        }

        protected abstract bool GetRessource(string id, WebProgressTask task);

        protected abstract bool GetRessource(byte[] hash, WebProgressTask task);

        protected abstract bool GetRessource(string[] path, WebProgressTask task);

        /// <summary>
        /// Notifies a ressource could be loaded in the next time.
        /// Returns a unique url for ressource access
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public abstract string NotifyRessource(string localPath);

        /// <summary>
        /// Notifies a specific ressource at a local path. It has a high possibility to
        /// be loaded next time. It returns a unique url for this ressource access depends
        /// on the content info.
        /// </summary>
        /// <param name="localPath">the local path where this ressource is stores</param>
        /// <param name="content">specify the content and provider</param>
        /// <returns>a unique url</returns>
        public abstract string NotifyRessource(string localPath, ContentInfo content);

        /// <summary>
        /// Creates a ressource handler for a new temporary file. After the file is successfully
        /// loaded the method <see cref="RessourceToken.NotifyContentReady()"/> should be called. 
        /// Then the ressource is accessible through this access.
        /// 
        /// If the content is already loaded, then <see cref="RessourceToken.ContentReady"/>
        /// is true.
        /// </summary>
        /// <param name="ressourceHandle">a unique ressource handle</param>
        /// <returns>a ressource handler</returns>
        public abstract RessourceToken CreateTempRessource(string ressourceHandle);
    }
}
