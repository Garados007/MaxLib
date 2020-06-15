using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Testing
{
    public class TestWebServer : WebServer
    {
        static WebServerSettings GetSettings()
        {
            return new WebServerSettings(80, 0)
            {
                Debug_LogConnections = false,
                Debug_WriteRequests = false,
                IPFilter = IPAddress.Any,
            };
        }

        public TestWebServer()
            : base(GetSettings())
        {

        }

        public override void Start()
        {
            ServerExecution = true;
        }

        public override void Stop()
        {
            ServerExecution = false;
        }

        protected override void ServerMainTask()
        {
        }

        protected override void ClientConnected(TcpClient client)
        {
        }

        protected override Task SafeClientStartListen(HttpSession session)
            => Task.CompletedTask;

        protected override Task ClientStartListen(HttpSession session)
            => Task.CompletedTask;

        public Task Execute(WebProgressTask task, WebServiceType terminationState = WebServiceType.SendResponse)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));
            return ExecuteTaskChain(task, terminationState);
        }

        public new void RemoveSession(HttpSession session)
            => base.RemoveSession(session);

        public new HttpSession CreateRandomSession()
            => base.CreateRandomSession();

        public TestTask CreateTest()
            => new TestTask(this);
    }
}
