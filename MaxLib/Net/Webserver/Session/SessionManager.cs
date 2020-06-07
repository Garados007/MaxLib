using System;
using System.Collections.Generic;

namespace MaxLib.Net.Webserver.Session
{
    public static class SessionManager
    {
        static readonly List<SessionInformation> Sessions = new List<SessionInformation>();

        public static void Register(WebProgressTask task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));
            var cookie = task.Document.RequestHeader.Cookie.Get("Session");
            if (cookie == null)
            {
                var si = RegisterNewSession(task.Session);
                task.Session.PublicSessionKey = si.ByteKey;
                task.Session.AlwaysSyncSessionInformation(si.Information);
                task.Document.RequestHeader.Cookie.AddedCookies.Add("Session", new HttpCookie.Cookie("Session", si.HexKey, 3600));
            }
            else
            {
                if (!RegisterSession(task.Session, cookie.Value.ValueString))
                    task.Document.RequestHeader.Cookie.AddedCookies.Add("Session",
                        new HttpCookie.Cookie("Session", Get(task.Session.PublicSessionKey).HexKey, 3600));
            }
        }

        public static bool RegisterSession(HttpSession session, string hexkey)
        {
            _ = session ?? throw new ArgumentNullException(nameof(session));
            _ = hexkey ?? throw new ArgumentNullException(nameof(hexkey));
            return RegisterSession(session, Get(hexkey));
        }

        public static bool RegisterSession(HttpSession session, byte[] binkey)
        {
            _ = session ?? throw new ArgumentNullException(nameof(session));
            _ = binkey ?? throw new ArgumentNullException(nameof(binkey));
            return RegisterSession(session, Get(binkey));
        }

        static bool RegisterSession(HttpSession session, SessionInformation si)
        {
            var added = si != null;
            si = si ?? RegisterNewSession(session);
            session.PublicSessionKey = si.ByteKey;
            session.AlwaysSyncSessionInformation(si.Information);
            return !added;
        }

        public static SessionInformation RegisterNewSession(HttpSession session)
        {
            _ = session ?? throw new ArgumentNullException(nameof(session));
            var key = GenerateSessionKey(out byte[] bkey);
            session.PublicSessionKey = bkey;
            var si = new SessionInformation(key, bkey, DateTime.Now);
            Sessions.Add(si);
            return si;
        }

        public static SessionInformation Get(string hexkey)
        {
            return Sessions.Find((si) => si != null && si.HexKey == hexkey);
        }

        public static SessionInformation Get(byte[] binkey)
        {
            return Sessions.Find((si) => WebServerUtils.BytesEqual(si.ByteKey, binkey));
        }

        public static void DeleteSession(string hexkey)
        {
            var ind = Sessions.FindIndex((si) => si.HexKey == hexkey);
            if (ind != -1) Sessions.RemoveAt(ind);
        }

        public static void DeleteSession(byte[] binkey)
        {
            var ind = Sessions.FindIndex((si) => si.ByteKey == binkey);
            if (ind != -1) Sessions.RemoveAt(ind);
        }

        const string hex = "0123456789ABCDEF";

        static string GenerateSessionKey(out byte[] b)
        {
            var r = new Random();
            while (true)
            {
                b = new byte[16];
                r.NextBytes(b);
                var h = "";
                for (int i = 0; i < b.Length; ++i) h += hex[b[i] / 16].ToString() + hex[b[i] % 16].ToString();
                if (!Sessions.Exists((si) => si.HexKey == h)) return h;
            }
        }
    }
}
