using MaxLib.Data.BitData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MaxLib.Test.Data.BitData
{
    [TestClass]
    public class TestBitSegment
    {

        [TestMethod]
        public void TestBitsSameOutput()
        {
            BitSegment val1 = (Bits)BitConverter.DoubleToInt64Bits(Math.E);
            BitSegment val2 = ((Bits)BitConverter.DoubleToInt64Bits(Math.PI)).ToBits(5, 17);
            BitSegment val3 = ((Bits)BitConverter.DoubleToInt64Bits(double.Epsilon)).ToBits(0, 13);

            using (var m = new MemoryStream())
            using (var r = new BitSegmentReader(m))
            using (var w = new BitSegmentWriter(m))
            {
                w.WriteBits(val1);
                w.WriteBits(val2);
                w.WriteBits(val3);
                w.Flush();

                m.Position = 0;
                Assert.AreEqual(val1, r.ReadBitSegment(val1.Length));
                Assert.AreEqual(val2, r.ReadBitSegment(val2.Length));
                Assert.AreEqual(val3, r.ReadBitSegment(val3.Length));
            }
        }

        [TestMethod]
        public void TestBitsWrite()
        {
            BitSegment val1 = Bits.CreateReversed(1, 0, 1, 0, 1, 0, 1, 0);
            const byte check = 0b10101010;

            using (var m = new MemoryStream())
            using (var w = new BitSegmentWriter(m))
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
            BitSegment val1 = Bits.CreateReversed(1, 0, 1, 0);
            BitSegment val2 = Bits.CreateReversed(0, 1, 1, 0, 1, 0);
            BitSegment val3 = Bits.CreateReversed(0, 1);
            BitSegment val4 = Bits.CreateReversed(0, 1, 0, 1);
            var check = new byte[] { 0b10101010, 0b01010101 };

            using (var m = new MemoryStream())
            using (var w = new BitSegmentWriter(m))
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
            BitSegment val1 = Bits.CreateReversed(1, 0, 1, 0, 1, 0, 1, 0);
            const byte check = 0b10101010;

            using (var m = new MemoryStream(new[] { check }))
            using (var r = new BitSegmentReader(m))
            {
                Assert.AreEqual(val1, r.ReadBitSegment(8));
            }
        }

        [TestMethod]
        public void TestBitsMultiRead()
        {
            BitSegment val1 = Bits.CreateReversed(1, 0, 1, 0);
            BitSegment val2 = Bits.CreateReversed(0, 1, 1, 0, 1, 0);
            BitSegment val3 = Bits.CreateReversed(0, 1);
            BitSegment val4 = Bits.CreateReversed(0, 1, 0, 1);

            using (var m = new MemoryStream(new byte[] { 0b10101010, 0b01010101 }))
            using (var r = new BitSegmentReader(m))
            {
                Assert.AreEqual(val1, r.ReadBitSegment(4));
                Assert.AreEqual(val2, r.ReadBitSegment(6));
                Assert.AreEqual(val3, r.ReadBitSegment(2));
                Assert.AreEqual(val4, r.ReadBitSegment(4));
            }
        }
    }
}
