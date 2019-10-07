using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxLib.Data.Json.Auto
{
    /// <summary>
    /// With this class are many automatic converter from native c# 
    /// objects to JSON accessible. Missing converters can easily be
    /// added. Every convertation is customizible.
    /// </summary>
    public class AutoJson
    {
        Dictionary<Type, Func<object, JsonElement>> FiniteParser;
        Dictionary<Type, Func<object, Dictionary<string, object>>> ObjectParser;
        Dictionary<Type, Func<object, IEnumerable<object>>> ListParser;
        List<Tuple<Func<object, bool>, Func<object,object>>> SpecialPurposeParser;

        public AutoJson()
        {
            FiniteParser = new Dictionary<Type, Func<object, JsonElement>>();
            ObjectParser = new Dictionary<Type, Func<object, Dictionary<string, object>>>();
            ListParser = new Dictionary<Type, Func<object, IEnumerable<object>>>();
            SpecialPurposeParser = new List<Tuple<Func<object, bool>, Func<object, object>>>();
            AddCommonParser();
        }

        private Func<object, U> Anonymize<T, U>(Func<T, U> func)
        {
            return (v) => func((T)v);
        }

        public void AddParser<T>(Func<T, JsonElement> directConverter)
        {
            if (directConverter == null) throw new ArgumentNullException("directConverter");
            var type = typeof(T);
            if (FiniteParser.ContainsKey(type))
                FiniteParser[type] = Anonymize(directConverter);
            else FiniteParser.Add(type, Anonymize(directConverter));
        }

        public void AddParser<T>(Func<T, Dictionary<string, object>> dictConverter)
        {
            if (dictConverter == null) throw new ArgumentNullException("dictConverter");
            var type = typeof(T);
            if (ObjectParser.ContainsKey(type))
                ObjectParser[type] = Anonymize(dictConverter);
            else ObjectParser.Add(type, Anonymize(dictConverter));
        }

        public void AddParser<T>(Func<T, IEnumerable<object>> arrayConverter)
        {
            if (arrayConverter == null) throw new ArgumentNullException("arrayConverter");
            var type = typeof(T);
            if (ListParser.ContainsKey(type))
                ListParser[type] = Anonymize(arrayConverter);
            else ListParser.Add(type, Anonymize(arrayConverter));
        }

        public void AddParser(Func<object, bool> acceptType, Func<object, object> converter)
        {
            if (acceptType == null) throw new ArgumentNullException("acceptType");
            if (converter == null) throw new ArgumentNullException("converter");
            SpecialPurposeParser.Add(new Tuple<Func<object, bool>, Func<object, object>>(
                acceptType, converter));
        }

        public JsonElement Convert(object data)
        {
            if (data == null)
                return JsonValue.Null;
            if (data is JsonElement json)
                return json;
            var type = data.GetType();

            if (FiniteParser.ContainsKey(type))
                return FiniteParser[type](data);

            if (ObjectParser.ContainsKey(type))
            {
                var obj = new JsonObject();
                foreach (var vp in ObjectParser[type](data))
                    obj.Add(vp.Key, Convert(vp.Value));
                return obj;
            }

            if (ListParser.ContainsKey(type))
            {
                var list = new JsonArray();
                foreach (var e in ListParser[type](data))
                    list.Add(Convert(e));
                return list;
            }

            foreach (var special in SpecialPurposeParser)
                if (special.Item1(data))
                {
                    return Convert(special.Item2(data));
                }

            throw new KeyNotFoundException("type of data wasn't registred");
        }

        private Func<T, JsonElement> TransformResult<T>(Func<T, JsonValue> converter)
        {
            return converter;
        }

        private void AddCommonParser()
        {
            AddParser(TransformResult<byte>(JsonValue.Create));
            AddParser(TransformResult<sbyte>(JsonValue.Create));
            AddParser(TransformResult<short>(JsonValue.Create));
            AddParser(TransformResult<ushort>(JsonValue.Create));
            AddParser(TransformResult<int>(JsonValue.Create));
            AddParser(TransformResult<uint>(JsonValue.Create));
            AddParser(TransformResult<long>(JsonValue.Create));
            AddParser(TransformResult<ulong>(JsonValue.Create));
            AddParser(TransformResult<float>(JsonValue.Create));
            AddParser(TransformResult<decimal>(JsonValue.Create));
            AddParser(TransformResult<double>(JsonValue.Create));
            AddParser(TransformResult<string>(JsonValue.Create));
            AddParser(TransformResult<bool>(JsonValue.Create));
            
            AddParser((o) => o is IDictionary,
                (o) =>
                {
                    var json = new JsonObject();
                    foreach (DictionaryEntry item in (o as IDictionary))
                        json.Add(item.Key.ToString(), Convert(item.Value));
                    return json;
                });
            AddParser((o) => o is IEnumerable,
                (o) =>
                {
                    var json = new JsonArray();
                    foreach (var item in (o as IEnumerable))
                        json.Add(Convert(item));
                    return json;
                });
            AddParser((o) => o.GetType().IsEnum,
                (o) => o.ToString());
        }
    }
}
