using System.Diagnostics;

public static class MemoryMonitor
{
    public struct MemorySnapshot
    {
        public long PrivateWorkingSetBytes { get; set; }
        public long VirtualMemorySizeBytes { get; set; }
        public long ManagedHeapSizeBytes { get; set; }

        public float PrivateWorkingSetMB => PrivateWorkingSetBytes / (1024f * 1024f);
        public float VirtualMemorySizeMB => VirtualMemorySizeBytes / (1024f * 1024f);
        public float ManagedHeapSizeMB => ManagedHeapSizeBytes / (1024f * 1024f);
    }

    public static MemorySnapshot LogMemoryUsage()
    {
        MemorySnapshot snap = new MemorySnapshot();

        Process currentProcess = Process.GetCurrentProcess();

        snap.PrivateWorkingSetBytes = currentProcess.PrivateMemorySize64;
        snap.VirtualMemorySizeBytes = currentProcess.VirtualMemorySize64;
        snap.ManagedHeapSizeBytes = GC.GetTotalMemory(forceFullCollection: false);

        return snap;
    }
}