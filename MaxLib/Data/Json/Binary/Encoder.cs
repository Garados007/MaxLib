using System;
using System.Collections.Generic;
using System.Text;
using MaxLib.Data.BitData;
using System.Linq;

namespace MaxLib.Data.Json.Binary
{
    /// <summary>
    /// This class try to create a <see cref="JsonEncoding"/> out of the <see cref="Analyzer"/> test
    /// result
    /// </summary>
    public class Encoder
    {
        private static Encoder defaultEncoder = new Encoder();

        public static Encoder Default
        {
            get => defaultEncoder;
            set => defaultEncoder = value ?? throw new ArgumentNullException(nameof(value));
        }

        protected virtual List<KeyValuePair<T, long>> SortSet<T>(Dictionary<T, long> set)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            var list = new List<KeyValuePair<T, long>>(set);
            list.Sort((p1, p2) => -p1.Value.CompareTo(p2.Value));
            return list;
        }

        /// <summary>
        /// Converts a single int into it <see cref="Bits"/> representation using a 4 bit block
        /// length encoding.
        /// </summary>
        /// <param name="value">the inserted value</param>
        /// <returns>the encoded int</returns>
        /// <example>
        /// GetCompressedInt(3).ToString() == "0
        /// </example>
        public virtual Bits GetCompressedInt(long value)
        {
            var list = new List<Bit>
            {
                value < 0
            };
            if (value < 0)
                value = -value - 1;
            do
            {
                list.Add(value >= 8);
                list.Add((value & 0x1) == 0x1);
                list.Add((value & 0x2) == 0x2);
                list.Add((value & 0x4) == 0x4);
                value >>= 3;
            }
            while (value > 0);
            return list.ToArray();
        }
        
        protected virtual IEnumerable<Tuple<Bits, int, int, int>> GetNumberGroups()
        {
            int border = 0;
            for (int i = 0; i<int.MaxValue; ++i)
            {
                var mask = Bits.Concat(
                    new Bits(Enumerable.Repeat((Bit)1, i)), 
                    new Bits((Bit)0));
                var groups = mask.Length;
                var totalBits = groups * 4;
                var dataBits = totalBits - mask.Length;
                var maxNumber = 0x1 << dataBits;
                yield return new Tuple<Bits, int, int, int>(mask, border, 
                    border + maxNumber - 1, dataBits);
                border += maxNumber;
                //Bits num = i;
                //int lastOne = num.Length - 1;
                //for (; lastOne >= 0; lastOne--)
                //    if (num[lastOne])
                //        break;
                //var mask = num.ToBits(0, lastOne + 2); //including the one and zero
                //int groups = mask.Length;
                //int bitInGroups = groups * 4;
                //int dataBits = bitInGroups - mask.Length;
                //int maxNumber = 0x1 << dataBits;
                //yield return new Tuple<Bits, int, int, int>(mask, border, maxNumber - 1, dataBits);
                //border += maxNumber;
            }
        }

        public virtual IEnumerable<Bits> GetEncodedIds(int maxCount)
        {
            int id = 0;
            foreach (var group in GetNumberGroups())
            {
                if (id >= maxCount) yield break;
                for (; id <= group.Item3 && id < maxCount; ++id)
                {
                    Bits num = id - group.Item2;
                    int dataBits = group.Item4;
                    if (group.Item3 + 1 >= maxCount)
                    {
                        Bits limit = maxCount - group.Item2 - 1;
                        int lastOne = limit.Length - 1;
                        for (; lastOne >= 0; lastOne--)
                            if (limit[lastOne])
                                break;
                        dataBits = lastOne + 1; //including the one
                    }
                    if (group.Item2 == 0 && group.Item3 + 1 >= maxCount)
                        yield return num.ToBits(0, dataBits);
                    else yield return Bits.Concat(group.Item1, num.ToBits(0, dataBits));
                }
            }
        }

