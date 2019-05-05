using System;

namespace PiTree.Shared
{
    [Flags]
    public enum MonitorStatus
    {
        Succeeded = 1 << 0,
        PartiallySucceeded = 1 << 1,
        Failed = 1 << 2
    }
}
