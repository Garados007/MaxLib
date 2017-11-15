using System;

namespace MaxLib
{
    public class FrameTimeCounter : IDisposable
    {
        double[] times;
        int[] frames;
        int index;
        int count;
        System.Diagnostics.Stopwatch sw;

        public FrameTimeCounter(int bufferedEntryCount)
        {
            if (bufferedEntryCount <= 0) throw new ArgumentOutOfRangeException("bufferedEntryCount");
            times = new double[bufferedEntryCount];
            frames = new int[bufferedEntryCount];
            sw = new System.Diagnostics.Stopwatch();
            sw.Start();
        }

        public void PushFrameCount(int frames)
        {
            if (frames < 0) throw new ArgumentOutOfRangeException("frames");
            var time = sw.Elapsed.TotalSeconds;
            sw.Restart();
            times[index] = time;
            this.frames[index] = frames;
            index = (index + 1) % times.Length;
            if (index > count) count = index;
        }

        public void Clear()
        {
            index = count = 0;
        }

        public void Shift(int count)
        {
            index = (index + count) % times.Length;
            if (index < 0) index += times.Length;
            if (index > count) count = index;
        }

        public double[] TimeFrames => times;

        public int[] FrameCounts => frames;

        public int MaxBuffer => times.Length;

        public int UsedBuffer => count;

        public double Time
        {
            get
            {
                double sum = 0;
                for (int i = 0; i < count; ++i) sum += times[i];
                return sum;
            }
        }

        public int Frames
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < count; ++i) sum += frames[i];
                return sum;
            }
        }

        public double Fps => Frames / Time;

        public override string ToString()
        {
            return Fps.ToString("#0.00");
        }

        public void Dispose()
        {
            sw.Stop();
        }
    }
}
