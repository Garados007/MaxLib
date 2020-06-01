using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MaxLib.Data.BitData;

namespace MaxLib.Data.Json.Binary
{
    public class EncodedWriter : IDisposable
    {
        protected BitsWriter Writer { get; private set; }

        public Stream BaseStream { get; private set; }


        public JsonEncoding Encoding { get; private set; }

        public EncodedWriter(Stream baseStream)
            : this(baseStream, false)
        {

        }

        public EncodedWriter(Stream baseStream, bool leaveOpen)
        {
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            Writer = new BitsWriter(baseStream, leaveOpen);
        }

        public void Dispose()
        {
            Writer.Dispose();
        }

        public bool HasUnflushedBits => Writer.HasUnflushedBits;

        public virtual void Flush() 
            => Writer.Flush();

        protected virtual void TableWrite(char value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value.ToString());
            var length = ((Bits)(bytes.Length - 1)).ToBits(0, 2); // the size is between 1 and 4
            Writer.WriteBits(length);
            Writer.WriteBits(bytes);
        }

        protected virtual void StringWrite(string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            var length = Encoder.Default.GetCompressedInt(bytes.Length);
            Writer.WriteBits(length);
            Writer.WriteBits(bytes);
        }

        protected virtual void StringWrite(string value, Dictionary<char, Bits> table)
        {
            var length = Encoder.Default.GetCompressedInt(value.Length);
            Writer.WriteBits(length);
            for (int i = 0; i<value.Length; ++i)
            {
                var bits = table[value[i]];
                Writer.WriteBits(bits);
            }
        }

        public virtual void Write(JsonEncoding encoding, bool replace = true)
        {
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            if (replace)
                Encoding = encoding;

            Bits header = new[]
            {
                encoding.ObjectKeyChars != null,
                encoding.ObjectKeys != null,
                encoding.GlobalStringChars != null,
                encoding.ObjectStringChars != null,
                false, //reserved for future use
                false, //reserved for future use
                false, //reserved for future use
                false, //reserved for future use
            };
            Writer.WriteBits(header);
            if (encoding.ObjectKeyChars != null)
            {
                Writer.WriteBits(Encoder.Default.GetCompressedInt(encoding.ObjectKeyChars.Count));
                foreach (var item in encoding.ObjectKeyChars)
                    TableWrite(item.Key);
            }
            if (encoding.ObjectKeys != null)
            {
                Writer.WriteBits(Encoder.Default.GetCompressedInt(encoding.ObjectKeys.Count));
                foreach (var item in encoding.ObjectKeys)
                    if (encoding.ObjectKeyChars != null)
                        StringWrite(item.Key, encoding.ObjectKeyChars);
                    else StringWrite(item.Key);
            }
            if (encoding.GlobalStringChars != null)
            {
                Writer.WriteBits(Encoder.Default.GetCompressedInt(encoding.GlobalStringChars.Count));
                foreach (var item in encoding.GlobalStringChars)
                    TableWrite(item.Key);
            }
            if (encoding.ObjectStringChars != null)
            {
                Writer.WriteBits(Encoder.Default.GetCompressedInt(encoding.ObjectStringChars.Count));
                foreach (var table in encoding.ObjectStringChars)
                {
                    if (encoding.ObjectKeys != null)
                        Writer.WriteBits(encoding.ObjectKeys[table.Key]);
                    else if (encoding.ObjectKeyChars != null)
                        StringWrite(table.Key, encoding.ObjectKeyChars);
                    else StringWrite(table.Key);
                    Writer.WriteBits(Encoder.Default.GetCompressedInt(table.Value.Count));
                    foreach (var item in table.Value)
                        TableWrite(item.Key);
                }
            }
        }

        public virtual void Write(JsonElement json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            if (Encoding == null)
                throw new NotSupportedException($"{nameof(Encoding)} is not set");
            Write(json, Encoding);
        }

        public virtual void Write(JsonElement json, JsonEncoding encoding)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));

            Write("", json, encoding);
        }

        protected virtual void Write(string parent, JsonElement json, JsonEncoding encoding)
        {
            if (json.Value != null)
                Write(parent, json.Value, encoding);
            if (json.Array != null)
                Write(parent, json.Array, encoding);
            if (json.Object != null)
                Write(parent, json.Object, encoding);
        }

        protected virtual void Write(string parent, JsonValue json, JsonEncoding encoding)
        {
            if (json.IsNull())
            {
                Writer.WriteBits(Bits.Create(0, 0, 0));
            }
            else if (json.IsInt64())
            {
                Writer.WriteBits(Bits.Create(1, 0, 0));
                Writer.WriteBits(Encoder.Default.GetCompressedInt(json.Get<long>()));
            }
            else if (json.IsSingle())
            {
                Writer.WriteBits(Bits.Create(0, 1, 0, 0));
                Writer.WriteBits(BitConverter.GetBytes(json.Get<float>()));
            }
            else if (json.IsDouble())
            {
                Writer.WriteBits(Bits.Create(0, 1, 0, 1));
                Writer.WriteBits(BitConverter.GetBytes(json.Get<double>()));
            }
            else if (json.IsString())
            {
                Dictionary<char, Bits> dict = null;
                if (encoding.ObjectKeyChars == null || 
                    !encoding.ObjectStringChars.TryGetValue(parent, out dict))
                    dict = encoding.GlobalStringChars;
                if (dict != null)
                    StringWrite(json.Get<string>(), dict);
                else StringWrite(json.Get<string>());
            }
            else if (json.IsBool())
            {
                Writer.WriteBits(Bits.CreateReversed(0, 1, 1));
                Writer.WriteBits((Bit)json.Get<bool>());
            }
        }

        protected virtual void Write(string parent, JsonArray json, JsonEncoding encoding)
        {
            Writer.WriteBits(Bits.Create(0, 0, 1));
            foreach (var items in json)
            {
                Write(parent, items, encoding);
            }
            Writer.WriteBits(Bits.Create(1, 1, 1));
        }

        protected virtual void Write(string parent, JsonObject json, JsonEncoding encoding)
        {
            Writer.WriteBits(Bits.Create(1, 0, 1));

            Writer.WriteBits(Encoder.Default.GetCompressedInt(json.Elements.Count));
            foreach (var item in json)
            {
                if (encoding.ObjectKeys != null)
                    Writer.WriteBits(encoding.ObjectKeys[item.Key]);
                else if (encoding.ObjectKeyChars != null)
                    StringWrite(item.Key, encoding.ObjectKeyChars);
                else StringWrite(item.Key);
                Write(item.Key, item.Value, encoding);
            }
        }
    }
}
