namespace MaxLib.Net.Webserver
{
    public enum HttpStateCode
    {
        /// <summary>
        /// 100 - Die laufende Anfrage an den Server wurde noch nicht zurückgewiesen. 
        /// (Wird im Zusammenhang mit dem „Expect 100-continue“-Header-Feld verwendet.) 
        /// Der Client kann nun mit der potentiell sehr großen Anfrage fortfahren.
        /// </summary>
        Continue = 100,
        /// <summary>
        /// 101 - Wird verwendet, wenn der Server eine Anfrage mit gesetztem 
        /// „Upgrade“-Header-Feld empfangen hat und mit dem Wechsel zu einem anderen 
        /// Protokoll einverstanden ist. Anwendung findet dieser Status-Code beispielsweise 
        /// im Wechsel von HTTP zu WebSocket.
        /// </summary>
        SwitchingProtocols = 101,
        /// <summary>
        /// 102 - Wird verwendet, um ein Timeout zu vermeiden, während der Server eine 
        /// zeitintensive Anfrage bearbeitet.
        /// </summary>
        Processing = 102,
        /// <summary>
        /// 200 - Die Anfrage wurde erfolgreich bearbeitet und das Ergebnis der Anfrage 
        /// wird in der Antwort übertragen.
        /// </summary>
        OK = 200,
        /// <summary>
        /// 201 - Die Anfrage wurde erfolgreich bearbeitet. Die angeforderte Ressource 
        /// wurde vor dem Senden der Antwort erstellt. Das „Location“-Header-Feld enthält 
        /// eventuell die Adresse der erstellten Ressource.
        /// </summary>
        Created = 201,
        /// <summary>
        /// 202 - Die Anfrage wurde akzeptiert, wird aber zu einem späteren Zeitpunkt 
        /// ausgeführt. Das Gelingen der Anfrage kann nicht garantiert werden.
        /// </summary>
        Accepted = 202,
        /// <summary>
        /// 203 - Die Anfrage wurde bearbeitet, das Ergebnis ist aber nicht unbedingt 
        /// vollständig und aktuell.
        /// </summary>
        NonAuthoritativeInformation = 203,
        /// <summary>
        /// 204 - Die Anfrage wurde erfolgreich durchgeführt, die Antwort enthält 
        /// jedoch bewusst keine Daten.
        /// </summary>
        NoContent = 204,
        /// <summary>
        /// 205 - Die Anfrage wurde erfolgreich durchgeführt; der Client soll das 
        /// Dokument neu aufbauen und Formulareingaben zurücksetzen.
        /// </summary>
        ResetContent = 205,
        /// <summary>
        /// 206 - Der angeforderte Teil wurde erfolgreich übertragen (wird im 
        /// Zusammenhang mit einem „Content-Range“-Header-Feld oder dem Content-Type 
        /// multipart/byteranges verwendet). Kann einen Client über Teil-Downloads 
        /// informieren (wird zum Beispiel von Wget genutzt, um den Downloadfortschritt 
        /// zu überwachen oder einen Download in mehrere Streams aufzuteilen).
        /// </summary>
        PartialContent = 206,
        /// <summary>
        /// 207 - Die Antwort enthält ein XML-Dokument, das mehrere Statuscodes zu 
        /// unabhängig voneinander durchgeführten Operationen enthält.
        /// </summary>
        MultiStatus = 207,
        /// <summary>
        /// 208 - WebDAV RFC 5842 – Die Mitglieder einer WebDAV-Bindung wurden bereits 
        /// zuvor aufgezählt und sind in dieser Anfrage nicht mehr vorhanden.
        /// </summary>
        AlreadyReported = 208,
        /// <summary>
        /// 226 - RFC 3229 – Der Server hat eine GET-Anforderung für die Ressource 
        /// erfüllt, die Antwort ist eine Darstellung des Ergebnisses von einem oder 
        /// mehreren Instanz-Manipulationen, bezogen auf die aktuelle Instanz.
        /// </summary>
        IMUsed = 226,
        /// <summary>
        /// 300 - Die angeforderte Ressource steht in verschiedenen Arten zur Verfügung. 
        /// Die Antwort enthält eine Liste der verfügbaren Arten. Das „Location“-Header-Feld 
        /// enthält eventuell die Adresse der vom Server bevorzugten Repräsentation.
        /// </summary>
        MultipleChoises = 300,
        /// <summary>
        /// 301 - Die angeforderte Ressource steht ab sofort unter der im 
        /// „Location“-Header-Feld angegebenen Adresse bereit (auch Redirect genannt). 
        /// Die alte Adresse ist nicht länger gültig.
        /// </summary>
        MovedPermanently = 301,
        /// <summary>
        /// 302 - Die angeforderte Ressource steht vorübergehend unter der im 
        /// „Location“-Header-Feld angegebenen Adresse bereit. Die alte Adresse 
        /// bleibt gültig. Die Browser folgen meist mit einem GET, auch wenn der 
        /// ursprüngliche Request ein POST war. Wird in HTTP/1.1 je nach Anwendungsfall 
        /// durch die Statuscodes 303 bzw. 307 ersetzt. 
        /// </summary>
        Found = 302,
        /// <summary>
        /// 303 - Die Antwort auf die durchgeführte Anfrage lässt sich unter der im 
        /// „Location“-Header-Feld angegebenen Adresse beziehen. Der Browser soll mit 
        /// einem GET folgen, auch wenn der ursprüngliche Request ein POST war.
        /// </summary>
        SeeOther = 303,
        /// <summary>
        /// 304 - Der Inhalt der angeforderten Ressource hat sich seit der letzten 
        /// Abfrage des Clients nicht verändert und wird deshalb nicht übertragen. 
        /// Zu den Einzelheiten siehe Browser-Cache-Versionsvergleich.
        /// </summary>
        NotModified = 304,
        /// <summary>
        /// 305 - Die angeforderte Ressource ist nur über einen Proxy erreichbar. 
        /// Das „Location“-Header-Feld enthält die Adresse des Proxy.
        /// </summary>
        UseProxy = 305,
        //306 ist reserviert und wird nicht mehr verwendet.
        /// <summary>
        /// 307 - Die angeforderte Ressource steht vorübergehend unter der im 
        /// „Location“-Header-Feld angegebenen Adresse bereit. Die alte Adresse 
        /// bleibt gültig. Der Browser soll mit derselben Methode folgen wie beim 
        /// ursprünglichen Request (d. h. einem POST folgt ein POST). Dies ist der 
        /// wesentliche Unterschied zu 302/303.
        /// </summary>
        TemporaryRedirect = 307,
        /// <summary>
        /// 308 - Experimentell eingeführt via RFC; die angeforderte Ressource steht ab 
        /// sofort unter der im „Location“-Header-Feld angegebenen Adresse bereit, die 
        /// alte Adresse ist nicht länger gültig. Der Browser soll mit derselben Methode 
        /// folgen wie beim ursprünglichen Request (d. h. einem POST folgt ein POST). Dies 
        /// ist der wesentliche Unterschied zu 302/303.
        /// </summary>
        PermanentRedirect = 308,
        /// <summary>
        /// 400 - Die Anfrage-Nachricht war fehlerhaft aufgebaut.
        /// </summary>
        BadRequest = 400,
        /// <summary>
        /// 401 - Die Anfrage kann nicht ohne gültige Authentifizierung durchgeführt 
        /// werden. Wie die Authentifizierung durchgeführt werden soll, wird im 
        /// „WWW-Authenticate“-Header-Feld der Antwort übermittelt.
        /// </summary>
        Unauthorized = 401,
        /// <summary>
        /// 402 - Übersetzt: Bezahlung benötigt. Dieser Status ist für zukünftige 
        /// HTTP-Protokolle reserviert.
        /// </summary>
        PaymentRequired = 402,
        /// <summary>
        /// 403 - Die Anfrage wurde mangels Berechtigung des Clients nicht durchgeführt. 
        /// Diese Entscheidung wurde – anders als im Fall des Statuscodes 401 – unabhängig 
        /// von Authentifizierungsinformationen getroffen, auch etwa wenn eine als HTTPS 
        /// konfigurierte URL nur mit HTTP aufgerufen wurde.
        /// </summary>
        Forbidden = 403,
        /// <summary>
        /// 404 - Die angeforderte Ressource wurde nicht gefunden. Dieser Statuscode 
        /// kann ebenfalls verwendet werden, um eine Anfrage ohne näheren Grund abzuweisen. 
        /// Links, welche auf solche Fehlerseiten verweisen, werden auch als Tote Links 
        /// bezeichnet.
        /// </summary>
        NotFound = 404,
        /// <summary>
        /// 405 - Die Anfrage darf nur mit anderen HTTP-Methoden (zum Beispiel GET statt 
        /// POST) gestellt werden. Gültige Methoden für die betreffende Ressource werden 
        /// im „Allow“-Header-Feld der Antwort übermittelt.
        /// </summary>
        MethodNotAllowed = 405,
        /// <summary>
        /// 406 - Die angeforderte Ressource steht nicht in der gewünschten Form zur 
        /// Verfügung. Gültige „Content-Type“-Werte können in der Antwort übermittelt werden.
        /// </summary>
        NotAcceptable = 406,
        /// <summary>
        /// 407 - Analog zum Statuscode 401 ist hier zunächst eine Authentifizierung des 
        /// Clients gegenüber dem verwendeten Proxy erforderlich. Wie die Authentifizierung 
        /// durchgeführt werden soll, wird im „Proxy-Authenticate“-Header-Feld der Antwort 
        /// übermittelt.
        /// </summary>
        ProxyAuthenticationRequired = 407,
        /// <summary>
        /// 408 - Innerhalb der vom Server erlaubten Zeitspanne wurde keine vollständige 
        /// Anfrage des Clients empfangen.
        /// </summary>
        RequestTimeOut = 408,
        /// <summary>
        /// 409 - Die Anfrage wurde unter falschen Annahmen gestellt. Im Falle einer 
        /// PUT-Anfrage kann dies zum Beispiel auf eine zwischenzeitliche Veränderung 
        /// der Ressource durch Dritte zurückgehen.
        /// </summary>
        Conflict = 409,
        /// <summary>
        /// 410 - Die angeforderte Ressource wird nicht länger bereitgestellt und wurde 
        /// dauerhaft entfernt.
        /// </summary>
        Gone = 410,
        /// <summary>
        /// 411 - Die Anfrage kann ohne ein „Content-Length“-Header-Feld nicht bearbeitet 
        /// werden.
        /// </summary>
        LengthRequired = 411,
        /// <summary>
        /// 412 - Eine in der Anfrage übertragene Voraussetzung, zum Beispiel in Form 
        /// eines „If-Match“-Header-Felds, traf nicht zu.
        /// </summary>
        PreconditionFailed = 412,
        /// <summary>
        /// 413 - Die gestellte Anfrage war zu groß, um vom Server bearbeitet werden zu 
        /// können. Ein „Retry-After“-Header-Feld in der Antwort kann den Client darauf 
        /// hinweisen, dass die Anfrage eventuell zu einem späteren Zeitpunkt bearbeitet 
        /// werden könnte.
        /// </summary>
        RequestEntityTooLarge = 413,
        /// <summary>
        /// 414 - Die URL der Anfrage war zu lang. Ursache ist oft eine Endlosschleife 
        /// aus Redirects.
        /// </summary>
        RequestUrlTooLong = 414,
        /// <summary>
        /// 415 - Der Inhalt der Anfrage wurde mit ungültigem oder nicht erlaubtem 
        /// Medientyp übermittelt.
        /// </summary>
        UnsupportedMediaType = 415,
        /// <summary>
        /// 416 - Der angeforderte Teil einer Ressource war ungültig oder steht auf 
        /// dem Server nicht zur Verfügung.
        /// </summary>
        RequestedRangeNotSatisfiable = 416,
        /// <summary>
        /// 417 - Verwendet im Zusammenhang mit einem „Expect“-Header-Feld. Das im 
        /// „Expect“-Header-Feld geforderte Verhalten des Servers kann nicht erfüllt 
        /// werden.
        /// </summary>
        ExpectationFailed = 417,
        /// <summary>
        /// 418 - Dieser Code ist als Aprilscherz der IETF zu verstehen, welcher 
        /// näher unter RFC 2324, Hyper Text Coffee Pot Control Protocol, beschrieben 
        /// ist. Innerhalb eines scherzhaften Protokolls zum Kaffeekochen zeigt er an, 
        /// dass fälschlicherweise eine Teekanne anstatt einer Kaffeekanne verwendet wurde. 
        /// Dieser Statuscode ist allerdings kein Bestandteil von HTTP, sondern lediglich 
        /// von HTCPCP (Hyper Text Coffee Pot Control Protocol). Trotzdem ist dieser 
        /// Scherz-Statuscode auf einigen Webseiten zu finden, real wird aber der 
        /// Statuscode 200 gesendet.
        /// </summary>
        ImATeapot = 418,
        /// <summary>
        /// 420 - In W3C PEP (Working Draft 21. November 1997) wird dieser Code 
        /// vorgeschlagen, um mitzuteilen, dass eine Bedingung nicht erfüllt wurde.
        /// </summary>
        PolicyNotFulfilled = 420,
        /// <summary>
        /// 421 - Verwendet, wenn die Verbindungshöchstzahl überschritten wird. 
        /// Ursprünglich wurde dieser Code in W3C PEP (Working Draft 21. November 
        /// 1997) vorgeschlagen, um auf den Fehler „Bad Mapping“ hinzuweisen.
        /// </summary>
        ThereAreTooManyConnectionsFromYourInternetAddress = 421,
        /// <summary>
        /// 422 - Verwendet, wenn weder die Rückgabe von Statuscode 415 noch 400 
        /// gerechtfertigt wäre, eine Verarbeitung der Anfrage jedoch zum Beispiel 
        /// wegen semantischer Fehler abgelehnt wird.
        /// </summary>
        UnprocessableEntity = 422,
        /// <summary>
        /// 423 - Die angeforderte Ressource ist zurzeit gesperrt.
        /// </summary>
        Locked = 423,
        /// <summary>
        /// 424 - Die Anfrage konnte nicht durchgeführt werden, weil sie das 
        /// Gelingen einer vorherigen Anfrage voraussetzt.
        /// </summary>
        FailedDependency = 424,
        /// <summary>
        /// 425 - In den Entwürfen von WebDav Advanced Collections definiert, aber 
        /// nicht im „Web Distributed Authoring and Versioning (WebDAV) Ordered 
        /// Collections Protocol“.
        /// </summary>
        UnorderedCollection = 425,
        /// <summary>
        /// 426 - Der Client sollte auf Transport Layer Security (TLS/1.0) umschalten.
        /// </summary>
        UpgradeRequired = 426,
        /// <summary>
        /// 428 - Für die Anfrage sind nicht alle Vorbedingungen erfüllt gewesen. 
        /// Dieser Statuscode soll Probleme durch Race Conditions verhindern, indem 
        /// eine Manipulation oder Löschen nur erfolgt, wenn der Client dies auf Basis 
        /// einer aktuellen Ressource anfordert (Beispielsweise durch Mitliefern eines 
        /// aktuellen ETag-Header).
        /// </summary>
        PreconditionRequired = 428,
        /// <summary>
        /// 429 - Der Client hat zu viele Anfragen in einem bestimmten Zeitraum gesendet.
        /// </summary>
        TooManyRequests = 429,
        /// <summary>
        /// 430 - Die Maximallänge eines Headerfelds oder des Gesamtheaders wurde 
        /// überschritten.
        /// </summary>
        RequestHeaderFieldsTooLarge = 430,
        /// <summary>
        /// 500 - Dies ist ein „Sammel-Statuscode“ für unerwartete Serverfehler.
        /// </summary>
        InternalServerError = 500,
        /// <summary>
        /// 501 - Die Funktionalität, um die Anfrage zu bearbeiten, wird von diesem 
        /// Server nicht bereitgestellt. Ursache ist zum Beispiel eine unbekannte oder 
        /// nicht unterstützte HTTP-Methode.
        /// </summary>
        NotImplemented = 501,
        /// <summary>
        /// 502 - Der Server konnte seine Funktion als Gateway oder Proxy nicht erfüllen, 
        /// weil er seinerseits eine ungültige Antwort erhalten hat.
        /// </summary>
        BadGateway = 502,
        /// <summary>
        /// 503 - Der Server steht temporär nicht zur Verfügung, zum Beispiel wegen 
        /// Überlastung oder Wartungsarbeiten. Ein „Retry-After“-Header-Feld in der 
        /// Antwort kann den Client auf einen Zeitpunkt hinweisen, zu dem die Anfrage 
        /// eventuell bearbeitet werden könnte.
        /// </summary>
        ServiceUnavaible = 503,
        /// <summary>
        /// 504 - Der Server konnte seine Funktion als Gateway oder Proxy nicht 
        /// erfüllen, weil er innerhalb einer festgelegten Zeitspanne keine Antwort 
        /// von seinerseits benutzten Servern oder Diensten erhalten hat.
        /// </summary>
        GatewayTimeOut = 504,
        /// <summary>
        /// 505 - Die benutzte HTTP-Version (gemeint ist die Zahl vor dem Punkt) wird 
        /// vom Server nicht unterstützt oder abgelehnt.
        /// </summary>
        HttpVersionNotSupported = 505,
        /// <summary>
        /// 506 - Die Inhaltsvereinbarung der Anfrage ergibt einen Zirkelbezug.
        /// </summary>
        VariantAlsoNegotiates = 506,
        /// <summary>
        /// 507 - Die Anfrage konnte nicht bearbeitet werden, weil der Speicherplatz 
        /// des Servers dazu zurzeit nicht mehr ausreicht
        /// </summary>
        InsufficientStorage = 507,
        /// <summary>
        /// 508 -  	Die Operation wurde nicht ausgeführt, weil die Ausführung in 
        /// eine Endlosschleife gelaufen wäre. Definiert in der Binding-Erweiterung 
        /// für WebDAV gemäß RFC 5842, weil durch Bindings zyklische Pfade zu
        /// WebDAV-Ressourcen entstehen können.
        /// </summary>
        LoopDetected = 508,
        /// <summary>
        /// 509 - Die Anfrage wurde verworfen, weil sonst die verfügbare Bandbreite 
        /// überschritten würde (inoffizielle Erweiterung einiger Server).
        /// </summary>
        BandwidthLimitExceeded = 509,
        /// <summary>
        /// 510 - Die Anfrage enthält nicht alle Informationen, die die angefragte 
        /// Server-Extension zwingend erwartet.
        /// </summary>
        NotExtended = 510,
    }
}
