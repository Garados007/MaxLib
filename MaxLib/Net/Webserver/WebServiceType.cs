namespace MaxLib.Net.Webserver
{
    /// <summary>
    /// Definitiert den Webservice
    /// </summary>
    public enum WebServiceType
    {
        /// <summary>
        /// Diese Gruppe verarbeitet nur die Nachrichten, die hereingekommen sind. Dazu werden nur die Daten aus dem Request geparst.
        /// </summary>
        PreParseRequest = 1,
        /// <summary>
        /// Diese Gruppe verarbeitet die geparsten Anforderungen. Hier wird das weitere Vorgehen bestimmt.
        /// </summary>
        PostParseRequest = 2,
        /// <summary>
        /// Hier wird ein Dokument vorverarbeitet. Dazu wird nur das Dokument geladen und bereitgestellt. Mit dem Dokument selbst wird nicht 
        /// gearbeitet.
        /// </summary>
        PreCreateDocument = 3,
        /// <summary>
        /// Hier wird ein Dokument nachverarbeitet. Hier wird eventuell Code ausgeführt, die das Dokument verändern.
        /// </summary>
        PostCreateDocument = 4,
        /// <summary>
        /// Hier wird die Antwort vorbereitet. Dazu wird ein Header generiert. Dazu dürfen nur Informationen aus dem Request bezogen werden.
        /// </summary>
        PreCreateResponse = 5,
        /// <summary>
        /// Hier wird die Antwort fertig gestellt. Hier werden Informationen aus dem Dokument dem Header ergänzt.
        /// </summary>
        PostCreateResponse = 6,
        /// <summary>
        /// Sended die Nachricht ab. Das ist der letzte Teil einer Abfrage.
        /// </summary>
        SendResponse = 7
    }
}
