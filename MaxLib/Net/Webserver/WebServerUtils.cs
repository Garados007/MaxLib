using System;
using System.Globalization;
using System.Net;

namespace MaxLib.Net.Webserver
{
    public static class WebServerUtils
    {
        public static string EncodeUri(string uri)
            => WebUtility.UrlEncode(uri);

        public static string DecodeUri(string uri)
            => WebUtility.UrlDecode(uri);

        public static string GetVolumeString(long byteCount, bool shortVersion, int digits)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            var sn = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            var ln = new[] { "Byte", "Kilobyte", "Megabyte", "Gigabyte", "Terabyte", "Petabyte", "Exabyte", "Zettabyte", "Yottabyte" };
            var names = shortVersion ? sn : ln;
            var step = 0;
            var bc = (double)byteCount;
            while (bc >= 1024)
            {
                step++;
                bc /= 1024;
            }
            if (step >= names.Length) 
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            var vkd = bc >= 1000 ? 4
                : bc >= 100 ? 3
                : bc >= 10 ? 2
                : 1;
            digits = Math.Max(Math.Min(digits, vkd + 3 * step), vkd);
            var mask = vkd == 4 ? "0,000" : new string('0', vkd);
            if (digits > vkd) mask += "." + new string('#', digits - vkd);
            return $"{bc.ToString(mask)} {names[step]}";
        }

        public static string GetDateString(DateTime date)
            => date.ToUniversalTime().ToString("r");

        public static DateTime GetDateFromString(string date)
            => DateTime.TryParse(date,
                CultureInfo.InvariantCulture.DateTimeFormat,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                out DateTime dateTime)
            ? dateTime 
            : new DateTime();
        

        public static bool BytesEqual(byte[] ba1, byte[] ba2)
        {
            _ = ba1 ?? throw new ArgumentNullException(nameof(ba1));
            _ = ba2 ?? throw new ArgumentNullException(nameof(ba2));
            if (ba1.Length != ba2.Length) 
                return false;
            for (int i = 0; i < ba1.Length; ++i) 
                if (ba1[i] != ba2[i]) 
                    return false;
            return true;
        }
    }
}
