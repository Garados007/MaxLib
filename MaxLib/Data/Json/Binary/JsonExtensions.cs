using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Data.Json.Binary
{
    public static class JsonExtensions
    {
        public static bool IsBool(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<bool>(); return true; }
            catch { return false; }
        }
        public static bool IsByte(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<byte>(); return true; }
            catch { return false; }
        }
        public static bool IsSByte(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<sbyte>(); return true; }
            catch { return false; }
        }
        public static bool IsInt16(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<short>(); return true; }
            catch { return false; }
        }
        public static bool IsUInt16(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<ushort>(); return true; }
            catch { return false; }
        }
        public static bool IsInt32(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<int>(); return true; }
            catch { return false; }
        }
        public static bool IsUInt32(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<uint>(); return true; }
            catch { return false; }
        }
        public static bool IsInt64(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<long>(); return true; }
            catch { return false; }
        }
        public static bool IsUInt64(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<ulong>(); return true; }
            catch { return false; }
        }
        public static bool IsSingle(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<float>(); return true; }
            catch { return false; }
        }
        public static bool IsDouble(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<double>(); return true; }
            catch { return false; }
        }
        public static bool IsDecimal(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<decimal>(); return true; }
            catch { return false; }
        }
        public static bool IsChar(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<char>(); return true; }
            catch { return false; }
        }
        public static bool IsString(this JsonValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try { value.Get<string>(); return true; }
            catch { return false; }
        }
    }
}
