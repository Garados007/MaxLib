using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MaxLib.Collections
{
    public class EnumeratorBuilder<T> : IEnumerable<T>, IEnumerator<T>
    {
        class StackItem
        {
            public T Single;
            public IEnumerator<T> Multi;
            public Func<T> SingleAsync;
            public Func<IEnumerator<T>> MultiAsync;
            public bool UseSingle, UseAsync;
        }

        Queue<StackItem> items = new Queue<StackItem>();
        int state = -2;
        int threadId;
        StackItem current;

        public EnumeratorBuilder()
        {
            threadId = Thread.CurrentThread.ManagedThreadId;
        }

        public T Current
        {
            get
            {
                if (current == null) throw new NotSupportedException();
                if (current.UseSingle)
                    return current.Single;
                else return current.Multi.Current;
            }
        }

        object IEnumerator.Current => Current;
        
        public IEnumerator<T> GetEnumerator()
        {
            EnumeratorBuilder<T> temp;
            if ((Thread.CurrentThread.ManagedThreadId == threadId) && (state == -2))
            {
                state = 0;
                temp = this;
            }
            else
            {
                temp = new EnumeratorBuilder<T>()
                {
                    state = 0,
                    items = new Queue<StackItem>(items)
                };
            }
            return temp;
        }

        public bool MoveNext()
        {
            if (current != null && !current.UseSingle && current.Multi.MoveNext())
            {
                return true;
            }
            while (items.Count > 0)
            {
                current = items.Dequeue();
                if (current.UseAsync)
                {
                    if (current.UseSingle)
                        current.Single = current.SingleAsync();
                    else current.Multi = current.MultiAsync();
                }
                if (current.UseSingle || current.Multi.MoveNext())
                    return true;
            }
            current = null;
            return false;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            foreach (var item in items)
                if (!item.UseSingle)
                    item.Multi?.Dispose();
            items.Clear();
            current = null;
        }

        public void Yield(T item)
        {
            if (state != -2) throw new NotSupportedException();
            items.Enqueue(new StackItem
            {
                UseSingle = true,
                UseAsync = false,
                Single = item
            });
        }

        public void Yield(IEnumerator<T> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (state != -2) throw new NotSupportedException();
            this.items.Enqueue(new StackItem
            {
                UseSingle = false,
                UseAsync = false,
                Multi = items
            });
        }

        public void Yield(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            Yield(items.GetEnumerator());
        }

        public void Yield(Func<T> itemAsync)
        {
            if (itemAsync == null) throw new ArgumentNullException("itemAsync");
            if (state != -2) throw new NotSupportedException();
            items.Enqueue(new StackItem
            {
                UseSingle = true,
                UseAsync = true,
                SingleAsync = itemAsync
            });
        }

        public void Yield(Func<IEnumerator<T>> itemsAsync)
        {
            if (itemsAsync == null) throw new ArgumentNullException("itemsAsync");
            if (state != -2) throw new NotSupportedException();
            this.items.Enqueue(new StackItem
            {
                UseSingle = false,
                UseAsync = true,
                MultiAsync = itemsAsync
            });
        }

        public void Yield(Func<IEnumerable<T>> itemsAsync)
        {
            if (itemsAsync == null) throw new ArgumentNullException("itemsAsync");
            Yield(() => itemsAsync().GetEnumerator());
        }
    }
}
