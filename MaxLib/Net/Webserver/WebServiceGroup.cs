using MaxLib.Collections;
using System;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver
{
    public class WebServiceGroup
    {
        public WebServiceType ServiceType { get; private set; }

        public WebServiceGroup(WebServiceType type)
        {
            ServiceType = type;
            Services = new PriorityList<WebProgressImportance, WebService>();
        }

        public virtual bool SingleExecution
        {
            get
            {
                switch (ServiceType)
                {
                    case WebServiceType.PostCreateDocument: return false;
                    case WebServiceType.PostCreateResponse: return false;
                    case WebServiceType.PostParseRequest: return false;
                    case WebServiceType.PreCreateDocument: return true;
                    case WebServiceType.PreCreateResponse: return false;
                    case WebServiceType.PreParseRequest: return true;
                    case WebServiceType.SendResponse: return true;
                    default: throw new NotImplementedException("ServiceType: " + ServiceType.ToString() + " is not implemented");
                }
            }
        }

        protected PriorityList<WebProgressImportance, WebService> Services { get; private set; }

        public void Add(WebService service)
        {
            _ = service ?? throw new ArgumentNullException(nameof(service));
            service.ImportanceChanged += Service_ImportanceChanged;
            Services.Add(service.Importance, service);
        }

        private void Service_ImportanceChanged(object sender, EventArgs e)
        {
            var service = sender as WebService;
            Services.ChangePriority(service.Importance, service);
        }

        public bool Remove(WebService service)
        {
            if (Services.Remove(service))
            {
                service.ImportanceChanged -= Service_ImportanceChanged;
                return true;
            }
            else return false;
        }

        public void Clear()
        {
            Services.Clear();
        }

        public bool Contains(WebService service)
        {
            return Services.Contains(service);
        }

        public T Get<T>() where T : WebService
        {
            return Services.Find((ws) => ws is T) as T;
        }

        public virtual async Task Execute(WebProgressTask task)
        {
            var se = SingleExecution;
            var set = false;
            var services = Services.ToArray();
            foreach (var service in services)
            {
                if (task.Session.NetworkClient != null && !task.Session.NetworkClient.Connected) return;
                if (service.CanWorkWith(task))
                {
                    if (task.Session.NetworkClient != null && !task.Session.NetworkClient.Connected) return;
                    await service.ProgressTask(task);
                    task.Document[ServiceType] = true;
                    if (se) 
                        return;
                    set = true;
                }
            }
            if (!set) 
                task.Document[ServiceType] = false;
        }
    }
}
