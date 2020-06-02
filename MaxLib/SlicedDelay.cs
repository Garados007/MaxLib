using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib
{
    [Obsolete]
    public class SlicedDelay : Disposable
    {
        private int sliceTime = 100;
        public int SliceTime
        {
            get => sliceTime;
            set
            {
                if (sliceTime <= 0) throw new ArgumentOutOfRangeException("SlicedTime");
                sliceTime = value;
            }
        }

        public SlicedDelay() { }
        public SlicedDelay(int sliceTime)
        {
            if (sliceTime <= 0) throw new ArgumentOutOfRangeException("sliceTime");
            this.sliceTime = sliceTime;
        }

        public event Action<object> AsyncWaitFinished;

        public void Wait(int duration)
        {
            var start = Environment.TickCount;
            int rest;
            while ((rest = start + duration - Environment.TickCount) > 0)
            {
                if (IsDisposed) return;
                else Thread.Sleep(Math.Min(rest, sliceTime));
            }
        }

        public void WaitAsync(int duration, object data)
        {
            Task.Run(async () =>
            {
                var start = Environment.TickCount;
                var rest = 0;
                while ((rest = start + duration - Environment.TickCount) > 0)
                {
                    if (IsDisposed) break;
                    else await Task.Delay(Math.Min(rest, sliceTime));
                }
                AsyncWaitFinished?.Invoke(data);
            });
        }
    }
}
