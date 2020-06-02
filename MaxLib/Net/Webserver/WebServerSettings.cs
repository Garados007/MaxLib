using MaxLib.Data.IniFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace MaxLib.Net.Webserver
{
    public class WebServerSettings
    {
        public int Port { get; private set; }

        public int ConnectionTimeout { get; private set; }

        IPAddress ipFilter = IPAddress.Any;
        public IPAddress IPFilter
        {
            get => ipFilter;
            set => ipFilter = value ?? throw new ArgumentNullException("IPFilter");
        }

        //Debug
        public bool Debug_WriteRequests = false;
        public bool Debug_LogConnections = false;

        public Dictionary<string, string> DefaultFileMimeAssociation { get; } = new Dictionary<string, string>();

        protected enum SettingTypes
        {
            MimeAssociation,
            ServerSettings
        }

        public string SettingsPath { get; private set; }

        public virtual void LoadSettingFromData(string data)
        {
            var sf = new OptionsLoader(data);
            var type = sf["Setting"].Options.GetEnum<SettingTypes>("Type");
            switch (type)
            {
                case SettingTypes.MimeAssociation: Load_Mime(sf); break;
                case SettingTypes.ServerSettings: Load_Server(sf); break;
            }
        }

        public virtual void LoadSetting(string path)
        {
            SettingsPath = path;
            var sf = new OptionsLoader(path, false);
            var type = sf["Setting"].Options.GetEnum<SettingTypes>("Type");
            switch (type)
            {
                case SettingTypes.MimeAssociation: Load_Mime(sf); break;
                case SettingTypes.ServerSettings: Load_Server(sf); break;
            }
        }

        protected virtual void Load_Mime(OptionsLoader set)
        {
            DefaultFileMimeAssociation.Clear();
            var gr = set["Mime"].Options.GetSearch().FilterKeys(true);
            foreach (var keypair in gr)
            {
                if (DefaultFileMimeAssociation.ContainsKey((keypair as OptionsKey).Name)) { }
                DefaultFileMimeAssociation.Add((keypair as OptionsKey).Name, (keypair as OptionsKey).GetString());
            }
        }

        protected virtual void Load_Server(OptionsLoader set)
        {
            var server = set["Server"].Options;
            Port = server.GetInt32("Port", 80);
            ConnectionTimeout = server.GetInt32("ConnectionTimeout", 2000);
        }

        public WebServerSettings(string settingFolderPath)
        {
            foreach (var file in Directory.GetFiles(settingFolderPath))
                if (file.EndsWith(".ini"))
                    LoadSetting(file);
        }

        public WebServerSettings(int port, int connectionTimeout)
        {
            Port = port;
            ConnectionTimeout = connectionTimeout;
        }
    }
}
