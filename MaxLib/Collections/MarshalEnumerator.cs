using System;
using System.Collections;
using System.Collections.Generic;

namespace MaxLib.Collections
{
    public class MarshalEnumerator<T> : MarshalByRefObject, IEnumerator<T>
    {
        readonly IEnumerator<T> source;

        public MarshalEnumerator(IEnumerator<T> source)
        {
            this.source = source ?? throw new ArgumentNullException("source");
        }

        public T Current => source.Current;

        object IEnumerator.Current => source.Current;

        public void Dispose()
        {
            source.Dispose();
        }

        public bool MoveNext()
        {
            return source.MoveNext();
        }

        public void Reset()
        {
            source.Reset();
        }
    }

    public class MarshalEnumerable<T> : MarshalByRefObject, IEnumerable<T>
    {
        readonly IEnumerable<T> source;

        public MarshalEnumerable(IEnumerable<T> source)
        {
            this.source = source ?? throw new ArgumentNullException("source");
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new MarshalEnumerator<T>(source.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new MarshalEnumerator<T>(source.GetEnumerator());
        }
    }
}
