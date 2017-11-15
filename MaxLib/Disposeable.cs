using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib
{
    public abstract class Disposable : IDisposable
    {
        public bool IsDisposed { get; internal set; }

        public virtual void Dispose()
        {
            IsDisposed = true;
        }

        ~Disposable()
        {
            if (!IsDisposed) Dispose();
        }
    }
}
