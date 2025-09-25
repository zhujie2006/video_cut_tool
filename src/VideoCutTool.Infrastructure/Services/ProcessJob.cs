using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VideoCutTool.Infrastructure.Services
{
    internal static class ProcessJob
    {
        private static readonly IntPtr _jobHandle;
        private static readonly bool _initialized;

        static ProcessJob()
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    _initialized = false;
                    return;
                }

                _jobHandle = CreateJobObject(IntPtr.Zero, null);
                if (_jobHandle == IntPtr.Zero)
                {
                    _initialized = false;
                    return;
                }

                var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                info.BasicLimitInformation.LimitFlags = JOBOBJECT_LIMIT_FLAGS.JOBOBJECT_LIMIT_KILL_ON_JOB_CLOSE;

                int length = Marshal.SizeOf(info);
                IntPtr infoPtr = Marshal.AllocHGlobal(length);
                try
                {
                    Marshal.StructureToPtr(info, infoPtr, false);
                    if (!SetInformationJobObject(_jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, infoPtr, (uint)length))
                    {
                        _initialized = false;
                        CloseHandle(_jobHandle);
                        _jobHandle = IntPtr.Zero;
                        return;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(infoPtr);
                }

                AppDomain.CurrentDomain.ProcessExit += (_, __) =>
                {
                    if (_jobHandle != IntPtr.Zero)
                    {
                        CloseHandle(_jobHandle);
                    }
                };

                _initialized = true;
            }
            catch
            {
                _initialized = false;
            }
        }

        public static void TryAssign(Process process)
        {
            try
            {
                if (!_initialized || _jobHandle == IntPtr.Zero || process == null) return;
                AssignProcessToJobObject(_jobHandle, process.Handle);
            }
            catch
            {
                // best-effort; ignore
            }
        }

        private enum JOBOBJECTINFOCLASS
        {
            JobObjectExtendedLimitInformation = 9
        }

        [Flags]
        private enum JOBOBJECT_LIMIT_FLAGS : uint
        {
            JOBOBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public JOBOBJECT_LIMIT_FLAGS LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public long Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr hJob, JOBOBJECTINFOCLASS JobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}

