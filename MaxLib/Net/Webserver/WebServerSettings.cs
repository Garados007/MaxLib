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
            set => ipFilter = value ?? throw new ArgumentNullException(nameof(IPFilter));
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
            _ = data ?? throw new ArgumentNullException(nameof(data));
            var sf = new OptionsLoader(data);
            if (sf["Mime"] != null)
                Load_Mime(sf);
            if (sf["Server"] != null)
                Load_Server(sf);
        }

        public virtual void LoadSetting(string path)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            SettingsPath = path;
            var sf = new OptionsLoader(path, false);
            if (sf["Mime"] != null)
                Load_Mime(sf);
            if (sf["Server"] != null)
                Load_Server(sf);
        }

        protected virtual void Load_Mime(OptionsLoader set)
        {
            DefaultFileMimeAssociation.Clear();
            var gr = set["Mime"].Options.GetSearch().FilterKeys(true);
            foreach (OptionsKey keypair in gr)
                DefaultFileMimeAssociation[keypair.Name] = keypair.GetString();
        }

        protected virtual void Load_Server(OptionsLoader set)
        {
            var server = set["Server"].Options;
            Port = server.GetInt32("Port", 80);
            if (Port <= 0 || Port >= 0xffff)
                Port = 80;
            ConnectionTimeout = server.GetInt32("ConnectionTimeout", 2000);
            if (ConnectionTimeout < 0)
                ConnectionTimeout = 2000;
        }

        public WebServerSettings(string settingFolderPath)
        {
            _ = settingFolderPath ?? throw new ArgumentNullException(nameof(settingFolderPath));
            if (Directory.Exists(settingFolderPath))
                foreach (var file in Directory.GetFiles(settingFolderPath))
                {
                    if (file.EndsWith(".ini"))
                        LoadSetting(file);
                }
            else if (File.Exists(settingFolderPath))
                LoadSetting(settingFolderPath);
            else throw new DirectoryNotFoundException();
        }

        public WebServerSettings(int port, int connectionTimeout)
        {
            if (port <= 0 || port >= 0xffff)
                throw new ArgumentOutOfRangeException(nameof(port));
            if (connectionTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(connectionTimeout));
            Port = port;
            ConnectionTimeout = connectionTimeout;
        }
    }
}
