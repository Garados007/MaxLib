using System;

namespace MaxLib.Maths
{
    [Obsolete]
    public abstract class MathBase<T>
    {
        protected abstract T One { get; }

        protected abstract T Zero { get; }

        protected abstract T Add(T value1, T value2);

        protected abstract T Negate(T value);

        protected abstract T Multiplicate(T value1, T value2);

        protected abstract T Divide(T value1, T value2);
    }
}
