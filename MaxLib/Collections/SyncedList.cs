using System;
using System.Collections;
using System.Collections.Generic;

namespace MaxLib.Collections
{
    public class SyncedList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
    {
        #region local values

        private readonly List<T> list;
        private readonly object lockList;
        private readonly bool ReadOnly;

        #endregion

        #region constructors

        private SyncedList(List<T> list, object lockList, bool ReadOnly)
        {
            this.list = list;
            this.lockList = lockList;
            this.ReadOnly = ReadOnly;
        }

        public SyncedList()
        {
            list = new List<T>();
            lockList = new object();
            ReadOnly = false;
        }

        public SyncedList(int capacity)
        {
            list = new List<T>(capacity);
            lockList = new object();
            ReadOnly = false;
        }

        public SyncedList(IEnumerable<T> collection)
        {
            list = new List<T>(collection);
            lockList = new object();
            ReadOnly = false;
        }

        #endregion

        #region custiom methods

        public SyncedList<T> ToSyncedList(bool readOnly)
        {
            if (!readOnly && ReadOnly)
                throw new InvalidOperationException("a readonly list cannot create a writeable list");
            return new SyncedList<T>(list, lockList, readOnly);
        }

        public void Execute(Action action)
        {
            lock (lockList) action();
        }

        #endregion

        #region inherited methods

        public T this[int index]
        {
            get
            {
                lock (lockList) return ((IList<T>)list)[index];
            }

            set
            {
                if (ReadOnly) throw new InvalidOperationException("collection is readonly");
                lock (lockList) ((IList<T>)list)[index] = value;
            }
        }

        object IList.this[int index]
        {
            get
            {
                lock (lockList) return ((IList)list)[index];
            }

            set
            {
                if (ReadOnly) throw new InvalidOperationException("collection is readonly");
                lock (lockList) ((IList)list)[index] = value;
            }
        }

        public int Count
        {
            get
            {
                lock (lockList) return ((IList<T>)list).Count;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                lock (lockList) return ((IList)list).IsFixedSize;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                lock (lockList) return ((IList<T>)list).IsReadOnly || ReadOnly;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                lock (lockList) return ((IList)list).IsSynchronized;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                lock (lockList) return ((IList)list).SyncRoot;
            }
        }

        int IList.Add(object value)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) return ((IList)list).Add(value);
        }

        public void Add(T item)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) ((IList<T>)list).Add(item);
        }

        public void Clear()
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) ((IList<T>)list).Clear();
        }

        bool IList.Contains(object value)
        {
            lock (lockList) return ((IList)list).Contains(value);
        }

        public bool Contains(T item)
        {
            lock (lockList) return ((IList<T>)list).Contains(item);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            lock (lockList) ((IList)list).CopyTo(array, index);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (lockList) ((IList<T>)list).CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (lockList) return ((IList<T>)list).GetEnumerator();
        }

        int IList.IndexOf(object value)
        {
            lock (lockList) return ((IList)list).IndexOf(value);
        }

        public int IndexOf(T item)
        {
            return ((IList<T>)list).IndexOf(item);
        }

        void IList.Insert(int index, object value)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) ((IList)list).Insert(index, value);
        }

        public void Insert(int index, T item)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) ((IList<T>)list).Insert(index, item);
        }

        void IList.Remove(object value)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) ((IList)list).Remove(value);
        }

        public bool Remove(T item)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) return ((IList<T>)list).Remove(item);
        }

        public void RemoveAt(int index)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) ((IList<T>)list).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (lockList) return ((IList<T>)list).GetEnumerator();
        }

        #endregion

        #region implicit operators

        public static implicit operator SyncedList<T>(List<T> list)
        {
            return new SyncedList<T>(list);
        }

        #endregion

        #region methods like List methods

        public void AddRange(IEnumerable<T> collection)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) list.AddRange(collection);
        }

        public int BinarySearch(T item)
        {
            lock (lockList) return list.BinarySearch(item);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            lock (lockList) return list.BinarySearch(item, comparer);
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            lock (lockList) return list.BinarySearch(index, count, item, comparer);
        }

        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            lock (lockList) return list.ConvertAll(converter);
        }

        public void CopyTo(T[] array)
        {
            lock (lockList) list.CopyTo(array);
        }
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            lock (lockList) list.CopyTo(index, array, arrayIndex, count);
        }

        public bool Exists(Predicate<T> match)
        {
            lock (lockList) return list.Exists(match);
        }

        public T Find(Predicate<T> match)
        {
            lock (lockList) return list.Find(match);
        }

        public List<T> FindAll(Predicate<T> match)
        {
            lock (lockList) return list.FindAll(match);
        }

        public int FindIndex(Predicate<T> match)
        {
            lock (lockList) return list.FindIndex(match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            lock (lockList) return list.FindIndex(startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            lock (lockList) return list.FindIndex(startIndex, count, match);
        }

        public T FindLast(Predicate<T> match)
        {
            lock (lockList) return list.FindLast(match);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            lock (lockList) return list.FindLastIndex(match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            lock (lockList) return list.FindLastIndex(startIndex, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            lock (lockList) return list.FindLastIndex(startIndex, count, match);
        }

        public void ForEach(Action<T> action)
        {
            lock (lockList) list.ForEach(action);
        }

        public List<T> GetRange(int index, int count)
        {
            lock (lockList) return list.GetRange(index, count);
        }

        public int IndexOf(T item, int index)
        {
            lock (lockList) return list.IndexOf(item, index);
        }

        public int IndexOf(T item, int index, int count)
        {
            lock (lockList) return list.IndexOf(item, index, count);
        }
       
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) list.InsertRange(index, collection);
        }

        public int LastIndexOf(T item)
        {
            lock (lockList) return list.LastIndexOf(item);
        }

        public int LastIndexOf(T item, int index)
        {
            lock (lockList) return list.LastIndexOf(item, index);
        }

        public int LastIndexOf(T item, int index, int count)
        {
            lock (lockList) return list.LastIndexOf(item, index, count);
        }

        public int RemoveAll(Predicate<T> match)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) return list.RemoveAll(match);
        }

        public void RemoveRange(int index, int count)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) list.RemoveRange(index, count);
        }

        public void Reverse()
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) list.Reverse();
        }

        public void Reverse(int index, int count)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) list.Reverse(index, count);
        }

        public void Sort()
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) list.Sort();
        }

        public void Sort(Comparison<T> comparison)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) list.Sort(comparison);
        }

        public void Sort(IComparer<T> comparer)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) list.Sort(comparer);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) list.Sort(index, count, comparer);
        }

        public T[] ToArray()
        {
            lock (lockList) return list.ToArray();
        }

        public void TrimExcess()
        {
            if (ReadOnly) throw new InvalidOperationException("collection is readonly");
            lock (lockList) list.TrimExcess();
        }

        public bool TrueForAll(Predicate<T> match)
        {
            lock (lockList) return list.TrueForAll(match);
        }

        #endregion
    }
}
