using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib
{
    public class SlicedDelay : Disposable
    {
        private int sliceTime = 100;
        public int SliceTime
        {
            get { return sliceTime; }
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
            var m = new ManualResetEvent(false);
            _wait(duration, () => m.Set());
            m.WaitOne();
            m.Dispose();
        }

        public void WaitAsync(int duration, object data)
        {
            _wait(duration, () =>
            {
                if (AsyncWaitFinished != null) AsyncWaitFinished(data);
            });
        }

        void _wait(int duration, Action callback)
        {
            new Task(() =>
            {
                var start = Environment.TickCount;
                var rest = 0;
                while ((rest = start+duration-Environment.TickCount)>0)
                {
                    if (IsDisposed) return;
                    else Thread.Sleep(Math.Min(rest, sliceTime));
                }
                callback();
            }).Start();
        }
    }
}
