using MaxLib.Data.BitData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MaxLib.Test.Data.BitData
{
    [TestClass]
    public class TestBits
    {

        [TestMethod]
        public void TestSpeed()
        {
            var val1 = (Bits)BitConverter.DoubleToInt64Bits(Math.E);
            var val2 = ((Bits)BitConverter.DoubleToInt64Bits(Math.PI)).ToBits(5, 17);
            var val3 = ((Bits)BitConverter.DoubleToInt64Bits(double.Epsilon)).ToBits(0, 13);

            const int times = 1_000_000;
            var watch = new Stopwatch();
            var read = new Stopwatch();
            var write = new Stopwatch();

            using (var m = new MemoryStream())
            using (var r = new BitsReader(m))
            using (var w = new BitsWriter(m))
            {
                read.Reset();
                write.Reset();
                watch.Restart();
                for (int i = 0; i < times; ++i)
                {
                    write.Start();
                    m.Position = 0;
                    w.WriteBits(val1);
                    w.WriteBits(val2);
                    w.WriteBits(val3);
                    w.Flush();
                    write.Stop();

                    read.Start();
                    m.Position = 0;
                    r.ReadBits(val1.Length);
                    r.ReadBits(val2.Length);
                    r.ReadBits(val3.Length);
                    read.Stop();
                }
                watch.Stop();
                Logger.LogMessage("time 1: read={1} write={2} total={0}",
                    watch.Elapsed, read.Elapsed, write.Elapsed);
            }

            using (var m = new MemoryStream())
            using (var r = new BitSegmentReader(m))
            using (var w = new BitSegmentWriter(m))
            {
                read.Reset();
                write.Reset();
                watch.Restart();
                for (int i = 0; i < times; ++i)
                {
                    write.Start();
                    m.Position = 0;
                    w.WriteBits(val1);
                    w.WriteBits(val2);
                    w.WriteBits(val3);
                    w.Flush();
                    write.Stop();

                    read.Start();
                    m.Position = 0;
                    r.ReadBitSegment(val1.Length);
                    r.ReadBitSegment(val2.Length);
                    r.ReadBitSegment(val3.Length);
                    read.Stop();
                }
                watch.Stop();
                Logger.LogMessage("time 2: read={1} write={2} total={0}",
                    watch.Elapsed, read.Elapsed, write.Elapsed);
            }
        }

        [TestMethod]
        public void TestBitsSameOutput()
        {
            var val1 = (Bits)BitConverter.DoubleToInt64Bits(Math.E);
            var val2 = ((Bits)BitConverter.DoubleToInt64Bits(Math.PI)).ToBits(5, 17);
            var val3 = ((Bits)BitConverter.DoubleToInt64Bits(double.Epsilon)).ToBits(0, 13);

            using (var m = new MemoryStream())
            using (var r = new BitsReader(m))
            using (var w = new BitsWriter(m))
            {
                w.WriteBits(val1);
                w.WriteBits(val2);
                w.WriteBits(val3);
                w.Flush();

                m.Position = 0;
                Assert.AreEqual(val1, r.ReadBits(val1.Length));
                Assert.AreEqual(val2, r.ReadBits(val2.Length));
                Assert.AreEqual(val3, r.ReadBits(val3.Length));
            }
        }

        [TestMethod]
        public void TestBitsWrite()
        {
            var val1 = Bits.CreateReversed(1, 0, 1, 0, 1, 0, 1, 0);
            const byte check = 0b10101010;

            using (var m = new MemoryStream())
            using (var w = new BitsWriter(m))
            {
                w.WriteBits(val1);
                w.Flush();

                Assert.AreEqual(1, m.Length);
                Assert.AreEqual(check, m.ToArray()[0]);
            }
        }

        [TestMethod]
        public void TestBitsMultiWrite()
        {
            var val1 = Bits.CreateReversed(1, 0, 1, 0);
            var val2 = Bits.CreateReversed(0, 1, 1, 0, 1, 0);
            var val3 = Bits.CreateReversed(0, 1);
            var val4 = Bits.CreateReversed(0, 1, 0, 1);
            var check = new byte[] { 0b10101010, 0b01010101 };

            using (var m = new MemoryStream())
            using (var w = new BitsWriter(m))
            {
                w.WriteBits(val1);
                w.WriteBits(val2);
                w.WriteBits(val3);
                w.WriteBits(val4);
                w.Flush();

                Assert.AreEqual(2, m.Length);
                Assert.AreEqual(check[0], m.ToArray()[0]);
                Assert.AreEqual(check[1], m.ToArray()[1]);
            }
        }

        [TestMethod]
        public void TestBitsRead()
        {
            var val1 = Bits.CreateReversed(1, 0, 1, 0, 1, 0, 1, 0);
            const byte check = 0b10101010;

            using (var m = new MemoryStream(new[] { check }))
            using (var r = new BitsReader(m))
            {
                Assert.AreEqual(val1, r.ReadBits(8));
            }
        }

        [TestMethod]
        public void TestBitsMultiRead()
        {
            var val1 = Bits.CreateReversed(1, 0, 1, 0);
            var val2 = Bits.CreateReversed(0, 1, 1, 0, 1, 0);
            var val3 = Bits.CreateReversed(0, 1);
            var val4 = Bits.CreateReversed(0, 1, 0, 1);

            using (var m = new MemoryStream(new byte[] { 0b10101010, 0b01010101 }))
            using (var r = new BitsReader(m))
            {
                Assert.AreEqual(val1, r.ReadBits(4));
                Assert.AreEqual(val2, r.ReadBits(6));
                Assert.AreEqual(val3, r.ReadBits(2));
                Assert.AreEqual(val4, r.ReadBits(4));
            }
        }

    }
}
