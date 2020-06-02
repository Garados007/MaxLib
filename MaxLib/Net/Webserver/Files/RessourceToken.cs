namespace MaxLib.Net.Webserver.Files
{
    public abstract class RessourceToken
    {
        /// <summary>
        /// The unique id of this ressource. Its provided by <see cref="SourceProvider"/>.
        /// </summary>
        public string Id { get; protected set; }

        /// <summary>
        /// The hash sum of this ressource.
        /// </summary>
        public byte[] Hash { get; protected set; }

        /// <summary>
        /// The Mime type of this ressource. If no mime-type is given, its guessed automaticly
        /// through the server config.
        /// </summary>
        public string Mime { get; protected set; }

        /// <summary>
        /// The local path of this ressource. If this class is created through 
        /// <see cref="SourceProvider.CreateTempRessource(string)"/> then this shows the target
        /// path of the temp file
        /// </summary>
        public string LocalPath { get; protected set; }

        /// <summary>
        /// The unique url of this ressource. Its only provided if <see cref="ContentReady"/>
        /// is true.
        /// </summary>
        public string Url { get; protected set; }

        /// <summary>
        /// Explains if the content is ready or not
        /// </summary>
        public bool ContentReady { get; protected set; }

        /// <summary>
        /// If this <see cref="RessourceToken"/> is created through 
        /// <see cref="SourceProvider.CreateTempRessource(string)"/> then this shows the 
        /// provided ressource handle. Otherwise its null.
        /// </summary>
        public string RessourceHandle { get; protected set; }

        /// <summary>
        /// Notifies the <see cref="SourceProvider"/> this content is ready. <see cref="SourceProvider"/>
        /// adds this file to its accessible ressources.
        /// </summary>
        public abstract void NotifyContentReady();

        /// <summary>
        /// Overrides the local <see cref="Mime"/>.
        /// </summary>
        /// <param name="mime">the new mime type</param>
        public abstract void SetMime(string mime);

        /// <summary>
        /// Discards the temporary file. Its only possible to call, when this class is created with
        /// <see cref="SourceProvider.CreateTempRessource(string)"/> and <see cref="NotifyContentReady"/>
        /// was never called.
        /// </summary>
        public abstract void Discard();
    }
}
