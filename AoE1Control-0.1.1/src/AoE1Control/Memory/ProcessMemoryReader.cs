using System.ComponentModel;
using System.Runtime.InteropServices;
using AoE1Control.Native;
using Microsoft.Win32.SafeHandles;

namespace AoE1Control.Memory;

internal sealed class ProcessMemoryReader
{
    private readonly SafeProcessHandle _handle;

    internal ProcessMemoryReader(SafeProcessHandle handle)
    {
        _handle = handle;
    }

    internal uint ReadPointer32(uint address) =>
        ReadUInt32(address);

    internal uint ReadUInt32(uint address)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        Read(address, buffer);
        return BitConverter.ToUInt32(buffer);
    }

    internal float ReadSingle(uint address)
    {
        Span<byte> buffer = stackalloc byte[sizeof(float)];
        Read(address, buffer);
        return BitConverter.ToSingle(buffer);
    }


    internal byte[] ReadBytes(uint address, int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        byte[] buffer = new byte[length];
        Read(address, buffer);
        return buffer;
    }

    internal bool CanRead(uint address, int length)
    {
        if (address == 0 || length <= 0)
            return false;

        NativeMethods.MemoryBasicInformation information;

        UIntPtr result = NativeMethods.VirtualQueryEx(
            _handle,
            new IntPtr(address),
            out information,
            (UIntPtr)Marshal.SizeOf<NativeMethods.MemoryBasicInformation>());

        if (result == UIntPtr.Zero)
            return false;

        if (information.State != NativeMethods.MEM_COMMIT)
            return false;

        if (!NativeMethods.IsReadableProtection(information.Protect))
            return false;

        ulong regionStart =
            unchecked((ulong)information.BaseAddress.ToInt64());

        ulong regionSize =
            information.RegionSize.ToUInt64();

        ulong requestedStart = address;
        ulong requestedEnd = requestedStart + (ulong)length;
        ulong regionEnd = regionStart + regionSize;

        return requestedStart >= regionStart &&
               requestedEnd <= regionEnd;
    }

    private void Read(uint address, Span<byte> destination)
    {
        if (!CanRead(address, destination.Length))
            throw new MemoryReadException(
                $"Endereço não legível: 0x{address:X8}");

        byte[] buffer = new byte[destination.Length];

        bool success = NativeMethods.ReadProcessMemory(
            _handle,
            new IntPtr(address),
            buffer,
            buffer.Length,
            out IntPtr bytesRead);

        if (!success || bytesRead.ToInt64() != buffer.Length)
        {
            int error = Marshal.GetLastWin32Error();

            throw new MemoryReadException(
                $"ReadProcessMemory falhou em 0x{address:X8}. Win32={error}",
                new Win32Exception(error));
        }

        buffer.AsSpan().CopyTo(destination);
    }
}
