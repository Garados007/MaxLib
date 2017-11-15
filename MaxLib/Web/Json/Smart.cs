using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Web.Json
{
    namespace Smart
    {
        public static class JsonExtensions
        {
            public static string ParseSmart(this JsonParser parser, SmartJson element)
            {
                return element.ToString(parser);
            }

            public static SmartJson ParseSmart(this JsonParser parser, string text)
            {
                return new SmartJson(parser.Parse(text));
            }

            public static SmartJson ToSmart(this JsonElement element)
            {
                return new SmartJson(element);
            }

            public static JsonElement ToElement(this SmartJson element)
            {
                return element.Element;
            }

            public static JsonElement Parse(this JsonParser parser, byte[] data, Encoding encoding = null)
            {
                return parser.Parse((encoding ?? Encoding.Default).GetString(data));
            }

            public static SmartJson ParseSmart(this JsonParser parser, byte[] data, Encoding encoding = null)
            {
                return parser.ParseSmart((encoding ?? Encoding.Default).GetString(data));
            }

            public static byte[] ToBytes(this SmartJson json, JsonParser parser, Encoding encoding = null)
            {
                return (encoding ?? Encoding.Default).GetBytes(json.ToString(parser));
            }

            public static byte[] ToBytes(this JsonElement json, JsonParser parser, Encoding encoding = null)
            {
                return (encoding ?? Encoding.Default).GetBytes(json.ToString(parser));
            }
        }
    }

    public class SmartJson
    {
        #region Mapping

        public enum JsonType
        {
            Value,
            Array,
            Object,
            Unknown
        }

        public JsonType OwnType { get; private set; }

        public JsonElement Element { get; private set; }
        
        #endregion

        #region JsonElement

        public string Json
        {
            get { return Element.Json; }
        }

        public int ChildCount
        {
            get { return Element.ChildCount; }
        }

        public bool TidySingleLine
        {
            get { return Element.TidySingleLine; }
        }

        public override string ToString()
        {
            return Element.ToString();
        }
        public string ToString(JsonParser parser)
        {
            return Element.ToString(parser);
        }
        public void ToString(JsonParser parser, StringBuilder sb, int depth)
        {
            Element.ToString(parser, sb, depth);
        }

        #endregion

        #region Creation

        public SmartJson(JsonElement element)
        {
            if (element == null) element = JsonValue.Create<object>(null);
            Element = element;
            if (element is JsonValue)
                OwnType = JsonType.Value;
            else if (element is JsonArray)
                OwnType = JsonType.Array;
            else if (element is JsonObject)
                OwnType = JsonType.Object;
            else OwnType = JsonType.Unknown;
        }

        #region Conversation

        public static implicit operator SmartJson(JsonElement json)
        {
            return new SmartJson(json);
        }

        public static implicit operator JsonElement(SmartJson json)
        {
            return json.Element;
        }

        public static implicit operator JsonValue(SmartJson json)
        {
            if (json.OwnType != JsonType.Value)
                throw new InvalidCastException("element is not a json value");
            return json.Element.Value;
        }

        public static implicit operator JsonArray(SmartJson json)
        {
            if (json.OwnType != JsonType.Array)
                throw new InvalidCastException("element is not a json array");
            return json.Element.Array;
        }

        public static implicit operator JsonObject(SmartJson json)
        {
            if (json.OwnType != JsonType.Object)
                throw new InvalidCastException("element is not a json object");
            return json.Element.Object;
        }

        #endregion

        #region static

        public static SmartJson CreateValue<T>(T value)
        {
            return new SmartJson(JsonValue.Create(value));
        }

        public static SmartJson CreateArray()
        {
            return new SmartJson(new JsonArray());
        }

        public static SmartJson CreateObject()
        {
            return new SmartJson(new JsonObject());
        }

        #endregion

        #endregion

        #region Access

        #region Get

        public SmartJson Get()
        {
            switch (OwnType)
            {
                case JsonType.Value:
                    return Element.Value;
                case JsonType.Array:
                    if (Element.ChildCount == 0) return null;
                    else return Element.Array.Get<JsonElement>(0);
                case JsonType.Object:
                    if (Element.ChildCount == 0) return null;
                    else return Element.Object.Elements.ElementAt(0).Value;
                default: return null;
            }
        }

        public SmartJson Get(int index)
        {
            switch (OwnType)
            {
                case JsonType.Value:
                    if (index != 0) return null;
                    else return Element.Value;
                case JsonType.Array:
                    if (index < 0 || index >= ChildCount) return null;
                    else return Element.Array.Get<JsonElement>(index);
                case JsonType.Object:
                    if (index < 0 || index >= ChildCount) return null;
                    else return Element.Object.Elements.ElementAt(index).Value;
                default: return null;
            }
        }

        public SmartJson Get(string key)
        {
            switch (OwnType)
            {
                case JsonType.Object:
                    var je = Element.Object.Get<JsonElement>(key);
                    if (je == null) return null;
                    else return new SmartJson(je);
                default: return null;
            }
        }

        public T GetValue<T>()
        {
            var json = Get();
            if (json == null || json.OwnType != JsonType.Value) return default(T);
            return json.Element.Value.Get<T>();
        }

        public T GetValue<T>(int index)
        {
            var json = Get(index);
            if (json == null || json.OwnType != JsonType.Value) return default(T);
            return json.Element.Value.Get<T>();
        }

        public T GetValue<T>(string key)
        {
            var json = Get(key);
            if (json == null || json.OwnType != JsonType.Value) return default(T);
            return json.Element.Value.Get<T>();
        }

        #endregion

        #region Set

        public bool Set(int index, SmartJson json)
        {
            if (index < 0 || index >= ChildCount) return false;
            switch (OwnType)
            {
                case JsonType.Array:
                    Element.Array[index] = json;
                    return true;
                case JsonType.Object:
                    Element.Object[Element.Object.Elements.ElementAt(index).Key] = json;
                    return true;
                default: return false;
            }
        }

        public bool Set(string key, SmartJson json)
        {
            switch (OwnType)
            {
                case JsonType.Object:
                    if (key == null) return false;
                    Element.Object[key] = json;
                    return true;
                default: return false;
            }
        }

        public bool SetDirect<T>(T value)
        {
            switch (OwnType)
            {
                case JsonType.Value:
                    Element.Value.Set(value);
                    return true;
                case JsonType.Array:
                    Element.Array[0] = JsonValue.Create(value);
                    return true;
                case JsonType.Object:
                    if (ChildCount == 0) return false;
                    Element.Object[Element.Object.Elements.ElementAt(0).Key] = JsonValue.Create(value);
                    return true;
                default: return false;
            }
        }

        public bool SetDirect<T>(int index, T value)
        {
            switch (OwnType)
            {
                case JsonType.Value:
                    if (index != 0) return false;
                    else Element.Value.Set(value);
                    return true;
                case JsonType.Array:
                    if (index < 0 || index > ChildCount) return false;
                    else Element.Array[index] = JsonValue.Create(value);
                    return true;
                case JsonType.Object:
                    if (index < 0 || index >= ChildCount) return false;
                    else Element.Object[Element.Object.Elements.ElementAt(0).Key] =
                            JsonValue.Create(value);
                    return true;
                default: return false;
            }
        }

        public bool SetDirect<T>(string key, T value)
        {
            switch (OwnType)
            {
                case JsonType.Object:
                    Element.Object[key] = JsonValue.Create(value);
                    return true;
                default: return false;
            }
        }

        #endregion

        #region Add

        public bool Add(SmartJson json)
        {
            switch (OwnType)
            {
                case JsonType.Array:
                    Element.Array.Add<JsonElement>(json);
                    return true;
                default: return false;
            }
        }

        public bool AddDirect<T>(T value)
        {
            switch (OwnType)
            {
                case JsonType.Array:
                    Element.Array.Add<JsonElement>(JsonValue.Create(value));
                    return true;
                default: return false;
            }
        }

        #endregion

        #region this[]

        public SmartJson this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        public SmartJson this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        #endregion

        #endregion

        #region Info

        public bool IsNull
        {
            get { return OwnType == JsonType.Value && Element.Value.IsNull(); }
        }

        public bool IsArray
        {
            get { return OwnType == JsonType.Array; }
        }

        public bool IsObject
        {
            get { return OwnType == JsonType.Object; }
        }

        public bool IsValue
        {
            get { return OwnType == JsonType.Value; }
        }

        public bool IsEmpty
        {
            get { return OwnType == JsonType.Value ? IsNull : ChildCount == 0; }
        }

        public string ArgumentString
        {
            get { return OwnType == JsonType.Value ? Element.Value.ArgumentString : null; }
        }

        #endregion
    }
}
