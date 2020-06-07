using System;
using System.Collections;
using System.Collections.Generic;

namespace MaxLib.Collections
{
    /// <summary>
    /// A dictionary that holds the values of all possible <typeparamref name="TKey"/>
    /// keys. The dictionary will be filled lazily (the values are added when needed).
    /// </summary>
    /// <typeparam name="TKey">the key type</typeparam>
    /// <typeparam name="TValue">the value type</typeparam>
    public class FullDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        readonly Dictionary<TKey, TValue> dict;

        readonly Func<TKey, TValue> CreateValue;

        /// <summary>
        /// Create a new dictionary that holds all values for all possible <typeparamref name="TKey"/>.
        /// If a value is currently missing the default of <typeparamref name="TValue"/> will be assigned.
        /// </summary>
        public FullDictionary()
            : this(null)
        { }

        /// <summary>
        /// Create a new dictionary that holds all values for all possible <typeparamref name="TKey"/>.
        /// If a value is currently missing it will create a new with <paramref name="creator"/>.
        /// </summary>
        /// <param name="creator">the function that creates a new value depending on the type</param>
        public FullDictionary(Func<TKey, TValue> creator)
        {
            CreateValue = creator;
        }

        #region IDictionary

        public TValue this[TKey key]
        {
            get
            {
                if (dict.TryGetValue(key, out TValue value))
                    return value;
                if (CreateValue != null)
                    return dict[key] = CreateValue(key);
                return dict[key] = default;
            }
            set => dict[key] = value;
        }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)dict).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)dict).Values;

        /// <summary>
        /// the current number of stored values. that can be less than all possible key values.
        /// </summary>
        public int Count => dict.Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>)dict).IsReadOnly;

        public void Add(TKey key, TValue value)
            => dict.Add(key, value);

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => ((IDictionary<TKey, TValue>)dict).Add(item);

        /// <summary>
        /// This is not supported
        /// </summary>
        /// <exception cref="NotSupportedException" />
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
            => throw new NotSupportedException();

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
         => ((IDictionary<TKey, TValue>)dict).Contains(item);

        public bool ContainsKey(TKey key)
            => true;

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            => ((IDictionary<TKey, TValue>)dict).CopyTo(array, arrayIndex);

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
            => dict.GetEnumerator();

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => ((IDictionary<TKey, TValue>)dict).GetEnumerator();

        /// <summary>
        /// This is not supported
        /// </summary>
        /// <exception cref="NotSupportedException" />
        bool IDictionary<TKey, TValue>.Remove(TKey key)
            => throw new NotSupportedException();

        /// <summary>
        /// This is not supported
        /// </summary>
        /// <exception cref="NotSupportedException" />
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
            => throw new NotSupportedException();

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (dict.TryGetValue(key, out value))
                return true;
            if (CreateValue != null)
                value = CreateValue(key);
            else value = default;
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => ((IDictionary<TKey, TValue>)dict).GetEnumerator();

        #endregion IDictionary

        /// <summary>
        /// Propagates all possible values of <typeparamref name="TKey"/> and add them to 
        /// the collection. This is only supported if <typeparamref name="TKey"/> is an
        /// enum type.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public void FullEnumKeys()
        {
            var type = typeof(TKey);
            if (!type.IsEnum)
                throw new NotSupportedException();
            foreach (TKey val in Enum.GetValues(type))
                if (!dict.ContainsKey(val))
                {
                    if (CreateValue != null)
                        dict.Add(val, CreateValue(val));
                    else dict.Add(val, default);
                }
        }
    }
}
