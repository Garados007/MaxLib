using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Collections
{
    public class KeyedCache<Key, Value> : IEnumerable<KeyValuePair<Key, Value>>
    {
        bool[] used;
        Key[] keys;
        Value[] values;
        uint[] usage;
        uint nextId;

        public Func<Key, Value> CreateValue { get; private set; }

        public Action<Key, Value> DisposeValue { get; private set; }

        public int BufferSize { get; private set; }

        public KeyedCache(int bufferSize, Func<Key, Value> createValue, Action<Key, Value> disposeValue)
        {
            if (bufferSize < 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (createValue == null) throw new ArgumentNullException(nameof(createValue));
            if (disposeValue == null) throw new ArgumentNullException(nameof(disposeValue));

            used = new bool[bufferSize];
            keys = new Key[bufferSize];
            values = new Value[bufferSize];
            usage = new uint[bufferSize];

            CreateValue = createValue;
            DisposeValue = disposeValue;
            BufferSize = bufferSize;
        }

        public Value Get(Key key)
        {
            var next = nextId++;
            if (nextId == 0)
            {
                RealignUsage();
                next = nextId++;
            }
            int firstFreeSpace = -1;
            int lowestUsageId = -1;
            uint lowestId = 0;
            for (int i = 0; i < BufferSize; ++i)
            {
                if (used[i] && Equals(key, keys[i]))
                {
                    usage[i] = next;
                    return values[i];
                }
                if (!used[i])
                    firstFreeSpace = i;
                else if (lowestUsageId == -1 || lowestId > usage[i])
                {
                    lowestUsageId = i;
                    lowestId = usage[i];
                }
            }
            //search for free space
            if (firstFreeSpace >= 0)
            {
                used[firstFreeSpace] = true;
                usage[firstFreeSpace] = next;
                keys[firstFreeSpace] = key;
                return values[firstFreeSpace] = CreateValue(key);
            }
            //search for oldest space
            DisposeValue(keys[lowestUsageId], values[lowestUsageId]);
            usage[lowestUsageId] = next;
            keys[lowestUsageId] = key;
            return values[lowestUsageId] = CreateValue(key);
        }

        private void RealignUsage()
        {
            var nusage = new uint[BufferSize];
            var given = new bool[BufferSize];
            uint nextId = 0;

            while (true)
            {
                bool u = false;
                int ind = 0;
                uint id = 0;

                for (int i = 0; i<BufferSize; ++i)
                    if (!given[i] && used[i])
                    {
                        if (!u)
                        {
                            ind = i;
                            id = usage[i];
                            u = true;
                        }
                        else if (id > usage[i])
                        {
                            ind = i;
                            id = usage[i];
                        }
                    }

                if (!u) break;

                nusage[ind] = nextId++;
                given[ind] = true;
            }

            usage = nusage;
            this.nextId = nextId;
        }

        public Value this[Key key] => Get(key);

        public IEnumerator<KeyValuePair<Key, Value>> GetEnumerator()
        {
            for (int i = 0; i < BufferSize; ++i)
                if (used[i])
                    yield return new KeyValuePair<Key, Value>(keys[i], values[i]);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
