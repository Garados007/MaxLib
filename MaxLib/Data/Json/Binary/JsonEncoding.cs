using System;
using System.Collections.Generic;
using System.Text;
using MaxLib.Data.BitData;

namespace MaxLib.Data.Json.Binary
{
    public class JsonEncoding
    {
        public Dictionary<string, Bits> ObjectKeys { get; set; }

        public Dictionary<char, Bits> ObjectKeyChars { get; set; }

        public Dictionary<char, Bits> GlobalStringChars { get; set; }

        public Dictionary<string, Dictionary<char, Bits>> ObjectStringChars { get; set; }

        public long SavedBits { get; internal set; }

        public long SavedBytes => SavedBits / 8;
    }
}
