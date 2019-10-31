using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MaxLib.Data.Bits
{
    public struct Bits : IComparable, IComparable<Bits>, IEquatable<Bits>
    {
        private readonly Bit[] bits;

        public int Length => bits?.Length ?? 0;

        public Bit this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return bits[index];
            }
        }

        public Bits Set(int index, Bit bit) 
            => new Bits(bits.Select((b, ind) => ind == index ? bit : b));

        public Bits(params Bit[] bits)
        {
            this.bits = bits ?? throw new ArgumentNullException(nameof(bits));
        }

        public Bits(IEnumerable<Bit> bits)
        {
            if (bits == null) throw new ArgumentNullException(nameof(bits));
            this.bits = bits.ToArray();
        }

        public override string ToString()
            => bits != null ? string.Join("", bits.Reverse()) : "";

        #region IComparable<Bits>

        public int CompareTo(Bits other)
        {
            var max = Math.Max(Length, other.Length);
            for (int i = max - 1; i >= 0; --i)
            {
                var b1 = i < Length ? this[i] : new Bit(false);
                var b2 = i < other.Length ? other[i] : new Bit(false);
                var c = b1.CompareTo(b2);
                if (c != 0)
                    return c;
            }
            return 0;
        }

        #endregion IComparable<Bits>

        #region IComparable

        public int CompareTo(object obj)
        {
            if (obj is Bits bits)
                return CompareTo(bits);
            else return 0;
        }

        #endregion IComparable

        #region IEquatable<Bits>

        public bool Equals(Bits other)
        {
            if (Length != other.Length)
                return false;
            if (bits == null)
                return true;
            for (int i = 0; i < bits.Length; ++i)
            {
                if (!bits[i].Equals(other.bits[i]))
                    return false;
            }
            return true;
        }

        #endregion IEquatable<Bits>

        public override bool Equals(object obj)
        {
            if (obj is Bits bits)
                return Equals(bits);
            else return false;
        }

        public override int GetHashCode()
        {
            var hash = 0;
            var mask = 0x1;
            if (bits != null)
                for (int i = 0; i<bits.Length; ++i)
                {
                    if (bits[i])
                        hash ^= mask;
                    mask <<= 1;
                    if (mask == 0)
                        mask = 0x1;
                }
            return hash;
        }

        #region operator: compare

        public static bool operator ==(Bits b1, Bits b2)
            => b1.Equals(b2);

        public static bool operator !=(Bits b1, Bits b2)
            => !b1.Equals(b2);

        public static bool operator <(Bits b1, Bits b2)
            => b1.CompareTo(b2) < 0;
        public static bool operator >(Bits b1, Bits b2)
            => b1.CompareTo(b2) > 0;
        public static bool operator <=(Bits b1, Bits b2)
            => b1.CompareTo(b2) <= 0;
        public static bool operator >=(Bits b1, Bits b2)
            => b1.CompareTo(b2) >= 0;

        #endregion operator: compare

        #region operator: calc

        public static Bits operator !(Bits b)
            => new Bits(b.bits?.Select(v => !v) ?? new Bit[0]);

        public static Bits operator <<(Bits b, int count)
        {
            if (count < 0)
                return b >> (-count);
            var prefix = new Bit[count];
            return new Bits(prefix.Concat(b.bits ?? new Bit[0]));
        }
        public static Bits operator >>(Bits b, int count)
        {
            if (count < 0)
                return b << (-count);
            return new Bits(b.bits?.Skip(count) ?? new Bit[0]);            
        }

        public static Bits operator ^(Bits b1, Bits b2)
        {
            var result = new Bit[Math.Max(b1.Length, b2.Length)];
            for (int i = 0; i<result.Length; ++i)
            {
                var v1 = i < b1.Length ? b1[i] : new Bit();
                var v2 = i < b2.Length ? b2[i] : new Bit();
                result[i] = v1 ^ v2;
            }
            return new Bits(result);
        }
        public static Bits operator &(Bits b1, Bits b2)
        {
            var result = new Bit[Math.Max(b1.Length, b2.Length)];
            for (int i = 0; i < result.Length; ++i)
            {
                var v1 = i < b1.Length ? b1[i] : new Bit();
                var v2 = i < b2.Length ? b2[i] : new Bit();
                result[i] = v1 & v2;
            }
            return new Bits(result);
        }
        public static Bits operator |(Bits b1, Bits b2)
        {
            var result = new Bit[Math.Max(b1.Length, b2.Length)];
            for (int i = 0; i < result.Length; ++i)
            {
                var v1 = i < b1.Length ? b1[i] : new Bit();
                var v2 = i < b2.Length ? b2[i] : new Bit();
                result[i] = v1 | v2;
            }
            return new Bits(result);
        }

        #endregion operator: calc

        #region static methods

        /// <summary>
        /// This will explicit cast each item to a single <see cref="Bit"/> and join all
        /// of them together to <see cref="Bits"/>. This method will perfectly work if 
        /// you only enter 0 and 1 (other values than 0 and 1 are handled like a 1). The
        /// least bit are inserted first.
        /// </summary>
        /// <param name="bits">the bits you want to insert</param>
        /// <returns>a <see cref="Bits"/> value out of the input</returns>
        /// <example>
        /// Bits.Create(0, 1, 0, 1, 1).ToString() == "11010";
        /// </example>
        public static Bits Create(params byte[] bits)
        {
            if (bits == null) throw new ArgumentNullException(nameof(bits));
            return new Bits(bits.Select(b => new Bit(b != 0)));
        }

        /// <summary>
        /// This will explicit cast each item to a single <see cref="Bit"/> and join all
        /// of them together to <see cref="Bits"/>. This method will perfectly work if 
        /// you only enter 0 and 1 (other values than 0 and 1 are handled like a 1). The
        /// highest bit are inserted first.
        /// </summary>
        /// <param name="bits">the bits you want to insert</param>
        /// <returns>a <see cref="Bits"/> value out of the input</returns>
        /// <example>
        /// Bits.Create(0, 1, 0, 1, 1).ToString() == "01011";
        /// </example>
        public static Bits CreateReversed(params byte[] bits)
        {
            if (bits == null) throw new ArgumentNullException(nameof(bits));
            return new Bits(bits.Reverse().Select(b => new Bit(b != 0)));
        }

        public static Bits Concat(params Bits[] bits)
        {
            if (bits == null) throw new ArgumentNullException(nameof(bits));
            return new Bits(bits.SelectMany(b => b.bits ?? new Bit[0]));
        }

        public static Bits Concat(IEnumerable<Bits> bits)
        {
            if (bits == null) throw new ArgumentNullException(nameof(bits));
            return new Bits(bits.SelectMany(b => b.bits ?? new Bit[0]));
        }

        #endregion static methods

        #region cast: * -> Bits

        public static implicit operator Bits(Bit bit)
            => new Bits(bit);

        public static implicit operator Bits(Bit[] bits)
            => new Bits(bits ?? throw new ArgumentNullException(nameof(bits)));

        public static implicit operator Bits(bool[] bits)
            => bits?.Select(b => (Bit)b).ToArray() 
            ?? throw new ArgumentNullException(nameof(bits));

        public static implicit operator Bits(byte value)
        {
            var result = new Bit[8];
            var mask = 0x1;
            for (int i = 0; i < 8; ++i)
            {
                result[i] = (value & mask) == mask;
                mask <<= 1;
            }
            return result;
        }

        public static implicit operator Bits(byte[] value)
            => Concat(value?.Select(b => (Bits)b) 
                ?? throw new ArgumentNullException(nameof(value)));

        public static implicit operator Bits(sbyte value)
            => unchecked((byte)value);

        public static implicit operator Bits(ushort value)
        {
            var result = new byte[2];
            var mask = 0xff;
            for (int i = 0; i < 2; ++i)
            {
                result[i] = (byte)(value & mask);
                mask <<= 8;
            }
            return result;
        }

        public static implicit operator Bits(short value)
            => unchecked((ushort)value);

        public static implicit operator Bits(uint value)
        {
            var result = new byte[4];
            uint mask = 0xff;
            for (int i = 0; i < 4; ++i)
            {
                result[i] = (byte)(value & mask);
                mask <<= 8;
            }
            return result;
        }

        public static implicit operator Bits(int value)
            => unchecked((uint)value);

        public static implicit operator Bits(ulong value)
        {
            var result = new byte[8];
            ulong mask = 0xff;
            for (int i = 0; i < 8; ++i)
            {
                result[i] = (byte)(value & mask);
                mask <<= 8;
            }
            return result;
        }

        public static implicit operator Bits(long value)
            => unchecked((ulong)value);

        #endregion cast: * -> Bits

        #region getter: Bits -> *

        public Bit ToBit(int index)
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index));
            return this[index];
        }

        public Bits ToBits(int index, int length)
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (length < 0 || index + length > Length) throw new ArgumentOutOfRangeException(nameof(length));
            var result = new Bit[length];
            for (int i = 0; i < length; ++i)
                result[i] = this[i + index];
            return new Bits(result);
        }

        public Bit[] ToBitArray()
        {
            return (Bit[])bits.Clone();
        }

        public byte ToByte(int index)
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index));
            byte result = 0;
            byte mask = 0x1;
            for (int i = 0; i<8 && index + i < Length; ++i)
            {
                if (this[index + i])
                    result |= mask;
                mask <<= 1;
            }
            return result;
        }

        public sbyte ToSbyte(int index)
            => unchecked((sbyte)ToByte(index));

        public ushort ToUInt16(int index)
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index));
            ushort result = 0;
            ushort mask = 0x1;
            for (int i = 0; i < 16 && index + i < Length; ++i)
            {
                if (this[index + i])
                    result |= mask;
                mask <<= 1;
            }
            return result;
        }

        public short ToInt16(int index)
            => unchecked((short)ToUInt16(index));

        public uint ToUInt32(int index)
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index));
            uint result = 0;
            uint mask = 0x1;
            for (int i = 0; i < 32 && index + i < Length; ++i)
            {
                if (this[index + i])
                    result |= mask;
                mask <<= 1;
            }
            return result;
        }

        public int ToInt32(int index)
            => unchecked((int)ToUInt32(index));

        public ulong ToUInt64(int index)
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index));
            ulong result = 0;
            ulong mask = 0x1;
            for (int i = 0; i < 64 && index + i < Length; ++i)
            {
                if (this[index + i])
                    result |= mask;
                mask <<= 1;
            }
            return result;
        }

        public long ToInt64(int index)
            => unchecked((long)ToUInt64(index));

        #endregion getter: Bits -> *

        #region cast: Bits -> *

        public static explicit operator Bit(Bits bits)
            => bits.ToBit(0);

        public static implicit operator Bit[](Bits bits)
            => bits.ToBitArray();

        public static explicit operator byte(Bits bits)
            => bits.ToByte(0);

        public static explicit operator sbyte(Bits bits)
            => bits.ToSbyte(0);

        public static explicit operator ushort(Bits bits)
            => bits.ToUInt16(0);

        public static explicit operator short(Bits bits)
            => bits.ToInt16(0);

        public static explicit operator uint(Bits bits)
            => bits.ToUInt32(0);

        public static explicit operator int(Bits bits)
            => bits.ToInt32(0);

        public static explicit operator ulong(Bits bits)
            => bits.ToUInt64(0);

        public static explicit operator long(Bits bits)
            => bits.ToInt64(0);

        #endregion cast: Bits -> *
    }
}
