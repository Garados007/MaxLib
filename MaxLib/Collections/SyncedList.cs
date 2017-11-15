using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Collections
{
    public class SyncedList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
    {
        private List<T> list;
        private object lockList;
        private bool ReadOnly;

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

        public SyncedList<T> ToSyncedList(bool readOnly)
        {
            if (!readOnly && this.ReadOnly)
                throw new InvalidOperationException("a readonly list cannot create a writeable list");
            return new SyncedList<T>(list, lockList, readOnly);
        }

        public void Execute(Action action)
        {
            lock (lockList) action();
        }

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

        public static implicit operator SyncedList<T>(List<T> list)
        {
            return new SyncedList<T>(list);
        }
    }
}
