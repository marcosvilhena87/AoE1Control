using System.Globalization;
using System.Text;

namespace AoE1Control.PlayerBaseSnapshotDiagnostic;

internal static class DiagnosticWriter
{
    internal static void WriteAll(
        string outputDirectory,
        IReadOnlyList<MemoryCapture> captures)
    {
        Directory.CreateDirectory(outputDirectory);

        foreach (MemoryCapture capture in captures)
        {
            string safeName = Sanitize(capture.Phase.Name);

            File.WriteAllBytes(
                Path.Combine(outputDirectory, $"{safeName}.bin"),
                capture.Bytes);
        }

        WriteManifest(outputDirectory, captures);
        WriteChangedBytes(outputDirectory, captures);
        WriteDecodedValues(outputDirectory, captures);
        WriteDeltaCandidates(outputDirectory, captures);
    }

    private static void WriteManifest(
        string directory,
        IReadOnlyList<MemoryCapture> captures)
    {
        StringBuilder csv = new();
        csv.AppendLine(
            "phase,timestampUtc,playerBase,size,expectedCurrentDelta,expectedCapacityDelta");

        foreach (MemoryCapture capture in captures)
        {
            csv.Append(Escape(capture.Phase.Name)).Append(',')
               .Append(capture.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)).Append(',')
               .Append($"0x{capture.PlayerBase:X8}").Append(',')
               .Append(capture.Bytes.Length).Append(',')
               .Append(capture.Phase.ExpectedCurrentDelta?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
               .Append(capture.Phase.ExpectedCapacityDelta?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
               .AppendLine();
        }

        File.WriteAllText(
            Path.Combine(directory, "captures.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static void WriteChangedBytes(
        string directory,
        IReadOnlyList<MemoryCapture> captures)
    {
        StringBuilder csv = new();
        csv.Append("offsetHex,offsetDecimal");

        foreach (MemoryCapture capture in captures)
            csv.Append(',').Append(Escape(capture.Phase.Name));

        csv.AppendLine();

        int length = captures.Min(c => c.Bytes.Length);

        for (int offset = 0; offset < length; offset++)
        {
            byte first = captures[0].Bytes[offset];

            if (captures.All(c => c.Bytes[offset] == first))
                continue;

            csv.Append($"0x{offset:X4}").Append(',')
               .Append(offset);

            foreach (MemoryCapture capture in captures)
                csv.Append(',').Append(capture.Bytes[offset]);

            csv.AppendLine();
        }

        File.WriteAllText(
            Path.Combine(directory, "changed-bytes.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static void WriteDecodedValues(
        string directory,
        IReadOnlyList<MemoryCapture> captures)
    {
        StringBuilder csv = new();
        csv.Append("offsetHex,offsetDecimal,type");

        foreach (MemoryCapture capture in captures)
            csv.Append(',').Append(Escape(capture.Phase.Name));

        csv.AppendLine();

        string[] types =
        [
            "UInt8",
            "Int16",
            "UInt16",
            "Int32",
            "UInt32",
            "Float32"
        ];

        int length = captures.Min(c => c.Bytes.Length);

        for (int offset = 0; offset < length; offset++)
        {
            foreach (string type in types)
            {
                if (!CanDecode(type, offset, length))
                    continue;

                string[] values = captures
                    .Select(c => ValueDecoder.ReadAsString(c.Bytes, offset, type))
                    .ToArray();

                if (values.Distinct(StringComparer.Ordinal).Count() == 1)
                    continue;

                csv.Append($"0x{offset:X4}").Append(',')
                   .Append(offset).Append(',')
                   .Append(type);

                foreach (string value in values)
                    csv.Append(',').Append(value);

                csv.AppendLine();
            }
        }

        File.WriteAllText(
            Path.Combine(directory, "decoded-changes.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static void WriteDeltaCandidates(
        string directory,
        IReadOnlyList<MemoryCapture> captures)
    {
        StringBuilder csv = new();
        csv.AppendLine(
            "field,offsetHex,offsetDecimal,type,values,deltas,status");

        string[] types =
        [
            "UInt8",
            "Int16",
            "UInt16",
            "Int32",
            "UInt32",
            "Float32"
        ];

        int length = captures.Min(c => c.Bytes.Length);

        for (int offset = 0; offset < length; offset++)
        {
            foreach (string type in types)
            {
                if (!CanDecode(type, offset, length))
                    continue;

                double[] values = captures
                    .Select(c => ReadNumeric(c.Bytes, offset, type))
                    .ToArray();

                if (values.Any(double.IsNaN))
                    continue;

                double[] deltas = new double[values.Length - 1];
                for (int i = 1; i < values.Length; i++)
                    deltas[i - 1] = values[i] - values[i - 1];

                bool currentMatch =
                    MatchesExpected(
                        captures.Skip(1).Select(c => c.Phase.ExpectedCurrentDelta).ToArray(),
                        deltas);

                bool capacityMatch =
                    MatchesCapacityPattern(deltas);

                if (!currentMatch && !capacityMatch)
                    continue;

                string valuesText = string.Join(
                    "|",
                    values.Select(v => v.ToString("0.###", CultureInfo.InvariantCulture)));

                string deltasText = string.Join(
                    "|",
                    deltas.Select(v => v.ToString("+0.###;-0.###;0", CultureInfo.InvariantCulture)));

                if (currentMatch)
                {
                    AppendCandidate(
                        csv,
                        "populationCurrent",
                        offset,
                        type,
                        valuesText,
                        deltasText);
                }

                if (capacityMatch)
                {
                    AppendCandidate(
                        csv,
                        "populationCapacity",
                        offset,
                        type,
                        valuesText,
                        deltasText);
                }
            }
        }

        File.WriteAllText(
            Path.Combine(directory, "population-delta-candidates.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static bool MatchesCapacityPattern(double[] actual)
    {
        if (actual.Length != 4)
            return false;

        return Math.Abs(actual[0]) < 0.001 &&
               actual[1] > 0.001 &&
               Math.Abs(actual[2]) < 0.001 &&
               Math.Abs(actual[3]) < 0.001;
    }

    private static bool MatchesExpected(
        int?[] expected,
        double[] actual)
    {
        if (expected.Length != actual.Length)
            return false;

        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] is null)
                continue;

            if (Math.Abs(actual[i] - expected[i]!.Value) > 0.001)
                return false;
        }

        return true;
    }

    private static void AppendCandidate(
        StringBuilder csv,
        string field,
        int offset,
        string type,
        string values,
        string deltas)
    {
        csv.Append(field).Append(',')
           .Append($"0x{offset:X4}").Append(',')
           .Append(offset).Append(',')
           .Append(type).Append(',')
           .Append(Escape(values)).Append(',')
           .Append(Escape(deltas)).Append(',')
           .Append("PADRAO_EXATO")
           .AppendLine();
    }

    private static double ReadNumeric(
        byte[] data,
        int offset,
        string type)
    {
        return type switch
        {
            "UInt8" => data[offset],
            "Int16" => BitConverter.ToInt16(data, offset),
            "UInt16" => BitConverter.ToUInt16(data, offset),
            "Int32" => BitConverter.ToInt32(data, offset),
            "UInt32" => BitConverter.ToUInt32(data, offset),
            "Float32" => BitConverter.ToSingle(data, offset),
            _ => double.NaN
        };
    }

    private static bool CanDecode(
        string type,
        int offset,
        int length)
    {
        int size = type switch
        {
            "UInt8" => 1,
            "Int16" or "UInt16" => 2,
            _ => 4
        };

        return offset + size <= length;
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') &&
            !value.Contains('"') &&
            !value.Contains('\n') &&
            !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string Sanitize(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();

        return new string(
            value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
