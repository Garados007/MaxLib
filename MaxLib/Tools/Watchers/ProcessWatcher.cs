using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MaxLib.Tools.Watchers
{
    public class ProcessWatcher : IDisposable
    {
        public Process Process { get; }

        public float CpuUsage { get; private set; }

        public long RamUsage { get; private set; }

        private readonly PerformanceCounter process_cpu;
        private readonly PerformanceCounter total_cpu;

        public ProcessWatcher(Process process)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            CpuUsage = 0;
            RamUsage = 0;
            total_cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            process_cpu = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
        }

        public void Update()
        {
            CpuUsage = total_cpu.NextValue() * 0.01f * process_cpu.NextValue();
            Process.Refresh();
            RamUsage = Process.PeakWorkingSet64; 
        }

        public void Dispose()
        {
            Process.Dispose();
            total_cpu.Dispose();
            process_cpu.Dispose();
        }
    }
}
