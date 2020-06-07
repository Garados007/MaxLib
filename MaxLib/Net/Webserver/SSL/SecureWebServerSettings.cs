using MaxLib.Data.IniFiles;
using System.Security.Cryptography.X509Certificates;

namespace MaxLib.Net.Webserver.SSL
{
    public class SecureWebServerSettings : WebServerSettings
    {
        public int SecurePort { get; private set; }
        public bool EnableUnsafePort { get; set; } = true;

        public X509Certificate Certificate { get; set; }

        public SecureWebServerSettings(string settingFolderPath)
            : base(settingFolderPath)
        {
        }

        public SecureWebServerSettings(int port, int securePort, int connectionTimeout)
            : base(port, connectionTimeout)
        {
            SecurePort = securePort;
        }

        public SecureWebServerSettings(int securePort, int connectionTimeout)
            : base(80, connectionTimeout)
        {
            SecurePort = securePort;
            EnableUnsafePort = false;
        }

        protected override void Load_Server(OptionsLoader set)
        {
            base.Load_Server(set);
            var server = set["Server"].Options;
            SecurePort = server.GetInt32("SecurePort", 443);
            EnableUnsafePort = server.GetBool("EnableUnsafePort", true);
        }
    }
}
