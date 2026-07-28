using System.Globalization;

namespace AoE1Control.Profiles;

internal static class HexParser
{
    internal static uint ParseUInt32(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException(
                "Valor hexadecimal vazio.");

        string normalized = value.Trim();

        if (normalized.StartsWith(
                "0x",
                StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        if (!uint.TryParse(
                normalized,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out uint result))
        {
            throw new InvalidDataException(
                $"Valor hexadecimal inválido: {value}");
        }

        return result;
    }
}
