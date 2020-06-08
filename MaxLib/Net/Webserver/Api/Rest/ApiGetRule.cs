using System;

namespace MaxLib.Net.Webserver.Api.Rest
{
    public abstract class ApiGetRule : ApiRule
    {
        private string key;
        public string Key
        {
            get => key;
            set => key = value ?? throw new ArgumentNullException(nameof(Key));
        }
    }
}