        protected virtual Dictionary<T, Bits> GetEncoding<T>(List<T> orderedSet)
        {
            if (orderedSet == null) throw new ArgumentNullException(nameof(orderedSet));
            var result = new Dictionary<T, Bits>();
            int i = 0; 
            foreach (var bits in GetEncodedIds(orderedSet.Count))
            {
                result.Add(orderedSet[i], bits);
                i++;
                if (i >= orderedSet.Count) break;
            }
            return result;
            /*
            int dataBits = 0;
            int groups = -1;
            int max = 0;
            var lastGroup = false;
            for (int i = 0; i<orderedSet.Count; ++i)
            {
                if (i > max || max == 0)
                {
                    groups++; //attach new group
                    //check if a new maximum will enclose all ids
                    if (((max << 4) | 0xf) >= orderedSet.Count - 1)
                    {
                        lastGroup = true;
                        for (int j = 1; j<= 4; ++j)
                        {
                            int nmax = ~((~0) << j);
                            if (((max << j) | nmax) >= orderedSet.Count - 1)
                            {
                                dataBits += j;
                                max = (max << j) | nmax;
                                break;
                            }
                        }
                    }
                    else
                    {
                        dataBits += 3;
                        max = (max << 3) | 0x7;
                    }
                }
                Bit[] bits = new Bit[dataBits + groups + (lastGroup ? 0 : 1)];
                Bits number = i;
                int offset = 0;
                for (int j = 0; j<dataBits; ++j)
                {
                    if ((j%4) == 0 && (!lastGroup || j < groups * 4))
                    {
                        offset += 1;
                        bits[j + offset - 1] = offset != groups + 1;
                    }
                    bits[j + offset] = number[j];
                }
                result.Add(orderedSet[i], bits);
            }
            return result;*/
        }

        protected virtual long GetCompressedDataSize<T>(Dictionary<KeyValuePair<T, long>, Bits> encoding)
        {
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            long size = 0;
            foreach (var item in encoding)
            {
                size += item.Key.Value * item.Value.Length;
            }
            return size;
        }

        protected virtual long GetCompressedTableSize<T>(Dictionary<KeyValuePair<T, long>, Bits> encoding, Func<T, long> keySize)
        {
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            if (keySize == null) throw new ArgumentNullException(nameof(keySize));
            long size = GetCompressedInt(encoding.Count).Length;
            foreach (var item in encoding)
                size += keySize(item.Key.Key);
            return size;
        }

