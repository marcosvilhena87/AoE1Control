using System.ComponentModel;
using System.Runtime.InteropServices;
using AoE1Control.Native;

namespace AoE1Control.Utilities;

internal static class ArchitectureUtility
{
    internal static bool IsWow64X86(IntPtr processHandle)
    {
        if (!Environment.Is64BitOperatingSystem)
            return true;

        if (!NativeMethods.IsWow64Process(
                processHandle,
                out bool wow64))
        {
            int error = Marshal.GetLastWin32Error();

            throw new AoE1ControlException(
                $"IsWow64Process falhou. Win32={error}",
                new Win32Exception(error));
        }

        return wow64;
    }
}
