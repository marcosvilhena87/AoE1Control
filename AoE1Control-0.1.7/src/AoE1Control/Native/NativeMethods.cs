using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AoE1Control.Native;

internal static class NativeMethods
{
    internal const uint PROCESS_VM_READ = 0x0010;
    internal const uint PROCESS_QUERY_INFORMATION = 0x0400;

    internal const uint MEM_COMMIT = 0x1000;

    internal const uint PAGE_NOACCESS = 0x01;
    internal const uint PAGE_GUARD = 0x100;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryBasicInformation
    {
        internal IntPtr BaseAddress;
        internal IntPtr AllocationBase;
        internal uint AllocationProtect;
        internal UIntPtr RegionSize;
        internal uint State;
        internal uint Protect;
        internal uint Type;
    }

    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    internal static extern SafeProcessHandle OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        int processId);

    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReadProcessMemory(
        SafeProcessHandle processHandle,
        IntPtr baseAddress,
        [Out] byte[] buffer,
        int size,
        out IntPtr numberOfBytesRead);

    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    internal static extern UIntPtr VirtualQueryEx(
        SafeProcessHandle processHandle,
        IntPtr address,
        out MemoryBasicInformation buffer,
        UIntPtr length);

    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWow64Process(
        IntPtr processHandle,
        [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

    internal static bool IsReadableProtection(uint protection)
    {
        if ((protection & PAGE_GUARD) != 0)
            return false;

        if ((protection & PAGE_NOACCESS) != 0)
            return false;

        return true;
    }
}