        protected virtual long GetRawDataSize<T>(Dictionary<T, long> data, Func<T, long> keySize)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (keySize == null) throw new ArgumentNullException(nameof(keySize));
            long size = 0;
            foreach (var item in data)
            {
                size += keySize(item.Key) * item.Value;
            }
            return size;
        }

        /// <summary>
        /// Checks if the encoding is good for the dataset. The return value is the bit count that could be saved
        /// if the encoding is applied. If the return value is negative the encoding itself is bad and shouldn't 
        /// applied.
        /// </summary>
        /// <typeparam name="T">type of encoded data</typeparam>
        /// <param name="data">the original data meassurement</param>
        /// <param name="encoding">the encoding including meassurement</param>
        /// <param name="keySize">size function of the data to encode</param>
        /// <returns>saved bits.</returns>
        protected virtual long IsEncodingGood<T>(Dictionary<T, long> data, Dictionary<KeyValuePair<T, long>, Bits> encoding, Func<T, long> keySize)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            if (keySize == null) throw new ArgumentNullException(nameof(keySize));

            var compTable = GetCompressedTableSize(encoding, keySize);
            var compData = GetCompressedDataSize(encoding);
            var rawData = GetRawDataSize(data, keySize);

            return rawData - compTable - compData;
        }

        protected virtual Dictionary<T1, Dictionary<T2, Bits>> GetBestEncodings<T1, T2>(Dictionary<T1, Dictionary<T2, long>> data, Func<T2, long> keySize, out long savedBits)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (keySize == null) throw new ArgumentNullException(nameof(keySize));
            var globalResult = new Dictionary<T1, Dictionary<T2, Bits>>();
            savedBits = 0;
            foreach (var entry in data)
            {
                var sorted = SortSet(entry.Value);
                var encoding = GetEncoding(sorted);
                long saved;
                if ((saved = IsEncodingGood(entry.Value, encoding, keySize)) > 0)
                {
                    var result = new Dictionary<T2, Bits>();
                    foreach (var item in encoding)
                        result.Add(item.Key.Key, item.Value);
                    globalResult.Add(entry.Key, result);
                    savedBits += saved;
                }
            }
            return globalResult;
        }

        protected virtual long CharSize(char value)
        {
            return Encoding.UTF8.GetByteCount(value.ToString()) * 8;
        }

        protected virtual long StringSize(string value)
        {
            return Encoding.UTF8.GetByteCount(value) * 8 + GetCompressedInt(value.Length).Length;
        }

        protected virtual Dictionary<T, Bits> ReduceEncoding<T>(Dictionary<KeyValuePair<T, long>, Bits> encoding)
        {
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            var result = new Dictionary<T, Bits>();
            foreach (var item in encoding)
                result.Add(item.Key.Key, item.Value);
            return result;
        }

        /// <summary>
        /// Create a <see cref="JsonEncoding"/> out of a <see cref="Analyzer"/> test result
        /// </summary>
        /// <param name="analyzer">the test result</param>
        /// <returns>the new <see cref="JsonEncoding"/></returns>
        public virtual JsonEncoding CreateEncoding(Analyzer analyzer)
        {
            if (analyzer == null) throw new ArgumentNullException(nameof(analyzer));

            Dictionary<KeyValuePair<string, long>, Bits> objectKeys = null;
            Dictionary<KeyValuePair<char, long>, Bits> objectKeyChars = null;
            Dictionary<KeyValuePair<char, long>, Bits> objectKeyCharUnique = null;
            Dictionary<KeyValuePair<char, long>, Bits> globalStringChars = null;
            Dictionary<string, Dictionary<char, Bits>> objectStringChars = null;
            long bitsOK = 0, bitsOKC = 0, bitsOKCU = 0, bitsGSC = 0, bitsOSC = 0;

            if (analyzer.ObjectKeys != null)
            {
                var enc = GetEncoding(SortSet(analyzer.ObjectKeys));
                if ((bitsOK = IsEncodingGood(analyzer.ObjectKeys, enc, StringSize)) > 0)
                    objectKeys = enc;
            }
            if (analyzer.ObjectKeyChars != null)
            {
                var enc = GetEncoding(SortSet(analyzer.ObjectKeyChars));
                if ((bitsOKC = IsEncodingGood(analyzer.ObjectKeyChars, enc, CharSize)) > 0)
                    objectKeyChars = enc;
            }
            if (analyzer.ObjectKeyCharUnique != null)
            {
                var enc = GetEncoding(SortSet(analyzer.ObjectKeyCharUnique));
                if ((bitsOKCU = IsEncodingGood(analyzer.ObjectKeyCharUnique, enc, CharSize)) > 0)
                    objectKeyCharUnique = enc;

            }
            if (analyzer.GlobalStringChars != null)
            {
                var enc = GetEncoding(SortSet(analyzer.GlobalStringChars));
                if ((bitsGSC = IsEncodingGood(analyzer.GlobalStringChars, enc, CharSize)) > 0)
                    globalStringChars = enc;
            }
            if (analyzer.ObjectStringChars != null)
            {
                objectStringChars = GetBestEncodings(analyzer.ObjectStringChars, CharSize, out bitsOSC);
                if (objectStringChars.Count == 0)
                    objectStringChars = null;
            }

            //get only best encodings methods

            if (objectKeys == null && objectKeyCharUnique != null)
                objectKeyCharUnique = null;

            if (objectKeys != null && objectKeyChars != null && objectKeyCharUnique != null)
            {
                if (bitsOKCU + bitsOK > bitsOKC)
                    objectKeyChars = null;
                else objectKeyCharUnique = null;
            }

            if (objectKeys != null && objectKeyChars != null && objectKeyCharUnique == null)
            {
                if (bitsOK > bitsOKC)
                    objectKeyChars = null;
                else objectKeys = null;
            }

            if (globalStringChars != null && objectStringChars != null)
            {
                //fix the table headings
                bitsOSC -= GetCompressedInt(objectStringChars.Count).Length;
                var okc = objectKeyChars != null ? ReduceEncoding(objectKeyChars) : null;
                foreach (var key in objectStringChars.Keys)
                    bitsOSC -= EncodedStringSize(objectKeys, okc, key);

                if (bitsGSC > bitsOSC)
                    objectStringChars = null;
                else globalStringChars = null;
            }

            return new JsonEncoding
            {
                ObjectKeys = objectKeys != null ? ReduceEncoding(objectKeys) : null,
                ObjectKeyChars = objectKeyChars != null ? ReduceEncoding(objectKeyChars)
                    : objectKeyCharUnique != null ? ReduceEncoding(objectKeyCharUnique) : null,
                GlobalStringChars = globalStringChars != null ? ReduceEncoding(globalStringChars) : null,
                ObjectStringChars = objectStringChars,
                SavedBits =
                    (objectKeys != null ? bitsOK : 0)
                    + (objectKeyChars != null ? bitsOKC : 0)
                    + (objectKeyCharUnique != null ? bitsOKCU : 0)
                    + (globalStringChars != null ? bitsGSC : 0)
                    + (objectStringChars != null ? bitsOSC : 0),
            };
        }

        protected virtual long EncodedStringSize(Dictionary<KeyValuePair<string, long>, Bits> objectKeys,
            Dictionary<char, Bits> objectKeyChars, string value)
        {
            if (objectKeys != null)
            {
                var item = objectKeys.FirstOrDefault(e => e.Key.Key == value);
                if (Equals(item, default(KeyValuePair<KeyValuePair<string, long>, Bits>)))
                    return item.Value.Length;
            }
            if (objectKeyChars != null)
            {
                long length = GetCompressedInt(value.Length).Length;
                foreach (var c in value)
                    if (objectKeyChars.TryGetValue(c, out Bits bits))
                        length += bits.Length;
                    else length += 32; //shouldn't be possible in normal case
                return length;
            }
            return StringSize(value);
        }
    }
}
