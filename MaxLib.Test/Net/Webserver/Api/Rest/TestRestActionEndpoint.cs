using MaxLib.Net.Webserver;
using MaxLib.Net.Webserver.Api.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaxLib.Test.Net.Webserver.Api.Rest
{
    [TestClass]
    public class TestRestActionEndpoint
    {
        class Methods
        {
            public static Task<HttpDataSource> HandleDicSource(Dictionary<string, object> args)
            {
                Assert.IsNotNull(args);
                return Task.FromResult<HttpDataSource>(
                    new HttpStringDataSource("handle-dic-source"));
            }

            public static Task<HttpDataSource> HandleFunc0Source()
            {
                return Task.FromResult<HttpDataSource>(
                    new HttpStringDataSource("handle-func-0-source"));
            }

            public static Task<HttpDataSource> HandleFunc1Source(string arg1)
            {
                Assert.AreEqual("value-1", arg1);
                return Task.FromResult<HttpDataSource>(
                    new HttpStringDataSource("handle-func-1-source"));
            }

            public static Task<HttpDataSource> HandleFunc2Source(string arg1, string arg2)
            {
                Assert.AreEqual("value-1", arg1);
                Assert.AreEqual("value-2", arg2);
                return Task.FromResult<HttpDataSource>(
                    new HttpStringDataSource("handle-func-2-source"));
            }

            public static Task<HttpDataSource> HandleFunc3Source(string arg1, string arg2, string arg3)
            {
                Assert.AreEqual("value-1", arg1);
                Assert.AreEqual("value-2", arg2);
                Assert.AreEqual("value-3", arg3);
                return Task.FromResult<HttpDataSource>(
                    new HttpStringDataSource("handle-func-3-source"));
            }

            public static Task<HttpDataSource> HandleFunc4Source(string arg1, string arg2, string arg3, string arg4)
            {
                Assert.AreEqual("value-1", arg1);
                Assert.AreEqual("value-2", arg2);
                Assert.AreEqual("value-3", arg3);
                Assert.AreEqual("value-4", arg4);
                return Task.FromResult<HttpDataSource>(
                    new HttpStringDataSource("handle-func-4-source"));
            }
        }

        private async Task GetResult(RestActionEndpoint endpoint, string check, params (string, object)[] values)
        {
            var args = new Dictionary<string, object>();
            foreach (var (key, value) in values)
                args[key] = value;
            var source = await endpoint.GetSource(args);
            Assert.IsTrue(source is HttpStringDataSource, "source is not a string source");
            var ds = (HttpStringDataSource)source;
            Assert.AreEqual(check, ds.Data);
        }

        [TestMethod]
        public async Task TestDicSource()
        {
            var ep = RestActionEndpoint.Create(Methods.HandleDicSource);
            await GetResult(ep, "handle-dic-source");
        }

        [TestMethod]
        public async Task TestFunc0Source()
        {
            var ep = RestActionEndpoint.Create(Methods.HandleFunc0Source);
            await GetResult(ep, "handle-func-0-source");
        }

        [TestMethod]
        public async Task TestFunc1Source()
        {
            var ep = RestActionEndpoint.Create<string>(Methods.HandleFunc1Source, "arg1");
            await GetResult(ep, "handle-func-1-source", 
                ("arg1", "value-1"));
        }

        [TestMethod]
        public async Task TestFunc2Source()
        {
            var ep = RestActionEndpoint.Create<string, string>(Methods.HandleFunc2Source, "arg1", "arg2");
            await GetResult(ep, "handle-func-2-source",
                ("arg2", "value-2"),
                ("arg1", "value-1"));
        }

        [TestMethod]
        public async Task TestFunc3Source()
        {
            var ep = RestActionEndpoint.Create<string, string, string>(
                Methods.HandleFunc3Source, "arg1", "arg2", "arg3");
            await GetResult(ep, "handle-func-3-source",
                ("arg2", "value-2"),
                ("arg3", "value-3"),
                ("arg1", "value-1"));
        }

        [TestMethod]
        public async Task TestFunc4Source()
        {
            var ep = RestActionEndpoint.Create(
                new Func<string, string, string, string, Task<HttpDataSource>>(
                    Methods.HandleFunc4Source), 
                new string[]
                { 
                    "arg1", 
                    "arg2", 
                    "arg3",
                    "arg4",
                });
            await GetResult(ep, "handle-func-4-source",
                ("arg4", "value-4"),
                ("arg2", "value-2"),
                ("arg3", "value-3"),
                ("arg1", "value-1"));
        }
    }
}
