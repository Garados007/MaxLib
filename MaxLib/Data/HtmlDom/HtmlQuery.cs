using System;
using System.Collections.Generic;

namespace MaxLib.Data.HtmlDom
{
    public class HtmlQuery
    {
    }

    public static class HtmlDomQueryExtension
    {
        public static IEnumerable<HtmlDomElement> DeepSearch(this IEnumerable<HtmlDomElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            foreach (var element in elements)
            {
                yield return element;
                foreach (var sub in DeepSearch(element))
                    yield return sub;
            }
        }

        public static IEnumerable<HtmlDomElement> TagName(this IEnumerable<HtmlDomElement> elements, string tagName)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));

            foreach (var element in elements)
                if (element.ElementName == tagName)
                    yield return element;
        }

        public static IEnumerable<HtmlDomElement> Class(this IEnumerable<HtmlDomElement> elements, params string[] classes)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (classes == null) throw new ArgumentNullException(nameof(classes));

            if (classes.Length == 0)
                yield break;

            foreach (var element in elements)
            {
                bool contains = true;
                foreach (var className in classes)
                    if (!element.Class.Contains(className))
                    {
                        contains = false;
                        break;
                    }
                if (contains)
                    yield return element;
            }
        }

        public static IEnumerable<HtmlDomElement> Attribute(this IEnumerable<HtmlDomElement> elements, string name, string value, HtmlQueryAttributePattern pattern = HtmlQueryAttributePattern.Complete)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            foreach (var element in elements)
            {
                var contains = false;
                foreach (var attr in element.GetAttribute(name))
                {
                    switch (pattern)
                    {
                        case HtmlQueryAttributePattern.Complete:
                            contains = value == attr.Value;
                            break;
                        case HtmlQueryAttributePattern.Contains:
                            contains = attr.Value.Contains(value);
                            break;
                        case HtmlQueryAttributePattern.Start:
                            contains = attr.Value.StartsWith(value);
                            break;
                        case HtmlQueryAttributePattern.End:
                            contains = attr.Value.EndsWith(value);
                            break;
                    }
                    if (contains) break;
                }
                if (contains)
                    yield return element;
            }
        }

        public static IEnumerable<HtmlDomElement> Id(this IEnumerable<HtmlDomElement> elements, string id)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (id == null) throw new ArgumentNullException(nameof(id));

            foreach (var element in elements)
                if (element.Id == id)
                    yield return element;
        }

        public static IEnumerable<HtmlDomElement> NextNeighbour(this IEnumerable<HtmlDomElement> elements, bool direct = true)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            foreach (var element in elements)
            {
                if (element.Parent == null) continue;
                var ind = element.Parent.Elements.IndexOf(element);

                if (direct)
                {
                    if (ind + 1 < element.Parent.Elements.Count)
                        yield return element.Parent.Elements[ind + 1];
                }
                else
                {
                    for (int i = ind + 1; i < element.Parent.Elements.Count; ++i)
                        yield return element.Parent.Elements[ind + 1];
                }
            }
        }

        public static IEnumerable<HtmlDomElement> ChildCount(this IEnumerable<HtmlDomElement> elements, int count, HtmlQueryNumberCompare compare = HtmlQueryNumberCompare.Equal)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            foreach (var element in elements)
            {
                var ec = element.Elements.Count;
                bool success = false;
                switch (compare)
                {
                    case HtmlQueryNumberCompare.Equal: success = ec == count; break;
                    case HtmlQueryNumberCompare.EqualOrHigher: success = ec >= count; break;
                    case HtmlQueryNumberCompare.EqualOrLower: success = ec <= count; break;
                    case HtmlQueryNumberCompare.Higher: success = ec > count; break;
                    case HtmlQueryNumberCompare.Lower: success = ec < count; break;
                    case HtmlQueryNumberCompare.NotEqual: success = ec != count; break;
                }
                if (success)
                    yield return element;
            }
        }

        public static IEnumerable<HtmlDomElement> SpecificChild(this IEnumerable<HtmlDomElement> elements, int index)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            foreach (var element in elements)
            {
                if (element.Elements.Count <= index) continue;
                yield return element.Elements[index];
            }
        }

        public static IEnumerable<HtmlDomElement> SpecificChild(this IEnumerable<HtmlDomElement> elements, params int[] indeces)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (indeces == null) throw new ArgumentNullException(nameof(indeces));

            for (int i = 0; i<indeces.Length; ++i)
            {
                elements = SpecificChild(elements, indeces[i]);
            }
            return elements;
        }

        public static IEnumerable<string> Text(this IEnumerable<HtmlDomElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            foreach (var element in elements)
                if (element is HtmlDomElementText text)
                    yield return text.Text;
        }

        public static IEnumerable<HtmlDomElement> Search(this HtmlDomElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            yield return element;
        }

        public static IEnumerable<U> TrySelect<T, U>(this IEnumerable<T> ts, Func<T, U> converter)
        {
            if (ts == null) throw new ArgumentNullException(nameof(ts));
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            foreach (var t in ts)
            {
                U result = default;
                bool valid = false;
                try
                {
                    result = converter(t);
                    valid = true;
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e);
                }
                if (valid)
                    yield return result;
            }
        }

        public static IEnumerable<T> Debug<T>(this IEnumerable<T> ts, ICollection<T> output)
        {
            if (ts == null) throw new ArgumentNullException(nameof(ts));
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (output.IsReadOnly) throw new ArgumentException("output is readonly", nameof(output));

            foreach (var t in ts)
            {
                output.Add(t);
                yield return t;
            }
        }
    }

    public enum HtmlQueryAttributePattern
    {
        Complete,
        Start,
        End,
        Contains
    }

    public enum HtmlQueryNumberCompare
    {
        Equal,
        NotEqual,
        Lower,
        Higher,
        EqualOrLower,
        EqualOrHigher
    }
}
