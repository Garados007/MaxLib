using MaxLib.Net.Webserver.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MaxLib.Test.Net.Webserver.Testing
{
    [TestClass]
    public class TestTestTask
    {
        [TestMethod]
        public void TestGetAddedCookies()
        {
            var server = new TestWebServer();
            var test = new TestTask(server);
            test.Task.Document.RequestHeader.Cookie.AddedCookies.Add(
                "test1",
                new MaxLib.Net.Webserver.HttpCookie.Cookie("test2", "test3"));
            var added = test.GetAddedCookies().ToArray();
            Assert.AreEqual(1, added.Length);
            Assert.AreEqual("test1", added[0].Item1);
            Assert.AreEqual("test2", added[0].Item2.NameString);
            Assert.AreEqual("test3", added[0].Item2.ValueString);
        }
    }
}
