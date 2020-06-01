using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MaxLib.Tools.Watchers
{
    public class ProcessWatcher : IDisposable
    {
        public Process Process { get; }

        public float CpuUsage { get; private set; }

        public long RamUsage { get; private set; }

        private readonly PerformanceCounter process_cpu;
        private readonly float multiplier_cpu;

        public ProcessWatcher(Process process)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            CpuUsage = 0;
            RamUsage = 0;
            process_cpu = new PerformanceCounter("Process", "% Processor Time", GetInstanceNameForProcess(process));
            multiplier_cpu = 1f / Environment.ProcessorCount;
        }

        private static string GetInstanceNameForProcess(Process process)
        {
            string processName = Path.GetFileNameWithoutExtension(process.ProcessName);

            PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");
            string[] instances = cat.GetInstanceNames()
                .Where(inst => inst.StartsWith(processName))
                .ToArray();

            foreach (string instance in instances)
            {
                using (PerformanceCounter cnt = new PerformanceCounter("Process",
                    "ID Process", instance, true))
                {
                    int val = (int)cnt.RawValue;
                    if (val == process.Id)
                    {
                        return instance;
                    }
                }
            }
            return null;
        }

        public void Update()
        {
            Process.Refresh();
            if (Process.HasExited)
            {
                CpuUsage = 0;
                RamUsage = 0;
            }
            else
            {
                try
                {
                    CpuUsage = process_cpu.NextValue() * multiplier_cpu;
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        process_cpu.InstanceName = GetInstanceNameForProcess(Process);
                        CpuUsage = process_cpu.NextValue() * multiplier_cpu;
                    }
                    catch (Exception)
                    {
                        CpuUsage = 0;
                    }
                }
                RamUsage = Process.PeakWorkingSet64; 
            }
        }

        public void Dispose()
        {
            Process.Dispose();
            process_cpu.Dispose();
        }
    }
}
