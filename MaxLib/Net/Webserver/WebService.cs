using System;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver
{
    public abstract class WebService
    {
        public WebServiceType ServiceType { get; private set; }

        public WebService(WebServiceType type)
        {
            ServiceType = type;
            Importance = WebProgressImportance.Normal;
        }

        public abstract Task ProgressTask(WebProgressTask task);

        public abstract bool CanWorkWith(WebProgressTask task);

        public event EventHandler ImportanceChanged;

        WebProgressImportance importance;
        public WebProgressImportance Importance
        {
            get => importance;
            protected set
            {
                importance = value;
                ImportanceChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
