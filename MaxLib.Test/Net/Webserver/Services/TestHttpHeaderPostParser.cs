using MaxLib.Net.Webserver;
using MaxLib.Net.Webserver.Services;
using MaxLib.Net.Webserver.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MaxLib.Test.Net.Webserver.Services
{
    [TestClass]
    public class TestHttpHeaderPostParser
    {
        TestWebServer server;
        TestTask test;

        [TestInitialize]
        public void Init()
        {
            server = new TestWebServer();
            server.AddWebService(new HttpHeaderPostParser());
            test = new TestTask(server)
            {
                CurrentState = WebServiceType.PostParseRequest,
                TerminationState = WebServiceType.PostParseRequest,
            };
        }

        [TestMethod]
        public async Task TestAccept()
        {
            test.Request.HeaderParameter.Add("Accept", "text/css,*/*;q=0.1");
            await new HttpHeaderPostParser().ProgressTask(test.Task);
            Assert.AreEqual(3, test.Request.FieldAccept.Count);
            Assert.AreEqual("text/css", test.Request.FieldAccept[0]);
            Assert.AreEqual("*/*", test.Request.FieldAccept[1]);
            Assert.AreEqual("q=0.1", test.Request.FieldAccept[2]);
        }

        [TestMethod]
        public async Task TestAcceptEncoding()
        {
            test.Request.HeaderParameter.Add("Accept-Encoding", "gzip, deflate, br");
            await new HttpHeaderPostParser().ProgressTask(test.Task);
            Assert.AreEqual(3, test.Request.FieldAcceptEncoding.Count);
            Assert.AreEqual("gzip", test.Request.FieldAcceptEncoding[0]);
            Assert.AreEqual("deflate", test.Request.FieldAcceptEncoding[1]);
            Assert.AreEqual("br", test.Request.FieldAcceptEncoding[2]);
        }

        [TestMethod]
        public async Task TestConnection()
        {
            test.Request.HeaderParameter.Add("Connection", "keep-alive");
            await new HttpHeaderPostParser().ProgressTask(test.Task);
            Assert.AreEqual(HttpConnectionType.KeepAlive, test.Request.FieldConnection);
        }

        [TestMethod]
        public async Task TestConnectionClose()
        {
            test.Request.HeaderParameter.Add("Connection", "close");
            await new HttpHeaderPostParser().ProgressTask(test.Task);
            Assert.AreEqual(HttpConnectionType.Close, test.Request.FieldConnection);
        }

        [TestMethod]
        public async Task TestHost()
        {
            test.Request.HeaderParameter.Add("Host", "test.domain");
            await new HttpHeaderPostParser().ProgressTask(test.Task);
            Assert.AreEqual("test.domain", test.Request.Host);
        }

        [TestMethod]
        public async Task TestCookie()
        {
            test.Request.HeaderParameter.Add("Cookie", "key1=value1; key2= value2;");
            await new HttpHeaderPostParser().ProgressTask(test.Task);
            Assert.AreEqual("key1=value1; key2= value2;", test.Request.Cookie.CompleteRequestCookie);
        }
    }
}
