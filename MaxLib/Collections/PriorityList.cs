using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Collections
{
    public class PriorityList<Priority, Element> : IList<Element>
        where Priority : IComparable
    {
        private readonly SortedDictionary<Priority, List<Element>> dict;
        private readonly object syncObject = new object();

        public Element this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException("index");
                lock (syncObject)
                    foreach (var c in dict)
                        if (index < c.Value.Count)
                            return c.Value[index];
                        else index -= c.Value.Count;
                throw new ArgumentOutOfRangeException("index");
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException("index");
                lock (syncObject)
                    foreach (var c in dict)
                        if (index < c.Value.Count)
                        {
                            c.Value[index] = value;
                            return;
                        }
                        else index -= c.Value.Count;
            }
        }

        public int Count { get; private set; } = 0;

        public bool IsReadOnly => false;

        void ICollection<Element>.Add(Element item)
        {
            throw new NotSupportedException();
        }

        public void Add(Priority priority, Element item)
        {
            lock (syncObject)
            {
                if (!dict.ContainsKey(priority))
                    dict.Add(priority, new List<Element>());
                dict[priority].Add(item);
                Count++;
            }
        }

        public void Clear()
        {
            lock (syncObject)
            {
                dict.Clear();
                Count = 0;
            }
        }

        public bool Contains(Element item)
        {
            lock (syncObject)
            {
                foreach (var e in this)
                    if (Equals(e, item))
                        return true;
                return false;
            }
        }

        public void CopyTo(Element[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex + Count > array.Length) throw new ArgumentOutOfRangeException("arrayIndex");

            lock (syncObject)
            {
                foreach (var d in dict)
                {
                    for (int i = 0; i < d.Value.Count; ++i)
                        array[arrayIndex + i] = d.Value[i];
                    arrayIndex += d.Value.Count;
                }
            }
        }

        public IEnumerator<Element> GetEnumerator()
        {
            lock (syncObject)
                foreach (var d in dict)
                    foreach (var e in d.Value)
                        yield return e;
        }

        public int IndexOf(Element item)
        {
            int index = 0;
            lock (syncObject)
                foreach (var d in this)
                    if (Equals(item, d)) return index;
                    else index++;
            return -1;
        }
        
        void IList<Element>.Insert(int index, Element item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(Element item)
        { 
            lock (syncObject)
            {
                foreach (var d in dict)
                    if (d.Value.Remove(item))
                    {
                        Count--;
                        if (d.Value.Count == 0)
                            dict.Remove(d.Key);
                        return true;
                    }
                return false;
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException("index");
            lock (syncObject)
            {
                foreach (var d in dict)
                    if (index < d.Value.Count)
                    {
                        d.Value.RemoveAt(index);
                        if (d.Value.Count == 0)
                            dict.Remove(d.Key);
                        break;
                    }
                    else index -= d.Value.Count;
                Count--;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PriorityList()
        {
            dict = new SortedDictionary<Priority, List<Element>>();
        }

        public void ChangePriority(Priority priority, Element item)
        {
            Remove(item);
            Add(priority, item);
        }

        public Priority GetPriority(Element item)
        {
            lock (syncObject)
                foreach (var d in dict)
                    if (d.Value.Contains(item))
                        return d.Key;
            throw new KeyNotFoundException("item not in the collection found");
        }

        public Element[] ToArray()
        {
            lock (syncObject)
            {
                var a = new Element[Count];
                CopyTo(a, 0);
                return a;
            }
        }

        public List<Element> ToList()
        {
            return ToArray().ToList();
        }

        public IEnumerator<Element> GetEnumerator(Priority priority)
        {
            lock (syncObject)
            {
                if (dict.ContainsKey(priority))
                    return dict[priority].GetEnumerator();
                else return Empty();
            }
        }

        private IEnumerator<Element> Empty()
        {
            yield break;
        }

        public Element Find(Predicate<Element> match)
        {
            if (match == null) throw new ArgumentNullException("match");
            lock (syncObject)
                foreach (var e in this)
                    if (match(e))
                        return e;
            return default;
        }
    }
}
