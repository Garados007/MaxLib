using MaxLib.Data.VirtualIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MaxLib.Test.Data.VirtualIO
{
    [TestClass]
    public class TestVirtualPath
    {
        #region Parsing

        [TestMethod]
        public void TestSimpleParsing()
        {
            var path = VirtualPath.Parse("/foo//bar/./baz");
            Assert.AreEqual("foo/bar/baz", path.ToString());
        }

        [TestMethod]
        public void TestSingleRoot()
        {
            var path = VirtualPath.Parse("@/foo/bar/");
            Assert.AreEqual("@/foo/bar", path.ToString());
        }

        [TestMethod]
        public void TestMultiRoot()
        {
            var path = VirtualPath.Parse("@/foo/bar/@/baz");
            Assert.AreEqual("@/baz", path.ToString());
        }

        [TestMethod]
        public void TestStartingEllipsis()
        {
            var path = VirtualPath.Parse("../../foo/bar");
            Assert.AreEqual("../../foo/bar", path.ToString());
        }

        [TestMethod]
        public void TestInlineEllipsis()
        {
            var path = VirtualPath.Parse("foo/bar/../baz");
            Assert.AreEqual("foo/baz", path.ToString());
        }

        [TestMethod]
        public void TestLock()
        {
            var path = VirtualPath.Parse("foo/:/bar/");
            Assert.AreEqual("foo/:/bar", path.ToString());

            path = VirtualPath.Parse("foo/:/bar/:/baz");
            Assert.AreEqual("foo/:/bar/baz", path.ToString());
        }

        [TestMethod]
        public void TestEmpty()
        {
            var path = VirtualPath.Parse("");
            Assert.AreEqual("/", path.ToString());
        }

        #endregion

        #region Combination

        [TestMethod]
        public void TestCombineNormal()
        {
            var path1 = VirtualPath.Parse("a/b/");
            var path2 = VirtualPath.Parse("1/2/");
            var result = VirtualPath.Combine(path1, path2);
            Assert.AreEqual("a/b/1/2", result.ToString());
        }

        [TestMethod]
        public void TestCombineTrim()
        {
            var path1 = VirtualPath.Parse("a/b/c/d");
            var path2 = VirtualPath.Parse("../../1/2/3");
            var result = VirtualPath.Combine(path1, path2);
            Assert.AreEqual("a/b/1/2/3", result.ToString(), "normal path trim");

            path1 = VirtualPath.Parse("@/a");
            path2 = VirtualPath.Parse("../../1/2/3");
            result = VirtualPath.Combine(path1, path2);
            Assert.AreEqual("@/1/2/3", result.ToString(), "do not trim the root");
        }

        [TestMethod]
        public void TestCombineNewRoot()
        {
            var path1 = VirtualPath.Parse("a/b/c/d");
            var path2 = VirtualPath.Parse("@/1/2/3");
            var result = VirtualPath.Combine(path1, path2);
            Assert.AreEqual("@/1/2/3", result.ToString());
        }

        [TestMethod]
        public void TestCombineLocks()
        {
            var path1 = VirtualPath.Parse("a/b/c/d");
            var path2 = VirtualPath.Parse(":/1/2/3");
            var result = VirtualPath.Combine(path1, path2);
            Assert.AreEqual("a/b/c/d/:/1/2/3", result.ToString(), "use the new lock");

            path1 = VirtualPath.Parse("a/b/:/c/d");
            path2 = VirtualPath.Parse("1/:/2/3");
            result = VirtualPath.Combine(path1, path2);
            Assert.AreEqual("a/b/:/c/d/1/2/3", result.ToString(), "remove double locks");

            path1 = VirtualPath.Parse("a/b/c/d/:");
            path2 = VirtualPath.Parse("../1/2/3");
            result = VirtualPath.Combine(path1, path2);
            Assert.AreEqual("a/b/c/1/2/3", result.ToString(), "locks are not a path element");
        }

        #endregion

        #region Compare To

        [TestMethod]
        public void TestCompareDifferent()
        {
            var path1 = VirtualPath.Parse("a/b/c");
            var path2 = VirtualPath.Parse("1/2/3");
            Assert.AreEqual(null, path1.CompareTo(path2));
        }

        [TestMethod]
        public void TestCompareEqual()
        {
            var path1 = VirtualPath.Parse("a/b/c");
            var path2 = VirtualPath.Parse("a/b/c");
            Assert.AreEqual(0, path1.CompareTo(path2));
        }

        [TestMethod]
        public void TestCompareLess()
        {
            var path1 = VirtualPath.Parse("a/b");
            var path2 = VirtualPath.Parse("a/b/c/d");
            Assert.IsTrue(path1.CompareTo(path2) < 0);
        }

        [TestMethod]
        public void TestCompareGreater()
        {
            var path1 = VirtualPath.Parse("a/b/c/d");
            var path2 = VirtualPath.Parse("a/b");
            Assert.IsTrue(path1.CompareTo(path2) > 0);
        }

        [TestMethod]
        public void TestCompareWithRoot()
        {
            var path1 = VirtualPath.Parse("@/a/b");
            var path2 = VirtualPath.Parse("a/b");
            Assert.AreEqual(0, path1.CompareTo(path2, true, true), "single root");

            path1 = VirtualPath.Parse("@/a/b");
            path2 = VirtualPath.Parse("@/a/b");
            Assert.AreEqual(0, path1.CompareTo(path2, true, true), "double root");
        }

        [TestMethod]
        public void TestCompareWithHostLocks()
        {
            var path1 = VirtualPath.Parse("a/:/b");
            var path2 = VirtualPath.Parse("a/b");
            Assert.AreEqual(0, path1.CompareTo(path2, true, true), "single one");

            path1 = VirtualPath.Parse("a/:/b");
            path2 = VirtualPath.Parse("a/:/b");
            Assert.AreEqual(0, path1.CompareTo(path2, true, true), "same place");

            path1 = VirtualPath.Parse("1/:/2/3");
            path2 = VirtualPath.Parse("1/2/:/3");
            Assert.AreEqual(0, path1.CompareTo(path2, true, true), "out of order");

            path1 = VirtualPath.Combine(VirtualPath.Parse("1/2/:/"), VirtualPath.Parse(":/3"));
            path2 = VirtualPath.Parse("1/2/3");
            Assert.AreEqual(0, path1.CompareTo(path2, true, true), "multiple locks");
        }

        #endregion

        [TestMethod]
        public void TestHostFilter()
        {
            var path = VirtualPath.Parse("a/b/c/:/d/e");
            Assert.AreEqual("a/b/c", path.GetHostFilter().ToString());

            path = VirtualPath.Parse("@/a/b/c/:/d/e");
            Assert.AreEqual("@/a/b/c", path.GetHostFilter().ToString());
        }

        [TestMethod]
        public void TestCreateSubPath()
        {
            var path = VirtualPath.Parse("a/b/c/d/e");
            Assert.AreEqual("c/d/e", path.CreateSubPath(2).ToString(), "normal sub path");

            path = VirtualPath.Parse("@/a/b/c/d/e");
            Assert.AreEqual("c/d/e", path.CreateSubPath(2, true).ToString(), "sub path from rooted");

            path = VirtualPath.Parse("@/a/b/:/c/d/e");
            Assert.AreEqual("c/d/e", path.CreateSubPath(2, true).ToString(), "with removeable lock");

            path = VirtualPath.Parse("@/a/b/c/d/:/e");
            Assert.AreEqual("c/d/:/e", path.CreateSubPath(2, true).ToString(), "with non removing lock");
        }

        [TestMethod]
        public void TestMakeRootPath()
        {
            var path = VirtualPath.Parse("@/a/b/c");
            Assert.AreEqual("@/a/b/c", path.MakeRootPath().ToString());

            path = VirtualPath.Parse("@/../../a/b/c");
            Assert.AreEqual("@/a/b/c", path.MakeRootPath().ToString());
        }

        [TestMethod]
        public void TestRemoveLocks()
        {
            var path = VirtualPath.Parse("a/b/:/c");
            Assert.AreEqual("a/b/c", path.RemoveLocks().ToString());
        }
    }
}
