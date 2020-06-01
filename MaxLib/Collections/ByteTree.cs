using System;
using System.Collections;
using System.Collections.Generic;

namespace MaxLib.Collections
{
    public class ByteTree<T> : ICollection<T>
    {
        T NodeValue = default;
        bool ContainsNodeValue = false;
        ByteTree<T>[] Nodes = new ByteTree<T>[256];
        int count = 0;

        public int Count => count;

        bool ICollection<T>.IsReadOnly => false;

        void ICollection<T>.Add(T item)
        {
            Set(item);
        }

        public void Clear()
        {
            NodeValue = default;
            ContainsNodeValue = false;
            Nodes = new ByteTree<T>[256];
        }

        bool ICollection<T>.Contains(T item)
        {
            foreach (var value in this)
                if (Equals(value, item))
                    return true;
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (arrayIndex < 0 || array.Length < arrayIndex + count)
                throw new ArgumentOutOfRangeException("arrayIndex");
            int index = arrayIndex;
            InternalCopyTo(array, ref index);
        }

        void InternalCopyTo(T[] array, ref int arrayIndex)
        {
            if (ContainsNodeValue)
                array[arrayIndex++] = NodeValue;
            for (int i = 0; i < 256; ++i)
                if (Nodes[i] != null)
                    Nodes[i].InternalCopyTo(array, ref arrayIndex);
        }

        public bool Contains()
        {
            return ContainsNodeValue;
        }

        public bool Contains(byte[] path)
        {
            return Contains(path, 0);
        }

        public bool Contains(byte[] path, int index)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (index < 0 || index > path.Length) throw new ArgumentOutOfRangeException("index");

            if (index == path.Length) return ContainsNodeValue;

            var node = Nodes[path[index]];
            if (node == null) return false;
            return node.Contains(path, index + 1);
        }

        public T Get()
        {
            return Get(new byte[0], 0);
        }

        public T Get(byte[] path)
        {
            return Get(path, 0);
        }

        public T Get(byte[] path, int offset)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (offset < 0 || offset > path.Length) throw new ArgumentOutOfRangeException("offset");

            if (offset == path.Length)
            {
                if (ContainsNodeValue) return NodeValue;
                else throw new KeyNotFoundException();
            }

            var node = Nodes[path[offset]];
            if (node == null) throw new KeyNotFoundException();
            else return node.Get(path, offset + 1);
        }

        public bool TryGet(out T value)
        {
            if (ContainsNodeValue)
            {
                value = NodeValue;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryGet(byte[] path, out T value)
        {
            return TryGet(path, 0, out value);
        }

        public bool TryGet(byte[] path, int offset, out T value)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (offset < 0 || offset > path.Length) throw new ArgumentOutOfRangeException("offset");

            if (offset == path.Length)
                return TryGet(out value);

            var node = Nodes[path[offset]];
            if (node == null)
            {
                value = default;
                return false;
            }
            else return node.TryGet(path, offset + 1, out value);
        }


        public IEnumerator<T> GetEnumerator()
        {
            var eb = new EnumeratorBuilder<T>();
            if (ContainsNodeValue) eb.Yield(NodeValue);
            for (int i = 0; i < 256; ++i)
                if (Nodes[i] != null)
                    eb.Yield(Nodes[i].GetEnumerator());
            return eb;
        }

        bool ICollection<T>.Remove(T item)
        {
            if (ContainsNodeValue && Equals(item, NodeValue))
            {
                NodeValue = default;
                ContainsNodeValue = false;
                count--;
                return true;
            }
            for (int i = 0; i < 256; ++i)
                if (Nodes[i] != null && ((ICollection<T>)Nodes[i]).Remove(item))
                {
                    count--;
                    return true;
                }
            return false;
        }

        public void Set(T value)
        {
            if (!ContainsNodeValue) count++;
            NodeValue = value;
            ContainsNodeValue = true;
        }

        public void Set(byte[] path, T value)
        {
            Set(path, value, 0);
        }

        public void Set(byte[] path, T value, int index)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (index < 0 || index > path.Length) throw new ArgumentOutOfRangeException("index");

            if (index == path.Length)
            {
                Set(value);
                return;
            }

            var node = Nodes[path[index]];
            if (node == null)
                node = Nodes[path[index]] = new ByteTree<T>();
            var c = node.count;
            node.Set(path, value, index + 1);
            if (node.count != c) count++;
        }

        public bool Remove()
        {
            if (ContainsNodeValue)
            {
                count--;
                ContainsNodeValue = false;
                NodeValue = default;
                return true;
            }
            else return false;
        }

        public bool Remove(byte[] path)
        {
            return Remove(path, 0);
        }

        public bool Remove(byte[] path, int index)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (index < 0 || index > path.Length) throw new ArgumentOutOfRangeException("index");

            if (path.Length == index)
                return Remove();

            var node = Nodes[path[index]];
            if (node == null) return false;
            if (node.Remove(path, index + 1))
            {
                count--;
                if (node.count == 0)
                    Nodes[path[index]] = null;
                return true;
            }
            else return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
