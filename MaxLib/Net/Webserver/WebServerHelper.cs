using System;
using System.Linq;
using System.Net;

namespace MaxLib.Net.Webserver
{
    public static class WebServerHelper
    {
        public static string EncodeUri(string uri)
        {
            return WebUtility.UrlEncode(uri);
        }

        public static string DecodeUri(string uri)
        {
            return WebUtility.UrlDecode(uri);
        }

        public static string GetVolumeString(long byteCount, bool shortVersion, int digits)
        {
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
            if (step >= names.Length) throw new ArgumentOutOfRangeException("byteCount");
            var vkd = bc < 1000 ? bc < 100 ? bc < 10 ? 1 : 2 : 3 : 4;
            digits = Math.Max(Math.Min(digits, vkd + 3 * step), vkd);
            var mask = vkd == 4 ? "0,000" : new string('0', vkd);
            if (digits > vkd) mask += "." + new string('#', digits - vkd);
            return (bc.ToString(mask) + " " + names[step]).TrimEnd('.', ',');
        }

        public static string GetDateString(DateTime date)
        {
            var mn = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var format = "{0}, {1:00} {2} {3:0000} {4:00}:{5:00}:{6:00} GMT";
            var s = string.Format(format, date.DayOfWeek.ToString().Substring(0, 3),
                date.Day, mn[date.Month - 1], date.Year,
                date.Hour, date.Minute, date.Second);
            return s;
        }

        public static DateTime GetDateFromString(string date)
        {
            var tiles = date.Split(new char[] { ',', ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
            var min = DateTime.MinValue;
            int day = min.Day, month = min.Month, year = min.Year, hour = min.Hour, minute = min.Minute, second = min.Second;
            var mn = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" }.ToList();
            if (tiles.Length >= 2) int.TryParse(tiles[1], out day);
            if (tiles.Length >= 3) month = mn.IndexOf(tiles[2]);
            if (tiles.Length >= 4) int.TryParse(tiles[3], out year);
            if (tiles.Length >= 5) int.TryParse(tiles[4], out hour);
            if (tiles.Length >= 6) int.TryParse(tiles[5], out minute);
            if (tiles.Length >= 7) int.TryParse(tiles[6], out second);
            return new DateTime(year, month, day, hour, minute, second);
        }

        public static bool BytesEqual(byte[] ba1, byte[] ba2)
        {
            if (ba1 == null) throw new ArgumentNullException("ba1");
            if (ba2 == null) throw new ArgumentNullException("ba2");
            if (ba1.Length != ba2.Length) return false;
            for (int i = 0; i < ba1.Length; ++i) if (ba1[i] != ba2[i]) return false;
            return true;
        }
    }
}
