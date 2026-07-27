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

    internal byte[] ReadAvailableBytes(uint address, int requestedLength)
    {
        if (requestedLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedLength));

        List<byte> result = new(requestedLength);
        uint cursor = address;
        int remaining = requestedLength;

        while (remaining > 0)
        {
            NativeMethods.MemoryBasicInformation information;

            UIntPtr queryResult = NativeMethods.VirtualQueryEx(
                _handle,
                new IntPtr(cursor),
                out information,
                (UIntPtr)Marshal.SizeOf<NativeMethods.MemoryBasicInformation>());

            if (queryResult == UIntPtr.Zero)
                break;

            if (information.State != NativeMethods.MEM_COMMIT ||
                !NativeMethods.IsReadableProtection(information.Protect))
            {
                break;
            }

            ulong regionStart =
                unchecked((ulong)information.BaseAddress.ToInt64());

            ulong regionSize =
                information.RegionSize.ToUInt64();

            ulong regionEnd = regionStart + regionSize;
            ulong current = cursor;

            if (current < regionStart || current >= regionEnd)
                break;

            int readableNow = checked(
                (int)Math.Min(
                    (ulong)remaining,
                    regionEnd - current));

            if (readableNow <= 0)
                break;

            byte[] chunk = new byte[readableNow];

            bool success = NativeMethods.ReadProcessMemory(
                _handle,
                new IntPtr(cursor),
                chunk,
                chunk.Length,
                out IntPtr bytesRead);

            int actual = checked((int)bytesRead.ToInt64());

            if (!success && actual <= 0)
                break;

            if (actual <= 0)
                break;

            result.AddRange(chunk.AsSpan(0, actual).ToArray());

            if ((ulong)cursor + (uint)actual > uint.MaxValue)
                break;

            cursor += (uint)actual;
            remaining -= actual;

            if (actual < readableNow)
                break;
        }

        if (result.Count == 0)
        {
            throw new MemoryReadException(
                $"Nenhum byte legível em 0x{address:X8}.");
        }

        return result.ToArray();
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
