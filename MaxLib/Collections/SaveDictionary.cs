using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;

namespace MaxLib.Collections
{
    public class SaveDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, ISerializable, IDeserializationCallback, IDisposable
    {
        readonly Dictionary<TKey, TValue> dict;

        bool writeAccess = false;
        int readCounter = 0;
        readonly object changeLock = new object();
        readonly Semaphore changeMutex = new Semaphore(1, 1);

        void EnterMode(bool write)
        {
            if (write)
            {
                changeMutex.WaitOne();
                lock (changeLock)
                {
                    writeAccess = true;
                    readCounter = 1;
                }
            }
            else
            {
                bool access;
                lock (changeLock) access = writeAccess || readCounter == 0;
                if (access)
                    changeMutex.WaitOne();
                lock (changeLock)
                {
                    writeAccess = false;
                    readCounter++;
                }
            }
        }

        void LeaveMode(bool write)
        {
            if (write)
            {
                lock (changeLock)
                {
                    readCounter = 0;
                    changeMutex.Release(1);
                }
            }
            else
            {
                int counter;
                lock (changeLock)
                {
                    counter = --readCounter;
                    if (counter == 0)
                        changeMutex.Release(1);
                }
            }
        }

        #region Constructor

        public SaveDictionary()
        {
            dict = new Dictionary<TKey, TValue>();
        }
        public SaveDictionary(int capacity)
        {
            dict = new Dictionary<TKey, TValue>(capacity);
        }
        public SaveDictionary(IEqualityComparer<TKey> comparer)
        {
            dict = new Dictionary<TKey, TValue>(comparer);
        }
        public SaveDictionary(IDictionary<TKey, TValue> dictionary)
        {
            dict = new Dictionary<TKey, TValue>(dictionary);
        }
        public SaveDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            dict = new Dictionary<TKey, TValue>(capacity, comparer);
        }
        public SaveDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            dict = new Dictionary<TKey, TValue>(dictionary, comparer);
        }

        #endregion

        #region Dictionary Methods

        public TValue this[TKey key]
        {
            get
            {
                EnterMode(false);
                var v = ((IDictionary<TKey, TValue>)dict)[key];
                LeaveMode(false);
                return v;
            }
            set
            {
                EnterMode(true);
                ((IDictionary<TKey, TValue>)dict)[key] = value;
                LeaveMode(true);
            }
        }
        object IDictionary.this[object key] { get => ((IDictionary)dict)[key]; set => ((IDictionary)dict)[key] = value; }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)dict).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)dict).Values;

        public int Count => ((IDictionary<TKey, TValue>)dict).Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>)dict).IsReadOnly;

        public bool IsFixedSize => ((IDictionary)dict).IsFixedSize;

        public object SyncRoot => ((IDictionary)dict).SyncRoot;

        public bool IsSynchronized => ((IDictionary)dict).IsSynchronized;

        ICollection IDictionary.Keys => ((IDictionary)dict).Keys;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => ((IReadOnlyDictionary<TKey, TValue>)dict).Keys;

        ICollection IDictionary.Values => ((IDictionary)dict).Values;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => ((IReadOnlyDictionary<TKey, TValue>)dict).Values;

        public void Add(TKey key, TValue value)
        {
            EnterMode(true);
            ((IDictionary<TKey, TValue>)dict).Add(key, value);
            LeaveMode(true);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            EnterMode(true);
            ((IDictionary<TKey, TValue>)dict).Add(item);
            LeaveMode(true);
        }

        void IDictionary.Add(object key, object value)
        {
            EnterMode(true);
            ((IDictionary)dict).Add(key, value);
            LeaveMode(true);
        }

        public void Clear()
        {
            EnterMode(true);
            ((IDictionary<TKey, TValue>)dict).Clear();
            LeaveMode(true);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            EnterMode(false);
            var c = ((IDictionary<TKey, TValue>)dict).Contains(item);
            LeaveMode(false);
            return c;
        }

        bool IDictionary.Contains(object key)
        {
            EnterMode(false);
            var c = ((IDictionary)dict).Contains(key);
            LeaveMode(false);
            return c;
        }

        public bool ContainsKey(TKey key)
        {
            EnterMode(false);
            var c = ((IDictionary<TKey, TValue>)dict).ContainsKey(key);
            LeaveMode(false);
            return c;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)dict).CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((IDictionary)dict).CopyTo(array, index);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            EnterMode(false);
            foreach (var e in dict)
                yield return e;
            LeaveMode(false);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ((ISerializable)dict).GetObjectData(info, context);
        }

        public void OnDeserialization(object sender)
        {
            ((IDeserializationCallback)dict).OnDeserialization(sender);
        }

        public bool Remove(TKey key)
        {
            EnterMode(true);
            var r = ((IDictionary<TKey, TValue>)dict).Remove(key);
            LeaveMode(true);
            return r;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            EnterMode(true);
            var r = ((IDictionary<TKey, TValue>)dict).Remove(item);
            LeaveMode(true);
            return r;
        }

        void IDictionary.Remove(object key)
        {
            EnterMode(true);
            ((IDictionary)dict).Remove(key);
            LeaveMode(true);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            EnterMode(false);
            var g = ((IDictionary<TKey, TValue>)dict).TryGetValue(key, out value);
            LeaveMode(false);
            return g;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new DictionaryEnumerator(GetEnumerator());
        }

        public void Dispose()
        {
            changeMutex.Dispose();
        }

        public class DictionaryEnumerator : IDictionaryEnumerator
        {
            readonly IEnumerator<KeyValuePair<TKey, TValue>> en;

            public object Key => en.Current.Key;

            public object Value => en.Current.Value;

            public DictionaryEntry Entry => new DictionaryEntry(en.Current.Key, en.Current.Value);

            public object Current => en.Current;

            public bool MoveNext()
            {
                return en.MoveNext();
            }

            public void Reset()
            {
                en.Reset();
            }

            public DictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
            {
                en = enumerator;
            }
        }

        #endregion
    }
}
