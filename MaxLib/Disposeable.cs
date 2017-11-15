using System;

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
