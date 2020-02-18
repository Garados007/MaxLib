using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//Keine Fehlersicherheit eingebaut!!!

namespace MaxLib.Data.Json
{
    public class JsonParser
    {
        public static JsonParser SingleLine
        {
            get { return new JsonParser() { BeatifulOutput = false, IndentCharCount = 0 }; }
        }
        public static JsonParser NoIndent
        {
            get { return new JsonParser() { BeatifulOutput = true, IndentCharCount = 0 }; }
        }
        public static JsonParser SmallIndent
        {
            get { return new JsonParser() { BeatifulOutput = true, IndentCharCount = 2 }; }
        }
        public static JsonParser MediumIndent
        {
            get { return new JsonParser() { BeatifulOutput = true, IndentCharCount = 4 }; }
        }
        public static JsonParser LargeIndent
        {
            get { return new JsonParser() { BeatifulOutput = true, IndentCharCount = 8 }; }
        }

        public bool BeatifulOutput { get; set; }

        public int IndentCharCount { get; set; }

        public JsonParser()
        {
            BeatifulOutput = true;
            IndentCharCount = 2;
        }

        public static string ToLiteral(string input)
        {
            var literal = new StringBuilder(input.Length + 2);
            //literal.Append("\"");
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\'': literal.Append(@"\'"); break;
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        if (Char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.Control)
                        {
                            literal.Append(c);
                        }
                        else
                        {
                            literal.Append(@"\u");
                            literal.Append(((ushort)c).ToString("x4"));
                        }
                        break;
                }
            }
            //literal.Append("\"");
            return literal.ToString();
        }
        public static string FromLiteral(string input)
        {
            var sb = new StringBuilder(input.Length);
            for (int i = 0; i<input.Length; ++i)
            {
                if (input[i]!='\\') sb.Append(input[i]);
                else
                {
                    ++i;
                    switch (input[i])
                    {
                        case '\'': sb.Append('\''); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '0': sb.Append('\0'); break;
                        case 'a': sb.Append('\a'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'v': sb.Append('\v'); break;
                        case '/': sb.Append('/'); break;
                        case 'u':
                            {
                                var hex = new StringBuilder();
                                for (int ic = i + 1; ic < Math.Min(input.Length, i + 5); ++ic) hex.Append(input[ic]);
                                i += hex.Length;
                                sb.Append((char)ushort.Parse(hex.ToString(), System.Globalization.NumberStyles.AllowHexSpecifier));
                            } break;
                        default: sb.Append('\\' + input[i]); break;
                    }
                }
            }
            return sb.ToString();
        }

        public string Parse(JsonElement element)
        {
            return element.ToString(this);
        }
        public JsonElement Parse(string text)
        {
            return new Parser{ Text = text }.ParseElement() ?? new JsonValue{ ArgumentString = "" };
        }

        class Parser
        {
            public JsonElement ParseElement()
            {
                var pb = GetNextBase();
                if (pb==null) return null;
                switch (pb.Type)
                {
                    case ParserBaseType.ObjectOpen:
                        {
                            var obj = new JsonObject();
                            while ((pb = GetNextBase()).Type!= ParserBaseType.ObjectClose)
                            {
                                var name = pb.Text;
                                if (name.StartsWith("\"")) name = name.Substring(1);
                                if (name.EndsWith("\"")) name = name.Remove(name.Length - 1);
                                pb = GetNextBase(); //Zuweisung
                                obj.Add(name, ParseElement());
                                pb = GetNextBase(); //Trenner
                                if (pb.Type == ParserBaseType.ObjectClose) break;
                            }
                            return obj;
                        }
                    case ParserBaseType.ArrayOpen:
                        {
                            var arr = new JsonArray();
                            var pos = StorePos();
                            while((pb=GetNextBase()).Type!=ParserBaseType.ArrayClose)
                            {
                                RestorePos(pos);
                                arr.Add(ParseElement());
                                pb = GetNextBase();
                                if (pb.Type == ParserBaseType.ArrayClose) break;
                                pos = StorePos();
                            }
                            return arr;
                        }
                    case ParserBaseType.Value:
                        {
                            return new JsonValue
                            {
                                ArgumentString = pb.Text
                            };
                        }
                    default: return null;
                }
            }

            #region Base Parsing
            public string Text;
            public int Line = 1, Position = 0, Index = 0;
            public bool isLiteral = false;

            Tuple<int,int,int> StorePos()
            {
                return new Tuple<int, int, int>(Line, Position, Index);
            }
            void RestorePos(Tuple<int,int,int> pos)
            {
                Line = pos.Item1; Position = pos.Item2; Index = pos.Item3;
            }
            ParserBase GetNextBase()
            {
                var pb = new ParserBase();
                char c;
                do c = GetNextChar();
                while ((c == ' ' || c == '\t' || c == '\r' || c == '\n') && c != 0);
                if (c == 0) return null;
                pb.Pos = StorePos();
                pb.Text = c.ToString();
                switch (c)
                {
                    case '{': pb.Type = ParserBaseType.ObjectOpen; return pb;
                    case '}': pb.Type = ParserBaseType.ObjectClose; return pb;
                    case '[': pb.Type = ParserBaseType.ArrayOpen; return pb;
                    case ']': pb.Type = ParserBaseType.ArrayClose; return pb;
                    case ':': pb.Type = ParserBaseType.NameSetter; return pb;
                    case ',': pb.Type = ParserBaseType.ArraySeperator; return pb;
                    default:
                        {
                            pb.Type = ParserBaseType.Value;
                            if (c == '"') isLiteral = true;
                            var lc = c;
                            var rp = StorePos();
                            bool first = true;
                            bool mask = false;
                            do
                            {
                                if (!first) pb.Text += c;
                                first = false;
                                lc = c; 
                                rp = StorePos(); 
                                c = GetNextChar();
                                if (c == '"' && isLiteral)
                                {
                                    pb.Text += c;
                                    first = true;
                                }
                                else if (c=='\\' && isLiteral)
                                {
                                    mask = lc != '\\';
                                }
                            }
                            while (c != 0 && 
                                (isLiteral ? 
                                    (c != '"' || (lc == '\\' && mask)) : 
                                    (c != ' ' && c != '\t' && c != '}' && c != ']' && c != ',')));
                            if (c == 0) return pb;
                            if (!isLiteral) RestorePos(rp);
                            else isLiteral = false;
                            return pb;
                        }
                }
            }
            char GetNextChar()
            {
                if (Index >= Text.Length) return (char)0;
                var c = Text[Index]; Index++; Position++;
                if (c=='\r')
                {
                    if (Index >= Text.Length) return (char)0;
                    c = Text[Index];
                    Index++;
                    if (c != '\n') { Line++; Position = 0; }
                }
                if (c=='\n')
                {
                    if (Index >= Text.Length) return (char)0;
                    c = Text[Index];
                    Index++; Line++; Position = 0;
                }
                return c;

            }
            class ParserBase
            {
                public Tuple<int, int, int> Pos;
                public string Text;
                public ParserBaseType Type;
            }
            enum ParserBaseType
            {
                ObjectOpen,
                ObjectClose,
                ArrayOpen,
                ArrayClose,
                NameSetter,
                Value,
                ArraySeperator
            }
            #endregion
        }
    }

    [Serializable]
    public abstract class JsonElement
    {
        public string Json
        {
            get { return ToString(); }
        }

        public abstract int ChildCount { get; }

        public abstract bool TidySingleLine { get; }

        public override string ToString()
        {
            return ToString(new JsonParser());
        }
        public virtual string ToString(JsonParser parser)
        {
            if (parser == null) throw new ArgumentNullException("parser");
            var sb = new StringBuilder();
            ToString(parser, sb, 0);
            return sb.ToString();
        }
        public abstract void ToString(JsonParser parser, StringBuilder sb, int depth);

        public JsonObject Object
        {
            get { return this as JsonObject; }
        }
        public JsonArray Array
        { get { return this as JsonArray; } }
        public JsonValue Value
        { get { return this as JsonValue; } }
    }

    [Serializable]
    public class JsonObject : JsonElement, IEnumerable<KeyValuePair<string, JsonElement>>
    {
        internal Dictionary<string, JsonElement> Elements = new Dictionary<string, JsonElement>();

        object lockObject = new object();
        public JsonObject Add<T>(string name, T element) where T : JsonElement
        {
            if (name == null) throw new ArgumentNullException("name");
            if (element == null) throw new ArgumentNullException("element");
            lock (lockObject)
            {
                if (Elements.ContainsKey(name)) Elements[name] = element;
                else Elements.Add(name, element);
            }
            return this;
        }

        public T Get<T>(string name) where T : JsonElement
        {
            if (!Elements.ContainsKey(name)) return null;
            return (T)Elements[name];
        }

        public JsonObject Remove(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            lock (lockObject)
            {
                Elements.Remove(name);
            }
            return this;
        }

        public bool Contains(string name)
        {
            return Elements.ContainsKey(name);
        }

        public JsonElement this[string name]
        {
            get { return Get<JsonElement>(name); }
            set { Add(name, value); }
        }

        public override void ToString(JsonParser parser, StringBuilder sb, int depth)
        {
            if (parser == null) throw new ArgumentNullException("parser");
            if (sb == null) throw new ArgumentNullException("sb");
            if (depth < 0) throw new ArgumentOutOfRangeException("depth", depth, "depth must be greater or equal 0");

            if (parser.BeatifulOutput)
            {
                if (Elements.Count == 0) sb.Append("{}");
                else if (TidySingleLine)
                {
                    sb.Append("{ \"");
                    sb.Append(JsonParser.ToLiteral(Elements.ElementAt(0).Key));
                    sb.Append("\": ");
                    Elements.ElementAt(0).Value.ToString(parser, sb, depth + 1);
                    sb.Append(" }");
                }
                else
                {
                    sb.AppendLine("{");
                    for (int i = 0; i<Elements.Count; ++i)
                    {
                        if (i > 0) sb.AppendLine(",");
                        var kvp = Elements.ElementAt(i);
                        sb.Append(' ', parser.IndentCharCount * (depth + 1));
                        sb.Append('\"');
                        sb.Append(JsonParser.ToLiteral(kvp.Key));
                        sb.Append("\": ");
                        kvp.Value.ToString(parser, sb, depth + 1);
                    }
                    sb.AppendLine();
                    sb.Append(' ', parser.IndentCharCount * depth);
                    sb.Append('}');
                }
            }
            else
            {
                sb.Append('{');
                for (int i = 0; i<Elements.Count; ++i)
                {
                    var kvp = Elements.ElementAt(i);
                    if (i > 0) sb.Append(',');
                    sb.Append('\"');
                    sb.Append(JsonParser.ToLiteral(kvp.Key));
                    sb.Append("\":");
                    kvp.Value.ToString(parser, sb, depth + 1);
                }
                sb.Append('}');
            }
        }

        public IEnumerator<KeyValuePair<string, JsonElement>> GetEnumerator()
        {
            return Elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Elements.GetEnumerator();
        }

        public override int ChildCount
        {
            get { return Elements.Count; }
        }

        public override bool TidySingleLine
        {
            get
            {
                if (Elements.Count > 1) return false;
                return Elements.Count == 0 || Elements.ElementAt(0).Value.TidySingleLine;
            }
        }
    }

    [Serializable]
    public class JsonArray: JsonElement, IEnumerable<JsonElement>
    {
        List<JsonElement> Elements = new List<JsonElement>();

        public JsonArray Add<T>(T element) where T : JsonElement
        {
            if (element == null) throw new ArgumentNullException("element");
            Elements.Add(element);
            return this;
        }

        public T Get<T>(int index) where T: JsonElement
        {
            if (index < 0 || index >= ChildCount) throw new ArgumentOutOfRangeException("index");
            return (T)Elements[index];
        }

        public JsonArray RemoveLast()
        {
            if (Elements.Count > 0)
                Elements.RemoveAt(Elements.Count - 1);
            return this;
        }

        public JsonElement this[int index]
        {
            get { return Elements[index]; }
            set
            {
                if (index < 0 || index > ChildCount) throw new ArgumentOutOfRangeException("index");
                if (value == null) throw new ArgumentNullException();
                if (index == ChildCount) Add(value);
                else Elements[index] = value;
            }
        }

        public override void ToString(JsonParser parser, StringBuilder sb, int depth)
        {
            if (parser == null) throw new ArgumentNullException("parser");
            if (sb == null) throw new ArgumentNullException("sb");
            if (depth < 0) throw new ArgumentOutOfRangeException("depth", depth, "depth must be greater or equal 0");

            if (parser.BeatifulOutput)
            {
                if (Elements.Count == 0) sb.Append("[]");
                else if (TidySingleLine)
                {
                    sb.Append("[ ");
                    Elements[0].ToString(parser, sb, depth + 1);
                    sb.Append(" ]");
                }
                else
                {
                    sb.AppendLine("[");
                    for (int i = 0; i < Elements.Count; ++i)
                    {
                        if (i > 0) sb.AppendLine(",");
                        var kvp = Elements.ElementAt(i);
                        sb.Append(' ', parser.IndentCharCount * (depth + 1));
                        Elements[i].ToString(parser, sb, depth + 1);
                    }
                    sb.AppendLine();
                    sb.Append(' ', parser.IndentCharCount * depth);
                    sb.Append(']');
                }
            }
            else
            {
                sb.Append('[');
                for (int i = 0; i < Elements.Count; ++i)
                {
                    var kvp = Elements.ElementAt(i);
                    if (i > 0) sb.Append(',');
                    Elements[i].ToString(parser, sb, depth + 1);
                }
                sb.Append(']');
            }
        }

        public IEnumerator<JsonElement> GetEnumerator()
        {
            return Elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Elements.GetEnumerator();
        }

        public override int ChildCount
        {
            get { return Elements.Count; }
        }

        public override bool TidySingleLine
        {
            get
            {
                if (Elements.Count > 1) return false;
                return Elements.Count == 0 || Elements[0].TidySingleLine;
            }
        }
    }

    [Serializable]
    public class JsonValue : JsonElement
    {
        private string argumentString = "";
        public string ArgumentString
        {
            get { return argumentString; }
            set
            {
                argumentString = value ?? throw new ArgumentNullException("ArgumentString");
            }
        }

        public override int ChildCount
        {
            get { return 0; }
        }

        public override bool TidySingleLine
        {
            get { return true; }
        }

        public override void ToString(JsonParser parser, StringBuilder sb, int depth)
        {
            sb.Append(ArgumentString);
        }

        string ArgStringExpr()
        {
            var s = ArgumentString;
            if (s.StartsWith("\"")) s = s.Substring(1);
            if (s.EndsWith("\"")) s = s.Remove(s.Length - 1);
            return s;
        }
        string NumStringExpr()
        {
            if ((5.3).ToString().Contains(',')) return ArgumentString.Replace('.', ',');
            else return ArgumentString;
        }

        public bool IsNull()
        {
            return ArgumentString == "null";
        }

        public T Get<T>()
        {
            if (CheckClass<object, T>()) throw new InvalidOperationException();
            if (CheckClass<bool, T>()) return (T)(object)bool.Parse(NumStringExpr());
            if (CheckClass<byte, T>()) return (T)(object)byte.Parse(NumStringExpr());
            if (CheckClass<sbyte, T>()) return (T)(object)sbyte.Parse(NumStringExpr());
            if (CheckClass<short, T>()) return (T)(object)short.Parse(NumStringExpr());
            if (CheckClass<ushort, T>()) return (T)(object)ushort.Parse(NumStringExpr());
            if (CheckClass<int, T>()) return (T)(object)int.Parse(NumStringExpr());
            if (CheckClass<uint, T>()) return (T)(object)uint.Parse(NumStringExpr());
            if (CheckClass<long, T>()) return (T)(object)long.Parse(NumStringExpr());
            if (CheckClass<ulong, T>()) return (T)(object)ulong.Parse(NumStringExpr());
            if (CheckClass<float, T>()) return (T)(object)float.Parse(NumStringExpr());
            if (CheckClass<double, T>()) return (T)(object)double.Parse(NumStringExpr());
            if (CheckClass<decimal, T>()) return (T)(object)decimal.Parse(NumStringExpr());
            if (CheckClass<char, T>()) return (T)(object)JsonParser.FromLiteral(ArgStringExpr())[0];
            if (CheckClass<string, T>()) return IsNull() ? (T)(object)null : (T)(object)JsonParser.FromLiteral(ArgStringExpr());
            throw new InvalidOperationException();
        }

        public void Set<T>(T value)
        {
            if (value == null) ArgumentString = "null";
            else if (CheckClass<bool, T>() || CheckClass<byte, T>() || CheckClass<sbyte, T>() || CheckClass<short, T>() ||
                CheckClass<ushort, T>() || CheckClass<int, T>() || CheckClass<uint, T>() || CheckClass<long, T>() ||
                CheckClass<ulong, T>() || CheckClass<float, T>() || CheckClass<decimal, T>() || CheckClass<double, T>())
            {
                ArgumentString = value.ToString().Replace(',', '.').ToLower();
            }
            else if (CheckClass<char, T>() || CheckClass<string, T>())
            {
                ArgumentString = '"' + JsonParser.ToLiteral(value.ToString()) + '"';
            }
            else throw new InvalidOperationException();
        }

        bool CheckClass<T1,T2>()
        {
            var t1 = typeof(T1);
            var t2 = typeof(T2);
            if (t1.AssemblyQualifiedName == t2.AssemblyQualifiedName) return true;
            return t1.IsSubclassOf(t2);
        }

        public static JsonValue Create<T>(T value)
        {
            var val = new JsonValue();
            val.Set(value);
            return val;
        }

        public static JsonValue Null => Create<object>(null);
    }
}
