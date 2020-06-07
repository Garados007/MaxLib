using System;

namespace MaxLib.Net.Webserver
{
    /// <summary>
    /// Hier ist eine kleine Auswahl der MIME-Typen.
    /// </summary>
    public static class MimeType
    {
        /// <summary>
        /// Microsoft Excel Dateien (*.xls *.xla)
        /// </summary>
        public const string ApplicationMsexcel = "application/msexcel";
        /// <summary>
        /// Microsoft Powerpoint Dateien (*.ppt *.ppz *.pps *.pot)
        /// </summary>
        public const string ApplicationMspowerpoint = "application/mspowerpoint";
        /// <summary>
        /// Microsoft Word Dateien (*.doc *.dot)
        /// </summary>
        public const string ApplicationMsword = "application/msword";
        /// <summary>
        /// GNU Zip-Dateien (*.gz)
        /// </summary>
        public const string ApplicationGzip = "application/gzip";
        /// <summary>
        /// JSON Dateien (*.json)
        /// </summary>
        public const string ApplicationJson = "application/json";
        /// <summary>
        /// Nicht näher spezifizierte Daten, z.B. ausführbare Dateien (*.bin *.exe *.com *.dll *.class)
        /// </summary>
        public const string ApplicationOctetStream = "application/octet-stream";
        /// <summary>
        /// PDF-Dateien (*.pdf)
        /// </summary>
        public const string ApplicationPdf = "application/pdf";
        /// <summary>
        /// RTF-Dateien (*.rtf)
        /// </summary>
        public const string ApplicationRtf = "application/rtf";
        /// <summary>
        /// XHTML-Dateien (*.htm *.html *.shtml *.xhtml)
        /// </summary>
        public const string ApplicationXhtml = "application/xhtml+xml";
        /// <summary>
        /// XML-Dateien (*.xml)
        /// </summary>
        public const string ApplicationXml = "application/xml";
        /// <summary>
        /// PHP-Dateien (*.php *.phtml)
        /// </summary>
        public const string ApplicationPhp = "application/x-httpd-php";
        /// <summary>
        /// serverseitige JavaScript-Dateien (*.js)
        /// </summary>
        public const string ApplicationJs = "application/x-javascript";
        /// <summary>
        /// ZIP-Archivdateien (*.zip)
        /// </summary>
        public const string ApplicationZip = "application/zip";
        /// <summary>
        /// MPEG-Audiodateien (*.mp2)
        /// </summary>
        public const string AudioMpeg = "audio/x-mpeg";
        /// <summary>
        /// WAV-Dateien (*.wav)
        /// </summary>
        public const string AudioWav = "audio/x-wav";
        /// <summary>
        /// GIF-Dateien (*.gif)
        /// </summary>
        public const string ImageGif = "image/gif";
        /// <summary>
        /// JPEG-Dateien (*.jpeg *.jpg *.jpe)
        /// </summary>
        public const string ImageJpeg = "image/jpeg";
        /// <summary>
        /// PNG-Dateien (*.png *.pneg)
        /// </summary>
        public const string ImagePng = "image/png";
        /// <summary>
        /// Icon-Dateien (z.B. Favoriten-Icons) (*.ico)
        /// </summary>
        public const string ImageIcon = "image/x-icon";
        /// <summary>
        /// mehrteilige Daten; jeder Teil ist eine zu den anderen gleichwertige Alternative 
        /// </summary>
        public const string MultipartAlternative = "multipart/alternative";
        /// <summary>
        /// mehrteilige Daten mit Byte-Angaben 
        /// </summary>
        public const string MultipartByteranges = "multipart/byteranges";
        /// <summary>
        /// mehrteilige Daten verschlüsselt 
        /// </summary>
        public const string MultipartEncrypted = "multipart/encrypted";
        /// <summary>
        /// mehrteilige Daten aus HTML-Formular (z.B. File-Upload) 
        /// </summary>
        public const string MultipartFormData = "multipart/form-Data";
        /// <summary>
        /// mehrteilige Daten ohne Bezug der Teile untereinander 
        /// </summary>
        public const string MultipartMixed = "multipart/mixed";
        /// <summary>
        /// CSS Stylesheet-Dateien (*.css)
        /// </summary>
        public const string TextCss = "text/css";
        /// <summary>
        /// HTML-Dateien (*.htm *.html *.shtml)
        /// </summary>
        public const string TextHtml = "text/html";
        /// <summary>
        /// JavaScript-Dateien (*.js)
        /// </summary>
        public const string TextJs = "text/javascript";
        /// <summary>
        /// reine Textdateien (*.txt)
        /// </summary>
        public const string TextPlain = "text/plain";
        /// <summary>
        /// RTF-Dateien (*.rtf)
        /// </summary>
        public const string TextRtf = "text/rtf";
        /// <summary>
        /// XML-Dateien (*.xml)
        /// </summary>
        public const string TextXml = "text/xml";
        /// <summary>
        /// MPEG-Videodateien (*.mpeg *.mpg *.mpe)
        /// </summary>
        public const string VideoMpeg = "video/mpeg";
        /// <summary>
        /// Microsoft AVI-Dateien (*.avi)
        /// </summary>
        public const string VideoAvi = "video/x-msvideo";
        /// <summary>
        /// Checks if the mime type matches the pattern.
        /// </summary>
        /// <param name="mime">the mime type</param>
        /// <param name="pattern">
        /// the pattern in the same format like the mime type. It can contains * as
        /// placeholder (e.g. "text/plain" matches "text/plain", "text/*", "*/plain" and "*/*")
        /// </param>
        /// <returns>true if mime matches pattern</returns>
        public static bool Check(string mime, string pattern)
        {
            _ = mime ?? throw new ArgumentNullException(nameof(mime));
            _ = pattern ?? throw new ArgumentNullException(nameof(pattern));
            var ind = mime.IndexOf('/');
            if (ind == -1) 
                throw new ArgumentException("no Mime", nameof(mime));
            var ml = mime.Remove(ind).ToLower();
            var mh = mime.Substring(ind + 1).ToLower();
            ind = pattern.IndexOf('/');
            if (ind == -1) 
                throw new ArgumentException("no Mime", nameof(pattern));
            var pl = pattern.Remove(ind).ToLower();
            var ph = pattern.Substring(ind + 1).ToLower();
            return (pl == "*" || pl == ml) && (ph == "*" || ph == mh);
        }
    }
}
