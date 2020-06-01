using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace MaxLib.Collections
{
    public class EnumeratorBackup<T> : IEnumerable<T>, IEnumerator<T>
    {
        private readonly IEnumerator<T> master;
        private readonly IEnumerator<T> backup;
        private IEnumerator<T> current;
        int state = -2;
        readonly int threadId;

        public EnumeratorBackup(IEnumerator<T> master, IEnumerator<T> backup)
        {
            this.master = master ?? throw new ArgumentNullException("master");
            this.backup = backup ?? throw new ArgumentNullException("backup");
            threadId = Thread.CurrentThread.ManagedThreadId;
        }

        public EnumeratorBackup(IEnumerable<T> master, IEnumerable<T> backup)
        {
            this.master = (master ?? throw new ArgumentNullException("master")).GetEnumerator();
            this.backup = (backup ?? throw new ArgumentNullException("backup")).GetEnumerator();
            threadId = Thread.CurrentThread.ManagedThreadId;
        }

        public T Current
        {
            get
            {
                if (current == null) throw new NotSupportedException();
                return current.Current;
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            master.Dispose();
            backup.Dispose();
            current?.Dispose();
            current = null;
        }

        public IEnumerator<T> GetEnumerator()
        {
            EnumeratorBackup<T> temp;
            if ((Thread.CurrentThread.ManagedThreadId == threadId) && (state == -2))
            {
                state = 0;
                temp = this;
                current = master;
            }
            else
            {
                temp = new EnumeratorBackup<T>(master, backup)
                {
                    state = 0,
                    current = master,
                };
            }
            return temp;
        }

        public bool MoveNext()
        {
            if (current == null) return false;
            if (!current.MoveNext())
            {
                if (state == 0)
                {
                    state = 1;
                    current = backup;
                    return current.MoveNext();
                }
                else return false;
            }
            else return true;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
