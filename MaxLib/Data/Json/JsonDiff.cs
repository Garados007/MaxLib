using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Data.Json.Diff
{
    [Obsolete]
    public abstract class JsonDiff
    {
        public abstract JsonElement Modify(JsonElement source);

        public abstract JsonElement Serialize();


    }

    [Obsolete]
    public class DiffModified : JsonDiff
    {
        public JsonElement NewValue { get; private set; }

        public DiffModified(JsonElement newValue)
        {
            NewValue = newValue ?? throw new ArgumentNullException(nameof(newValue));
        }

        public override JsonElement Modify(JsonElement source)
        {
            return NewValue;
        }

        public override JsonElement Serialize()
        {
            return new JsonObject
            {
                { "t", JsonValue.Create("m") },
                { "n", NewValue }
            };
        }

        public override string ToString()
        {
            return $"=> " + NewValue.ToString(JsonParser.SingleLine);
        }
    }

    [Obsolete]
    public class DiffArrayAdded : JsonDiff
    {
        public JsonElement NewValue { get; private set; }

        public int Index { get; private set; }

        public DiffArrayAdded(int index, JsonElement newValue)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            Index = index;
            NewValue = newValue ?? throw new ArgumentNullException(nameof(newValue));
        }

        public override JsonElement Modify(JsonElement source)
        {
            if (source is JsonArray array)
            {
                while (source.ChildCount < Index)
                {
                    array.Add(JsonValue.Null);
                }
                array[Index] = NewValue;
                return source;
            }
            else return source;
        }

        public override JsonElement Serialize()
        {
            return new JsonObject
            {
                { "t", JsonValue.Create("na") },
                { "i", JsonValue.Create(Index) },
                { "n", NewValue },
            };
        }

        public override string ToString()
        {
            return $"+{Index}: " + NewValue.ToString(JsonParser.SingleLine);
        }
    }

    [Obsolete]
    public class DiffObjectAdded : JsonDiff
    {
        public JsonElement NewValue { get; private set; }

        public string Key { get; private set; }

        public DiffObjectAdded(string key, JsonElement newValue)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            NewValue = newValue ?? throw new ArgumentNullException(nameof(newValue));
        }

        public override JsonElement Modify(JsonElement source)
        {
            if (source is JsonObject obj)
            {
                obj.Add(Key, NewValue);
                return source;
            }
            else return source;
        }

        public override JsonElement Serialize()
        {
            return new JsonObject
            {
                { "t", JsonValue.Create("no") },
                { "k", JsonValue.Create(Key) },
                { "n", NewValue },
            };
        }

        public override string ToString()
        {
            return $"+{Key}: " + NewValue.ToString(JsonParser.SingleLine);
        }
    }

    [Obsolete]
    public class DiffArrayShrink : JsonDiff
    {
        public int NewCount { get; private set; }

        public DiffArrayShrink(int newCount)
        {
            if (newCount < 0) throw new ArgumentOutOfRangeException(nameof(newCount));
            NewCount = newCount;
        }

        public override JsonElement Modify(JsonElement source)
        {
            if (source is JsonArray array)
            {
                while (array.ChildCount > NewCount)
                    array.RemoveLast();
                return source;
            }
            else return source;
        }

        public override JsonElement Serialize()
        {
            return new JsonObject
            {
                { "t", JsonValue.Create("s") },
                { "s", JsonValue.Create(NewCount) },
            };
        }

        public override string ToString()
        {
            return $"- until {NewCount}";
        }
    }

    [Obsolete]
    public class DiffObjectRemoved : JsonDiff
    {
        public string Key { get; private set; }

        public DiffObjectRemoved(string key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public override JsonElement Modify(JsonElement source)
        {
            if (source is JsonObject obj)
            {
                obj.Remove(Key);
                return source;
            }
            else return source;
        }

        public override JsonElement Serialize()
        {
            return new JsonObject
            {
                { "t", JsonValue.Create("r") },
                { "k", JsonValue.Create(Key) },
            };
        }

        public override string ToString()
        {
            return $"-{Key}";
        }
    }

    [Obsolete]
    public class DiffArrayPath : JsonDiff
    {
        public JsonDiff Diff { get; private set; }

        public int Index { get; private set; }

        public DiffArrayPath(int index, JsonDiff diff)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            Index = index;
            Diff = diff ?? throw new ArgumentNullException(nameof(diff));
        }

        public override JsonElement Modify(JsonElement source)
        {
            if (source is JsonArray array)
            {
                if (array.ChildCount > Index)
                {
                    array[Index] = Diff.Modify(array[Index]);
                }
                return source;
            }
            else return source;
        }

        public override JsonElement Serialize()
        {
            return new JsonObject
            {
                { "t", JsonValue.Create("p") },
                { "i", JsonValue.Create(Index) },
                { "d", Diff.Serialize() },
            };
        }

        public override string ToString()
        {
            return $"[{Index}]{Diff}";
        }
    }

    [Obsolete]
    public class DiffObjectPath : JsonDiff
    {
        public JsonDiff Diff { get; private set; }

        public string Key { get; private set; }

        public DiffObjectPath(string key, JsonDiff diff)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Diff = diff ?? throw new ArgumentNullException(nameof(diff));
        }

        public override JsonElement Modify(JsonElement source)
        {
            if (source is JsonObject obj)
            {
                if (obj.Contains(Key))
                {
                    obj[Key] = Diff.Modify(obj[Key]);
                }
                return source;
            }
            else return source;
        }

        public override JsonElement Serialize()
        {
            return new JsonObject
            {
                { "t", JsonValue.Create("k") },
                { "k", JsonValue.Create(Key) },
                { "d", Diff.Serialize() },
            };
        }

        public override string ToString()
        {
            return $".{Key}{Diff}";
        }
    }
    
    [Obsolete]
    public class DiffContainer : JsonDiff
    {
        public List<JsonDiff> Diff { get; private set; }

        public DiffContainer(params JsonDiff[] diff)
        {
            Diff = new List<JsonDiff>(diff ?? throw new ArgumentNullException(nameof(diff)));
            foreach (var d in diff)
                if (d == null)
                    throw new ArgumentNullException(nameof(diff));
        }

        public override JsonElement Modify(JsonElement source)
        {
            foreach (var diff in Diff)
                source = diff.Modify(source);
            return source;
        }

        public override JsonElement Serialize()
        {
            var array = new JsonArray();
            foreach (var item in Diff)
                array.Add(item.Serialize());
            return new JsonObject
            {
                { "t", JsonValue.Create("c") },
                { "d", array },
            };
        }

        public override string ToString()
        {
            return $"=> {Diff.Count} changes";
        }
    }

    [Obsolete]
    public class DiffDeserializer
    {
        public JsonDiff Deserialize(JsonElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            foreach (var func in GetFuncs())
            {
                var diff = func(element);
                if (diff != null) return diff;
            }
            return null;
        }

        protected virtual IEnumerable<Func<JsonElement, JsonDiff>> GetFuncs()
        {
            yield return GetDiffModified;
            yield return GetDiffArrayAdded;
            yield return GetDiffObjectAdded;
            yield return GetDiffArrayShrink;
            yield return GetDiffObjectRemoved;
            yield return GetDiffArrayPath;
            yield return GetDiffObjectPath;
            yield return GetDiffContainer;
        }

        protected virtual JsonDiff GetDiffModified(JsonElement element)
        {
            if (element?.Object == null)
                return null;
            var type = element.Object["t"]?.Value?.Get<string>();
            if (type != "m") return null;
            var n = element.Object["n"];
            if (n == null) return null;
            return new DiffModified(n);
        }

        protected virtual JsonDiff GetDiffArrayAdded(JsonElement element)
        {
            if (element?.Object == null)
                return null;
            var type = element.Object["t"]?.Value?.Get<string>();
            if (type != "na") return null;
            var ind = element.Object["i"]?.Value?.Get<int>();
            if (ind == null) return null;
            var n = element.Object["n"];
            if (n == null) return null;
            return new DiffArrayAdded(ind.Value, n);
        }

        protected virtual JsonDiff GetDiffObjectAdded(JsonElement element)
        {
            if (element?.Object == null)
                return null;
            var type = element.Object["t"]?.Value?.Get<string>();
            if (type != "no") return null;
            var k = element.Object["k"]?.Value?.Get<string>();
            if (k == null) return null;
            var n = element.Object["n"];
            if (n == null) return null;
            return new DiffObjectAdded(k, n);
        }

        protected virtual JsonDiff GetDiffArrayShrink(JsonElement element)
        {
            if (element?.Object == null)
                return null;
            var type = element.Object["t"]?.Value?.Get<string>();
            if (type != "s") return null;
            var s = element.Object["s"]?.Value?.Get<int>();
            if (s == null) return null;
            return new DiffArrayShrink(s.Value);
        }

        protected virtual JsonDiff GetDiffObjectRemoved(JsonElement element)
        {
            if (element?.Object == null)
                return null;
            var type = element.Object["t"]?.Value?.Get<string>();
            if (type != "r") return null;
            var k = element.Object["k"]?.Value?.Get<string>();
            if (k == null) return null;
            return new DiffObjectRemoved(k);
        }

        protected virtual JsonDiff GetDiffArrayPath(JsonElement element)
        {
            if (element?.Object == null)
                return null;
            var type = element.Object["t"]?.Value?.Get<string>();
            if (type != "p") return null;
            var i = element.Object["i"]?.Value?.Get<int>();
            if (i == null) return null;
            var d = element.Object["d"];
            if (d == null)
                return null;
            var rd = Deserialize(d);
            if (rd == null)
                return null;
            return new DiffArrayPath(i.Value, rd);
        }

        protected virtual JsonDiff GetDiffObjectPath(JsonElement element)
        {
            if (element?.Object == null)
                return null;
            var type = element.Object["t"]?.Value?.Get<string>();
            if (type != "k") return null;
            var k = element.Object["k"]?.Value?.Get<string>();
            if (k == null) return null;
            var d = element.Object["d"];
            if (d == null)
                return null;
            var rd = Deserialize(d);
            if (rd == null)
                return null;
            return new DiffObjectPath(k, rd);
        }

        protected virtual JsonDiff GetDiffContainer(JsonElement element)
        {
            if (element?.Object == null)
                return null;
            var type = element.Object["t"]?.Value?.Get<string>();
            if (type != "c") return null;
            var l = new List<JsonDiff>();
            var array = element.Object["d"]?.Array;
            if (array == null) return null;
            for (int i = 0; i<array.ChildCount; ++i)
            {
                var rd = Deserialize(array[i]);
                if (rd == null) return null;
                else l.Add(rd);
            }
            return new DiffContainer(l.ToArray());
        }
    }

    [Obsolete]
    public class DiffCreator
    {
        public virtual JsonDiff Diff(JsonElement oldElement, JsonElement newElement)
        {
            if (oldElement == null) throw new ArgumentNullException(nameof(oldElement));
            if (newElement == null) throw new ArgumentNullException(nameof(newElement));

            var list = DiffElement(oldElement, newElement).ToArray();
            if (list.Length == 0)
                return null;
            if (list.Length == 1)
                return list[0];
            return new DiffContainer(list);
        }

        protected virtual IEnumerable<JsonDiff> DiffElement(JsonElement oldElement, JsonElement newElement)
        {
            if (oldElement.Value != null && newElement.Value != null)
                return DiffValue(oldElement.Value, newElement.Value);

            if (oldElement.Object != null && newElement.Object != null)
                return DiffObject(oldElement.Object, newElement.Object);

            if (oldElement.Array != null && newElement.Array != null)
                return DiffArray(oldElement.Array, newElement.Array);

            return new JsonDiff[] { new DiffModified(newElement) };
        }

        protected virtual IEnumerable<JsonDiff> DiffValue(JsonValue oldElement, JsonValue newElement)
        {
            if (oldElement.ArgumentString != newElement.ArgumentString)
                yield return new DiffModified(newElement);
        }

        protected virtual IEnumerable<JsonDiff> DiffObject(JsonObject oldElement, JsonObject newElement)
        {
            var keys = oldElement.Elements.Keys
                .Union(newElement.Elements.Keys)
                .Distinct();
            foreach (var key in keys)
            {
                var ho = oldElement.Contains(key);
                var hn = newElement.Contains(key);
                if (ho && !hn)
                    yield return new DiffObjectRemoved(key);
                if (!ho && hn)
                    yield return new DiffObjectAdded(key, newElement[key]);
                if (ho && hn)
                {
                    var diff = Diff(oldElement[key], newElement[key]);
                    if (diff != null)
                        yield return new DiffObjectPath(key, diff);
                }
            }
        }

        protected virtual IEnumerable<JsonDiff> DiffArray(JsonArray oldElement, JsonArray newElement)
        {
            var min = Math.Min(oldElement.ChildCount, newElement.ChildCount);
            for (int i = 0; i<min; ++i)
            {
                var diff = Diff(oldElement[i], newElement[i]);
                if (diff != null)
                    yield return new DiffArrayPath(i, diff);
            }
            if (oldElement.ChildCount < newElement.ChildCount)
            {
                for (int i = oldElement.ChildCount; i < newElement.ChildCount; ++i)
                    yield return new DiffArrayAdded(i, newElement[i]);
            }
            if (oldElement.ChildCount > newElement.ChildCount)
                yield return new DiffArrayShrink(newElement.ChildCount);
        }
    }
}
