using MaxLib.Net.Webserver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MaxLib.Test.Net.Webserver
{
    [TestClass]
    [TestCategory("net > web server > http stream")]
    public class HttpStreamTest
    {
        private MemoryStream GetBaseData(Encoding encoding = null)
        {
            var m = new MemoryStream();
            var w = new StreamWriter(m, encoding ?? Encoding.Default);
            w.WriteLine("first line");
            w.Flush();
            m.Write(new byte[] { 0, 1, 2, 100, 200 }, 0, 5);
            w.WriteLine("third line");
            w.WriteLine();
            w.WriteLine("fifth line");
            w.Flush();
            m.Position = 0;
            return m;
        }

        [TestMethod]
        public void TestReadMixedASCII()
        {
            using (var hs = new HttpStream(GetBaseData(Encoding.ASCII), Encoding.ASCII, 10))
            {
                Assert.AreEqual("first line", hs.ReadLine());
                var control = new byte[] { 0, 1, 2, 100, 200 };
                var buffer = new byte[5];
                Assert.AreEqual(5, hs.Read(buffer, 0, 5));
                for (int i = 0; i < 5; ++i)
                    Assert.AreEqual(control[i], buffer[i]);
                Assert.AreEqual("third line", hs.ReadLine());
                Assert.AreEqual("", hs.ReadLine());
                Assert.AreEqual("fifth line", hs.ReadLine());
                Assert.AreEqual(null, hs.ReadLine());
            }
        }

        [TestMethod]
        public void TestReadMixedUtf8()
        {
            using (var hs = new HttpStream(GetBaseData(Encoding.UTF8), Encoding.UTF8, 10))
            {
                Assert.AreEqual("first line", hs.ReadLine());
                var control = new byte[] { 0, 1, 2, 100, 200 };
                var buffer = new byte[5];
                Assert.AreEqual(5, hs.Read(buffer, 0, 5));
                for (int i = 0; i < 5; ++i)
                    Assert.AreEqual(control[i], buffer[i]);
                Assert.AreEqual("third line", hs.ReadLine());
                Assert.AreEqual("", hs.ReadLine());
                Assert.AreEqual("fifth line", hs.ReadLine());
                Assert.AreEqual(null, hs.ReadLine());
            }
        }

        [TestMethod]
        public void TestWriteText()
        {
            using (var m = new MemoryStream())
            using (var r = new StreamReader(m))
            using (var hs = new HttpStream(m, Encoding.UTF8, 10))
            {
                hs.WriteLine("first line");
                hs.WriteLine("second line");
                hs.WriteLine("more text");
                m.Position = 0;
                Assert.AreEqual("first line", r.ReadLine());
                Assert.AreEqual("second line", r.ReadLine());
                Assert.AreEqual("more text", r.ReadLine());
            }
        }
    }
}
