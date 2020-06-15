using MaxLib.Net.Webserver;
using MaxLib.Net.Webserver.Api.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace MaxLib.Test.Net.Webserver.Api.Rest
{
    [TestClass]
    public class TestRestApiFactory
    {
        ApiRuleFactory fact;
        RestQueryArgs args;

        [TestInitialize]
        public void Init()
        {
            fact = new ApiRuleFactory();
            args = new RestQueryArgs(
                new[] { "foo", "bar", "0", "1" },
                new Dictionary<string, string>
                {
                    { "foo", "bar" },
                    { "baz", "7" },
                },
                new HttpPost("a=1&b=2", MimeType.ApplicationXWwwFromUrlencoded)
                );
        }

        [TestMethod]
        public void TestUrlConstant()
        {
            var rule = fact.UrlConstant("foo");
            Assert.IsTrue(rule.Check(args), "test: foo");

            rule = fact.UrlConstant("Bar", true, 1);
            Assert.IsTrue(rule.Check(args), "test: bar");

            rule = fact.UrlConstant("baz");
            Assert.IsFalse(rule.Check(args), "test: baz");
        }

        [TestMethod]
        public void TestUrlArgument()
        {
            var rule = fact.UrlArgument<int>("num0", int.TryParse, 2);
            Assert.IsTrue(rule.Check(args), "test:num0");
            Assert.AreEqual(0, args.ParsedArguments["num0"]);

            rule = fact.UrlArgument<int>("num1", int.TryParse, 3);
            Assert.IsTrue(rule.Check(args), "test:num1");
            Assert.AreEqual(1, args.ParsedArguments["num1"]);

            rule = fact.UrlArgument<int>("num2", int.TryParse, 1);
            Assert.IsFalse(rule.Check(args), "test:num2");
            Assert.IsFalse(args.ParsedArguments.ContainsKey("num2"), "check:num2");

            var rule2 = fact.UrlArgument("arg", 1);
            Assert.IsTrue(rule2.Check(args), "test:arg");
            Assert.AreEqual("bar", args.ParsedArguments["arg"]);
        }

        [TestMethod]
        public void TestMaxLength()
        {
            var rule = fact.MaxLength(4);
            Assert.IsTrue(rule.Check(args), "test:4");

            rule = fact.MaxLength(3);
            Assert.IsFalse(rule.Check(args), "test:3");

            rule = fact.MaxLength(5);
            Assert.IsTrue(rule.Check(args), "test:5");
        }

        [TestMethod]
        public void TestKeyExists()
        {
            var rule = fact.KeyExists("foo");
            Assert.IsTrue(rule.Check(args), "test:1");

            rule = fact.KeyExists("bar");
            Assert.IsFalse(rule.Check(args), "test:2");
        }

        [TestMethod]
        public void TestGetArgument()
        {
            var rule = fact.GetArgument<int>("baz", int.TryParse);
            Assert.IsTrue(rule.Check(args), "test:1");
            Assert.AreEqual(7, args.ParsedArguments["baz"]);

            rule = fact.GetArgument<int>("foo", int.TryParse);
            Assert.IsFalse(rule.Check(args), "test:2");
            Assert.IsFalse(args.ParsedArguments.ContainsKey("foo"));

            var rule2 = fact.GetArgument("foo");
            Assert.IsTrue(rule2.Check(args), "test:3");
            Assert.AreEqual("bar", args.ParsedArguments["foo"]);
        }

        [TestMethod]
        public void TestGroup()
        {
            var rule = fact.Group(
                fact.UrlConstant("foo"),
                fact.KeyExists("baz"));
            Assert.IsTrue(rule.Check(args), "test:1");

            rule = fact.Group(
                fact.Optional(fact.UrlConstant("bar")),
                fact.KeyExists("baz"));
            Assert.IsTrue(rule.Check(args), "test:2");

            rule = fact.Group(
                fact.UrlConstant("foo"),
                fact.Optional(fact.KeyExists("bar")));
            Assert.IsTrue(rule.Check(args), "test:3");

            rule = fact.Group(
                fact.UrlConstant("bar"),
                fact.KeyExists("baz"));
            Assert.IsFalse(rule.Check(args), "test:4");

            rule = fact.Group(
                fact.UrlConstant("foo"),
                fact.KeyExists("bar"));
            Assert.IsFalse(rule.Check(args), "test:5");
        }

        [TestMethod]
        public void TestLocation()
        {
            var rule = fact.Location(
                fact.UrlConstant("foo"),
                fact.UrlConstant("bar"));
            Assert.IsTrue(rule.Check(args), "test:1");

            rule = fact.Location(
                fact.UrlConstant("foo"),
                fact.UrlConstant("bar"),
                fact.UrlConstant("0"),
                fact.UrlArgument<int>("var", int.TryParse));
            Assert.IsTrue(rule.Check(args), "test:2");
            Assert.AreEqual(1, args.ParsedArguments["var"]);

            args.ParsedArguments.Clear();
            rule = fact.Location(
                fact.UrlConstant("foo"),
                fact.UrlConstant("bar"),
                fact.UrlConstant("0"),
                fact.UrlArgument<int>("var", int.TryParse),
                fact.MaxLength());
            Assert.IsTrue(rule.Check(args), "test:2");
            Assert.AreEqual(1, args.ParsedArguments["var"]);
        }

        [TestMethod]
        public void TestOptional()
        {
            var rule = fact.Optional(fact.UrlConstant("foo"));
            Assert.IsTrue(rule.Check(args), "test:1");

            rule = fact.Optional(fact.UrlConstant("baz"));
            Assert.IsFalse(rule.Check(args), "test:2");
        }

        [TestMethod]
        public void TestConditional()
        {
            var rule = fact.Conditional(
                fact.UrlConstant("foo"),
                fact.UrlArgument<int>("var", int.TryParse, 2),
                fact.UrlArgument<int>("var", int.TryParse, 3));
            Assert.IsTrue(rule.Check(args), "test:1");
            Assert.AreEqual(0, args.ParsedArguments["var"], "check:1");

            args.ParsedArguments.Clear();
            rule = fact.Conditional(
                fact.UrlConstant("bar"),
                fact.UrlArgument<int>("var", int.TryParse, 2),
                fact.UrlArgument<int>("var", int.TryParse, 3));
            Assert.IsTrue(rule.Check(args), "test:2");
            Assert.AreEqual(1, args.ParsedArguments["var"], "check:2");

            args.ParsedArguments.Clear();
            rule = fact.Conditional(
                fact.UrlConstant("foo"),
                fact.UrlArgument<int>("var", int.TryParse, 1),
                fact.UrlArgument<int>("var", int.TryParse, 3));
            Assert.IsFalse(rule.Check(args), "test:3");
            Assert.IsFalse(args.ParsedArguments.ContainsKey("var"), "check:3");

            args.ParsedArguments.Clear();
            rule = fact.Conditional(
                fact.UrlConstant("bar"),
                fact.UrlArgument<int>("var", int.TryParse, 2),
                fact.UrlArgument<int>("var", int.TryParse, 1));
            Assert.IsFalse(rule.Check(args), "test:4");
            Assert.IsFalse(args.ParsedArguments.ContainsKey("var"), "check:4");
        }
    }
}
