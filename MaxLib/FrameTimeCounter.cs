using System;

namespace MaxLib
{
    public class FrameTimeCounter : IDisposable
    {
        int index;
        readonly System.Diagnostics.Stopwatch sw;

        public FrameTimeCounter(int bufferedEntryCount)
        {
            if (bufferedEntryCount <= 0) throw new ArgumentOutOfRangeException("bufferedEntryCount");
            TimeFrames = new double[bufferedEntryCount];
            FrameCounts = new int[bufferedEntryCount];
            sw = new System.Diagnostics.Stopwatch();
            sw.Start();
        }

        public void PushFrameCount(int frames)
        {
            if (frames < 0) throw new ArgumentOutOfRangeException("frames");
            var time = sw.Elapsed.TotalSeconds;
            sw.Restart();
            TimeFrames[index] = time;
            this.FrameCounts[index] = frames;
            index = (index + 1) % TimeFrames.Length;
            if (index > UsedBuffer) UsedBuffer = index;
        }

        public void Clear()
        {
            index = UsedBuffer = 0;
        }

        public void Shift(int count)
        {
            index = (index + count) % TimeFrames.Length;
            if (index < 0) index += TimeFrames.Length;
        }

        public double[] TimeFrames { get; }

        public int[] FrameCounts { get; }

        public int MaxBuffer => TimeFrames.Length;

        public int UsedBuffer { get; private set; }

        public double Time
        {
            get
            {
                double sum = 0;
                for (int i = 0; i < UsedBuffer; ++i) sum += TimeFrames[i];
                return sum;
            }
        }

        public int Frames
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < UsedBuffer; ++i) sum += FrameCounts[i];
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
