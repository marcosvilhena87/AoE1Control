using System.Globalization;

namespace AoE1Control.PlayerBaseSnapshotDiagnostic;

internal static class ValueDecoder
{
    internal static string UInt8(byte[] data, int offset) =>
        data[offset].ToString(CultureInfo.InvariantCulture);

    internal static string Int16(byte[] data, int offset) =>
        offset + 2 <= data.Length
            ? BitConverter.ToInt16(data, offset).ToString(CultureInfo.InvariantCulture)
            : string.Empty;

    internal static string UInt16(byte[] data, int offset) =>
        offset + 2 <= data.Length
            ? BitConverter.ToUInt16(data, offset).ToString(CultureInfo.InvariantCulture)
            : string.Empty;

    internal static string Int32(byte[] data, int offset) =>
        offset + 4 <= data.Length
            ? BitConverter.ToInt32(data, offset).ToString(CultureInfo.InvariantCulture)
            : string.Empty;

    internal static string UInt32(byte[] data, int offset) =>
        offset + 4 <= data.Length
            ? BitConverter.ToUInt32(data, offset).ToString(CultureInfo.InvariantCulture)
            : string.Empty;

    internal static string Float32(byte[] data, int offset) =>
        offset + 4 <= data.Length
            ? BitConverter.ToSingle(data, offset).ToString("R", CultureInfo.InvariantCulture)
            : string.Empty;

    internal static bool EqualsInteger(
        byte[] data,
        int offset,
        string type,
        int expected)
    {
        return type switch
        {
            "UInt8" => data[offset] == expected,
            "Int16" => offset + 2 <= data.Length &&
                       BitConverter.ToInt16(data, offset) == expected,
            "UInt16" => offset + 2 <= data.Length &&
                        BitConverter.ToUInt16(data, offset) == expected,
            "Int32" => offset + 4 <= data.Length &&
                       BitConverter.ToInt32(data, offset) == expected,
            "UInt32" => offset + 4 <= data.Length &&
                        BitConverter.ToUInt32(data, offset) == expected,
            "Float32" => offset + 4 <= data.Length &&
                         Math.Abs(BitConverter.ToSingle(data, offset) - expected) < 0.001f,
            _ => false
        };
    }

    internal static string ReadAsString(
        byte[] data,
        int offset,
        string type)
    {
        return type switch
        {
            "UInt8" => UInt8(data, offset),
            "Int16" => Int16(data, offset),
            "UInt16" => UInt16(data, offset),
            "Int32" => Int32(data, offset),
            "UInt32" => UInt32(data, offset),
            "Float32" => Float32(data, offset),
            _ => string.Empty
        };
    }
}
