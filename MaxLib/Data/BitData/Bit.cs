using System;

namespace MaxLib.Data.BitData
{
    public struct Bit : IComparable, IComparable<Bit>, IConvertible, IEquatable<Bit>, IFormattable
    {
        public bool Set { get; set; }

        public Bit(bool set)
        {
            Set = set;
        }

        public override string ToString()
            => Set ? "1" : "0";

        #region IComparable<Bit>

        public int CompareTo(Bit other)
        {
            if (Set == other.Set)
                return 0;
            if (!Set)
                return -1;
            else return 1;
        }

        #endregion IComparable<Bit>

        #region IComparable

        int IComparable.CompareTo(object obj)
        {
            return obj is Bit bit ? CompareTo(bit) : 0;
        }

        #endregion IComparable

        #region IConvertible

        public TypeCode GetTypeCode()
        {
            return TypeCode.Boolean;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToBoolean(provider);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToByte(provider);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToChar(provider);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToDateTime(provider);
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToDecimal(provider);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToDouble(provider);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToInt16(provider);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToInt32(provider);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToInt64(provider);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToSByte(provider);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToSingle(provider);
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return (Set ? 1 : 0).ToString(provider);
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return ((IConvertible)Set).ToType(conversionType, provider);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToUInt16(provider);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToUInt32(provider);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return ((IConvertible)Set).ToUInt64(provider);
        }

        #endregion IConvertible
        
        #region IFormattable

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return (Set ? 1 : 0).ToString(format, formatProvider); 
        }

        #endregion IFormattable

        public bool Equals(Bit other)
        {
            return Set == other.Set;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Bit bit)
                return Equals(bit);
            else return false;
        }

        public override int GetHashCode()
        {
            return Set.GetHashCode();
        }

        #region static operators

        #region operator: compare

        public static bool operator ==(Bit b1, Bit b2)
            => b1.Set == b2.Set;

        public static bool operator !=(Bit b1, Bit b2)
            => b1.Set == b2.Set;

        public static bool operator <(Bit b1, Bit b2)
            => b1.CompareTo(b2) < 0;
        public static bool operator >(Bit b1, Bit b2)
            => b1.CompareTo(b2) > 0;
        public static bool operator <=(Bit b1, Bit b2)
            => b1.CompareTo(b2) <= 0;
        public static bool operator >=(Bit b1, Bit b2)
            => b1.CompareTo(b2) >= 0;

        #endregion operator: compare

        #region operator: calc

        public static Bit operator !(Bit b)
            => new Bit(!b.Set);

        public static Bit operator ^(Bit b1, Bit b2)
            => new Bit(b1.Set ^ b2.Set);
        public static Bit operator &(Bit b1, Bit b2)
            => new Bit(b1.Set & b2.Set);
        public static Bit operator |(Bit b1, Bit b2)
            => new Bit(b1.Set | b2.Set);


        #endregion operator: calc

        #region cast: Bit -> *

        public static implicit operator bool(Bit b)
            => b.Set;

        public static implicit operator byte(Bit b)
            => b.Set ? (byte)1 : (byte)0;

        public static implicit operator sbyte(Bit b)
            => b.Set ? (sbyte)1 : (sbyte)0;

        public static implicit operator short(Bit b)
            => (byte)b;

        public static implicit operator ushort(Bit b)
            => (byte)b;

        public static implicit operator int(Bit b)
            => (byte)b;

        public static implicit operator uint(Bit b)
            => (byte)b;

        public static implicit operator long(Bit b)
            => (byte)b;

        public static implicit operator ulong(Bit b)
            => (byte)b;

        #endregion cast: Bit -> *

        #region cast: * -> Bit

        public static implicit operator Bit(bool b)
            => new Bit(b);

        public static explicit operator Bit(byte b)
            => new Bit(b != 0);

        public static explicit operator Bit(sbyte b)
            => new Bit(b != 0);

        public static explicit operator Bit(int b)
            => new Bit(b != 0);

        public static explicit operator Bit(uint b)
            => new Bit(b != 0);

        public static explicit operator Bit(long b)
            => new Bit(b != 0);

        public static explicit operator Bit(ulong b)
            => new Bit(b != 0);

        #endregion cast: * -> Bit

        #endregion static operators
    }
}
