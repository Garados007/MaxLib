using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Data.BitData
{
    public class BitSegment
        : IEnumerable<Bit>
        , IComparable<BitSegment>, IComparable<Bits>, IComparable
        , IEquatable<BitSegment>, IEquatable<Bits>, IEquatable<Bit>
        , ICloneable
    {
        readonly struct BitsNode
        {
            public Bits Bits { get; }

            public int Start { get; }

            public int Length { get; }

            public BitsNode(Bits bits, int start, int length)
            {
                if (start < 0 || start >= bits.Length)
                    throw new ArgumentOutOfRangeException(nameof(start));
                if (length < 0 || start + length > bits.Length)
                    throw new ArgumentOutOfRangeException(nameof(length));
                Bits = bits;
                Start = start;
                Length = length;
            }

            public BitsNode(Bit[] bits, int start, int length)
            {
                _ = bits ?? throw new ArgumentNullException(nameof(bits));
                if (start < 0 || start >= bits.Length)
                    throw new ArgumentOutOfRangeException(nameof(start));
                if (length < 0 || start + length > bits.Length)
                    throw new ArgumentOutOfRangeException(nameof(length));
                Bits = new Bits(bits);
                Start = start;
                Length = length;
            }
        }

        readonly LinkedList<BitsNode> list = new LinkedList<BitsNode>();

        public int Length { get; private set; } = 0;

        private void GetNodeAt(int index, out LinkedListNode<BitsNode> node, out int localIndex)
        {
            node = list.First;
            while (node != null)
            {
                if (node.Value.Length < index)
                {
                    localIndex = index;
                    return;
                }
                index -= node.Value.Length;
                node = node.Next;
            }
            localIndex = -1;
        }

        public Bit this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                GetNodeAt(index, out LinkedListNode<BitsNode> node, out int localIndex);
                if (node != null)
                    return node.Value.Bits[node.Value.Start + localIndex];
                else return default;
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                GetNodeAt(index, out LinkedListNode<BitsNode> node, out int localIndex);
                if (node == null)
                    return;
                if (localIndex > 0)
                {
                    list.AddBefore(node, new BitsNode(node.Value.Bits, node.Value.Start, localIndex));
                }
                if (localIndex + 1 < node.Value.Length)
                {
                    list.AddAfter(node, new BitsNode(node.Value.Bits, node.Value.Start + localIndex + 1, node.Value.Length - localIndex - 1));
                }
                node.Value = new BitsNode(new Bits(value), 0, 1);
            }
        }

        public Bits Bits
        {
            get
            {
                var result = new Bit[Length];
                int index = 0;
                var node = list.First;
                while (node != null)
                {
                    for (int i = 0; i < node.Value.Length; ++i)
                        result[index++] = node.Value.Bits[node.Value.Start + i];
                    node = node.Next;
                }
                return new Bits(result);
            }
            set
            {
                Clear();
                list.AddLast(new BitsNode(value, 0, value.Length));
            }
        }

        public BitSegment() { }

        public BitSegment(params Bit[] bits)
        {
            _ = bits ?? throw new ArgumentNullException(nameof(bits));
            Length = bits.Length;
            list.AddLast(new BitsNode(bits, 0, bits.Length));
        }

        public BitSegment(ref Bits bits)
        {
            Append(ref bits);
        }

        public void Append(ref Bits bits)
        {
            Length += bits.Length;
            list.AddLast(new BitsNode(bits, 0, bits.Length));
        }

        public void Append(BitSegment bits)
        {
            _ = bits ?? throw new ArgumentNullException(nameof(bits));
            var node = bits.list.First;
            while (node != null)
            {
                list.AddLast(node.Value);
                node = node.Next;
            }
            Length += bits.Length;
        }

        public void Append(Bit[] bits)
        {
            _ = bits ?? throw new ArgumentNullException(nameof(bits));
            Length += bits.Length;
            list.AddLast(new BitsNode(bits, 0, bits.Length));
        }

        public void Prepend(ref Bits bits)
        {
            Length += bits.Length;
            list.AddFirst(new BitsNode(bits, 0, bits.Length));
        }

        public void Prepend(BitSegment bits)
        {
            _ = bits ?? throw new ArgumentNullException(nameof(bits));
            var node = bits.list.Last;
            while (node != null)
            {
                list.AddFirst(node.Value);
                node = node.Previous;
            }
            Length += bits.Length;
        }

        public void Prepend(Bit[] bits)
        {
            _ = bits ?? throw new ArgumentNullException(nameof(bits));
            Length += bits.Length;
            list.AddFirst(new BitsNode(bits, 0, bits.Length));
        }

        public void TrimStart(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (length >= Length)
            {
                Clear();
                return;
            }
            Length -= length;
            while (list.First != null && list.First.Value.Length <= length)
            {
                length -= list.First.Value.Length;
                list.RemoveFirst();
            }
            if (list.First is null)
                return;
            list.First.Value = new BitsNode(
                list.First.Value.Bits,
                list.First.Value.Start + length,
                list.First.Value.Length - length
                );
        }

        public void TrimEnd(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (length >= Length)
            {
                Clear();
                return;
            }
            Length -= length;
            while (list.Last != null && list.Last.Value.Length <= length)
            {
                length -= list.Last.Value.Length;
                list.RemoveLast();
            }
            if (list.Last is null)
                return;
            list.Last.Value = new BitsNode(
                list.Last.Value.Bits,
                list.Last.Value.Start,
                list.Last.Value.Length - length
                );
        }

        public void Clear()
        {
            Length = 0;
            list.Clear();
        }

        public Bits Section(int index, int length)
        {
            if (index < 0 || index > Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (length < 0 || index + length > Length)
                throw new ArgumentOutOfRangeException(nameof(length));
            var result = new Bit[length];
            int ind = 0;
            var node = list.First;
            while (node != null)
            {
                if (node.Value.Length > index)
                {
                    var start = node.Value.Start + index;
                    var stop = start + Math.Min(length, node.Value.Length);
                    for (int i = start; i < stop; ++i)
                        result[ind++] = node.Value.Bits[i];
                    index = 0;
                    length -= stop - start;
                }
                node = node.Next;
            }
            return new Bits(result);
        }

        public BitSegment SegmentSection(int index, int length)
        {
            if (index < 0 || index > Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (length < 0 || index + length > Length)
                throw new ArgumentOutOfRangeException(nameof(length));
            var result = new BitSegment
            {
                Length = length
            };
            var node = list.First;
            while (node != null && length > 0)
            {
                if (node.Value.Length > index)
                {
                    var start = node.Value.Start + index;
                    var end = Math.Min(node.Value.Start + node.Value.Length, start + length);
                    var blockLength = end - start;
                    result.list.AddLast(new BitsNode(node.Value.Bits, start, blockLength));
                    index = 0;
                    length -= blockLength;
                }
                node = node.Next;
            }
            return result;
        }

        public byte[] ToBytes(int index, int byteLength)
        {
            if (index < 0 || index > Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (byteLength < 0 || index + (byteLength >> 3) >= Length + 8)
                throw new ArgumentOutOfRangeException(nameof(byteLength));
            var result = new byte[byteLength];
            int i = 0; // i in result bits
            var node = list.First;
            var max = Math.Min(Length - index, byteLength << 3);
            while (node != null && i < max)
            {
                if (node.Value.Length > index)
                {
                    var lmax = Math.Min(node.Value.Length - index, max - i);
                    for (int j = 0; j < lmax; ++j, ++i)
                    {
                        if (node.Value.Bits[node.Value.Start + j + index].Set)
                            result[i >> 3] |= (byte)(1 << (i & 0x7));
                    }
                    index = 0;
                }
                else index -= node.Value.Length;
                node = node.Next;
            }
            return result;
        }

        public override string ToString()
            => string.Join("", this.Reverse());

        #region IEnumerable

        public IEnumerator<Bit> GetEnumerator()
        {
            var node = list.First;
            while (node != null)
            {
                for (int i = 0; i < node.Value.Length; ++i)
                    yield return node.Value.Bits[node.Value.Start + i];
                node = node.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        #endregion IEnumerable

        #region IComparable

        public int CompareTo(BitSegment other)
        {
            if (other is null)
                return 0;
            foreach (var (bit1, bit2) in SpecialZip(this, other, new Bit(false)))
            {
                var c = bit1.CompareTo(bit2);
                if (c != 0)
                    return c;
            }
            return 0;
        }

        public int CompareTo(Bits other)
        {
            foreach (var (bit1, bit2) in SpecialZip(this, other, new Bit(false)))
            {
                var c = bit1.CompareTo(bit2);
                if (c != 0)
                    return c;
            }
            return 0;
        }

        public int CompareTo(object obj)
        {
            if (obj is BitSegment bitSeg)
                return CompareTo(bitSeg);
            if (obj is Bits bits)
                return CompareTo(bits);
            return 0;
        }

        private IEnumerable<(Bit, Bit)> SpecialZip(IEnumerable<Bit> bits1, IEnumerable<Bit> bits2, Bit defaultBit)
        {
            var enum1 = bits1.GetEnumerator();
            var enum2 = bits2.GetEnumerator();
            while (true)
            {
                var has1 = enum1.MoveNext();
                var has2 = enum2.MoveNext();
                if (!has1 && !has2)
                    yield break;
                var bit1 = has1 ? enum1.Current : defaultBit;
                var bit2 = has2 ? enum2.Current : defaultBit;
                yield return (bit1, bit2);
            }
        }

        #endregion

        #region IEquatable

        public bool Equals(BitSegment other)
            => !(other is null) && CompareTo(other) == 0;

        public bool Equals(Bits other)
            => CompareTo(other) == 0;

        public bool Equals(Bit other)
            => Length == 1 && this[0] == other;

        public override bool Equals(object obj)
        {
            if (obj is BitSegment bitSeg)
                return Equals(bitSeg);
            if (obj is Bits bits)
                return Equals(bits);
            if (obj is Bit bit)
                return Equals(bit);
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 0x1;
            bool xor = true;
            foreach (var bit in this)
            {
                if (xor)
                {
                    if (bit)
                        hash ^= 0;
                    hash <<= 1;
                }
                else
                {
                    hash <<= 1;
                    if (bit)
                        hash |= 1;
                }
                xor = !xor;
            }
            return hash;
        }

        #endregion

        #region ICloneable

        public BitSegment Clone()
        {
            var bits = new BitSegment
            {
                Length = Length
            };
            var node = list.First;
            while (node != null)
            {
                bits.list.AddLast(node.Value);
                node = node.Next;
            }
            return bits;
        }

        object ICloneable.Clone()
            => Clone();

        #endregion

        public static implicit operator BitSegment(Bits bits)
            => new BitSegment(ref bits);

        public static implicit operator Bits(BitSegment bits)
            => bits?.Bits ?? new Bits();

        /// <summary>
        /// Take the section from <paramref name="offset"/> of <paramref name="value"/>
        /// with a length of <paramref name="length"/> and convert them to <see cref="BitSegment"/>
        /// </summary>
        /// <param name="value">the source</param>
        /// <param name="offset">the start in <paramref name="value"/></param>
        /// <param name="length">the length of data in <paramref name="value"/></param>
        /// <returns>the generated <see cref="BitSegment"/></returns>
        public static BitSegment ToBits(byte[] value, int offset, int length)
        {
            _ = value ?? throw new ArgumentNullException(nameof(value));
            if (offset < 0 || offset > value.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || offset + length > value.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
            var result = new Bit[length << 3];
            for (int i = 0; i < length; ++i)
            {
                int mask = 0x1;
                for (int j = 0; j < 8; ++j)
                {
                    result[(i << 3) + j] = (value[i] & mask) == mask;
                    mask <<= 1;
                }
            }
            return new BitSegment(result);
        }
    }
}
