using MaxLib.Data.VirtualIO;
using MaxLib.Data.VirtualIO.LocalDisk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace MaxLib.Test.Data.VirtualIO.LocalDisk
{
    [TestClass]
    public class TestLocalHost
    {
        DirectoryInfo testDir;
        RootController root;

        [TestInitialize]
        public void Init()
        {
            var rng = new Random();
            var buffer = new byte[9];
            while (testDir == null)
            {
                rng.NextBytes(buffer);
                var suffix = Convert.ToBase64String(buffer);
                var path = Path.Combine(Path.GetTempPath(), "MaxLib.Test", suffix);
                if (Directory.Exists(path))
                    continue;
                testDir = new DirectoryInfo(path);
                testDir.Create();
                break;
            }
            root = new RootController();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(Path.Combine(Path.GetTempPath(), "MaxLib.Test")))
                Directory.Delete(Path.Combine(Path.GetTempPath(), "MaxLib.Test"), true);
        }

        [TestMethod]
        public void TestNonExisting()
        {
            var host = new LocalHost(root, testDir);
            var entries = host.GetEntries(VirtualPath.Parse("foo/bar/baz"));
            Assert.AreEqual(0, entries.Count());
        }

        [TestMethod]
        public void TestDir()
        {
            if (!Directory.Exists(Path.Combine(testDir.FullName, "foo")))
                Directory.CreateDirectory(Path.Combine(testDir.FullName, "foo"));
            var host = new LocalHost(root, testDir);
            var entries = host.GetEntries(VirtualPath.Parse("foo"));
            Assert.AreEqual(1, entries.Count());
        }

        [TestMethod]
        public void TestFile()
        {
            if (!File.Exists(Path.Combine(testDir.FullName, "bar.txt")))
                File.Create(Path.Combine(testDir.FullName, "bar.txt"))
                    .Close();
            var host = new LocalHost(root, testDir);
            var entries = host.GetEntries(VirtualPath.Parse("bar.txt"));
            Assert.AreEqual(1, entries.Count());
        }
    }
}
