namespace MaxLib.Net.Webserver
{
    public static class HttpProtocollMethod
    {
        /// <summary>
        /// ist die gebräuchlichste Methode. Mit ihr wird eine Ressource (zum 
        /// Beispiel eine Datei) unter Angabe eines URI vom Server angefordert. 
        /// Als Argumente in dem URI können also auch Inhalte zum Server übertragen 
        /// werden, allerdings soll laut Standard eine GET-Anfrage nur Daten abrufen 
        /// und sonst keine Auswirkungen haben (wie Datenänderungen auf dem Server 
        /// oder ausloggen). Die Länge des URIs ist je nach eingesetztem Server 
        /// begrenzt und sollte aus Gründen der Abwärtskompatibilität nicht länger 
        /// als 255 Bytes sein.
        /// </summary>
        public const string Get = "GET";
        /// <summary>
        /// schickt unbegrenzte, je nach physischer Ausstattung des eingesetzten 
        /// Servers, Mengen an Daten zur weiteren Verarbeitung zum Server, diese 
        /// werden als Inhalt der Nachricht übertragen und können beispielsweise 
        /// aus Name-Wert-Paaren bestehen, die aus einem HTML-Formular stammen. 
        /// Es können so neue Ressourcen auf dem Server entstehen oder bestehende 
        /// modifiziert werden. POST-Daten werden im Allgemeinen nicht von Caches 
        /// zwischengespeichert. Zusätzlich können bei dieser Art der Übermittlung 
        /// auch Daten wie in der GET-Methode an den URI gehängt werden.
        /// </summary>
        public const string Post = "POST";
        /// <summary>
        /// weist den Server an, die gleichen HTTP-Header wie bei GET, nicht jedoch 
        /// den Nachrichtenrumpf mit dem eigentlichen Dokumentinhalt zu senden. So 
        /// kann zum Beispiel schnell die Gültigkeit einer Datei im Browser-Cache 
        /// geprüft werden.
        /// </summary>
        public const string Head = "HEAD";
        /// <summary>
        /// dient dazu eine Ressource (zum Beispiel eine Datei) unter Angabe des 
        /// Ziel-URIs auf einen Webserver hochzuladen. 
        /// Es können so neue Ressourcen auf dem Server entstehen oder bestehende 
        /// modifiziert werden.
        /// </summary>
        public const string Put = "PUT";
        /// <summary>
        /// löscht die angegebene Ressource auf dem Server. Heute ist das, ebenso 
        /// wie PUT, kaum implementiert beziehungsweise in der Standardkonfiguration 
        /// von Webservern abgeschaltet. Beides erlangt jedoch mit RESTful Web 
        /// Services und der HTTP-Erweiterung WebDAV neue Bedeutung.
        /// </summary>
        public const string Delete = "DELETE";
        /// <summary>
        /// liefert die Anfrage so zurück, wie der Server sie empfangen hat. So kann 
        /// überprüft werden, ob und wie die Anfrage auf dem Weg zum Server verändert 
        /// worden ist – sinnvoll für das Debugging von Verbindungen.
        /// </summary>
        public const string Trace = "TRACE";
        /// <summary>
        /// liefert eine Liste der vom Server unterstützen Methoden und Merkmale.
        /// </summary>
        public const string Options = "OPTIONS";
        /// <summary>
        /// wird von Proxyservern implementiert, die in der Lage sind, 
        /// SSL-Tunnel zur Verfügung zu stellen.
        /// </summary>
        public const string Connect = "CONNECT";
        /// <summary>
        /// RFC 5789 definiert zusätzlich eine PATCH-Methode, um Ressourcen zu 
        /// modifizieren – in Abgrenzung zur PUT-Methode, deren Intention das 
        /// Hochladen der kompletten Ressource ist.
        /// </summary>
        public const string Patch = "PATCH";
    }
}
