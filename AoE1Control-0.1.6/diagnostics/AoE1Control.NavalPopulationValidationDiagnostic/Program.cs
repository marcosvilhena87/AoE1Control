using System.Globalization;
using System.Text;
using AoE1Control;
using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control.NavalPopulationValidationDiagnostic;

internal static class Program
{
    private const uint PlayerStatePointerOffset = 0x0100;
    private const uint PopulationAvailableOffset = 0x0008;
    private const uint PopulationCurrentOffset = 0x0016;
    private const uint VillagerCountOffset = 0x004A;

    private const int ScanWindowSize = 0x200;

    private static readonly ValidationPhase[] Phases =
    [
        new(
            "BASELINE",
            "Deixe a partida estável. Não conclua nem perca unidades."),

        new(
            "ALDEAO_CONCLUIDO",
            "Conclua exatamente um aldeão."),

        new(
            "MILITAR_CONCLUIDO",
            "Conclua exatamente uma unidade militar terrestre."),

        new(
            "BARCO_PESCA_CONCLUIDO",
            "Conclua exatamente um barco de pesca."),

        new(
            "BARCO_TRANSPORTE_CONCLUIDO",
            "Conclua exatamente um barco de transporte.")
    ];

    private static int Main()
    {
        Console.Title =
            "AoE1Control 0.1.6 — NavalPopulationValidationDiagnostic";

        Console.WriteLine(
            "[AoE1Control] Carregado | " +
            "versao=0.1.6 | " +
            "diagnostico=NavalPopulationValidationDiagnostic");

        Console.WriteLine(
            "[NavalPopulationValidationDiagnostic] Configuracao | " +
            "playerState=[PlayerBase+0x0100] | " +
            "available=UInt8(+0x0008) | " +
            "population=UInt8(+0x0016) | " +
            "villagers=UInt8(+0x004A) | " +
            $"scanWindow=0x{ScanWindowSize:X}");

        try
        {
            string profilesPath =
                Path.Combine(AppContext.BaseDirectory, "profiles");

            IReadOnlyList<GameProfile> profiles =
                new ProfileRepository(profilesPath).LoadAll();

            using GameConnection connection =
                GameConnection.Connect(
                    new AoE1ControlOptions(),
                    profiles);

            GameSessionReader session =
                new(
                    connection.Memory,
                    connection.Profile);

            PointerChainResolver resolver =
                new(
                    connection.Memory,
                    connection.ModuleBase,
                    connection.Profile);

            Console.WriteLine(
                $"[NavalPopulationValidationDiagnostic] Processo conectado | " +
                $"perfil={connection.Profile.ProfileId}");

            if (!session.IsSessionActive())
            {
                Console.WriteLine(
                    "Entre em uma partida e pressione ENTER.");

                Console.ReadLine();
            }

            if (!session.IsSessionActive())
                throw new GameSessionNotActiveException(
                    "A sessão ainda não está ativa.");

            List<PhaseCapture> captures = [];

            foreach (ValidationPhase phase in Phases)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"[NavalPopulationValidationDiagnostic] FASE | " +
                    $"nome={phase.Name}");

                Console.WriteLine(phase.Instruction);
                Console.WriteLine(
                    "Espere a ação terminar completamente e pressione ENTER.");

                Console.ReadLine();

                resolver.Invalidate();

                ResolvedPlayerPointers pointers =
                    resolver.Resolve();

                uint playerState =
                    connection.Memory.ReadPointer32(
                        checked(
                            pointers.PlayerBase +
                            PlayerStatePointerOffset));

                byte[] bytes =
                    connection.Memory.ReadAvailableBytes(
                        playerState,
                        ScanWindowSize);

                byte available =
                    bytes[PopulationAvailableOffset];

                byte population =
                    bytes[PopulationCurrentOffset];

                byte villagers =
                    bytes[VillagerCountOffset];

                int capacity =
                    population + available;

                int nonVillagers =
                    Math.Max(
                        0,
                        population - villagers);

                PhaseCapture capture =
                    new(
                        phase.Name,
                        DateTimeOffset.UtcNow,
                        pointers.PlayerBase,
                        playerState,
                        available,
                        population,
                        capacity,
                        villagers,
                        nonVillagers,
                        bytes);

                captures.Add(capture);

                Console.WriteLine(
                    $"[NavalPopulationValidationDiagnostic] Captura | " +
                    $"fase={phase.Name} | " +
                    $"pop={population}/{capacity} | " +
                    $"available={available} | " +
                    $"villagers={villagers} | " +
                    $"nonVillagers={nonVillagers} | " +
                    $"bytes=0x{bytes.Length:X}");
            }

            string outputDirectory =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "naval-population-validation",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            Directory.CreateDirectory(outputDirectory);

            WriteCaptures(
                outputDirectory,
                captures);

            WriteCandidates(
                outputDirectory,
                captures);

            WriteSummary(
                outputDirectory,
                captures);

            Console.WriteLine();
            Console.WriteLine(
                "[NavalPopulationValidationDiagnostic] RESULTADO | " +
                "status=SUCESSO");

            Console.WriteLine(
                $"[NavalPopulationValidationDiagnostic] Arquivos | " +
                $"diretorio={outputDirectory}");

            Console.WriteLine(
                "[NavalPopulationValidationDiagnostic] Consulte: " +
                "naval-candidates.csv e summary.txt");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"[NavalPopulationValidationDiagnostic] ERRO_FATAL | " +
                $"tipo={ex.GetType().Name} | " +
                $"mensagem={Sanitize(ex.Message)}");

            Console.Error.WriteLine(
                "Pressione ENTER para encerrar.");

            Console.ReadLine();
            return 1;
        }
    }

    private static void WriteCaptures(
        string directory,
        IReadOnlyList<PhaseCapture> captures)
    {
        StringBuilder csv = new();

        csv.AppendLine(
            "phase,timestampUtc,playerBase,playerState,populationAvailable,populationCurrent,populationCapacity,villagerCount,nonVillagerCount,size");

        foreach (PhaseCapture capture in captures)
        {
            csv.Append(capture.Phase).Append(',')
               .Append(capture.Timestamp.UtcDateTime.ToString(
                    "O",
                    CultureInfo.InvariantCulture)).Append(',')
               .Append($"0x{capture.PlayerBase:X8}").Append(',')
               .Append($"0x{capture.PlayerState:X8}").Append(',')
               .Append(capture.PopulationAvailable).Append(',')
               .Append(capture.PopulationCurrent).Append(',')
               .Append(capture.PopulationCapacity).Append(',')
               .Append(capture.VillagerCount).Append(',')
               .Append(capture.NonVillagerCount).Append(',')
               .Append(capture.Bytes.Length)
               .AppendLine();

            File.WriteAllBytes(
                Path.Combine(
                    directory,
                    $"{capture.Phase}.bin"),
                capture.Bytes);
        }

        File.WriteAllText(
            Path.Combine(directory, "captures.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static void WriteCandidates(
        string directory,
        IReadOnlyList<PhaseCapture> captures)
    {
        StringBuilder csv = new();

        csv.AppendLine(
            "offsetHex,offsetDecimal,type,values,deltas,pattern,status");

        string[] types =
        [
            "UInt8",
            "Int16",
            "UInt16",
            "Int32",
            "UInt32",
            "Float32"
        ];

        int commonLength =
            captures.Min(c => c.Bytes.Length);

        for (int offset = 0; offset < commonLength; offset++)
        {
            foreach (string type in types)
            {
                int size = type switch
                {
                    "UInt8" => 1,
                    "Int16" or "UInt16" => 2,
                    _ => 4
                };

                if (offset + size > commonLength)
                    continue;

                double[] values =
                    captures
                        .Select(c =>
                            ReadValue(
                                c.Bytes,
                                offset,
                                type))
                        .ToArray();

                if (values.Any(double.IsNaN) ||
                    values.Any(double.IsInfinity))
                {
                    continue;
                }

                double[] deltas =
                [
                    values[1] - values[0],
                    values[2] - values[1],
                    values[3] - values[2],
                    values[4] - values[3]
                ];

                string? pattern =
                    Classify(deltas);

                if (pattern is null)
                    continue;

                csv.Append($"0x{offset:X4}").Append(',')
                   .Append(offset).Append(',')
                   .Append(type).Append(',')
                   .Append(string.Join(
                        "|",
                        values.Select(v =>
                            v.ToString(
                                "0.###",
                                CultureInfo.InvariantCulture)))).Append(',')
                   .Append(string.Join(
                        "|",
                        deltas.Select(v =>
                            v.ToString(
                                "+0.###;-0.###;0",
                                CultureInfo.InvariantCulture)))).Append(',')
                   .Append(pattern).Append(',')
                   .Append("CANDIDATO")
                   .AppendLine();
            }
        }

        File.WriteAllText(
            Path.Combine(directory, "naval-candidates.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static string? Classify(double[] deltas)
    {
        if (Matches(deltas, +1, 0, 0, 0))
            return "PROVAVEL_ALDEOES";

        if (Matches(deltas, +1, +1, 0, 0))
            return "PROVAVEL_POPULACAO_TERRESTRE";

        if (Matches(deltas, 0, +1, 0, 0))
            return "PROVAVEL_MILITAR_TERRESTRE";

        if (Matches(deltas, 0, 0, +1, 0))
            return "PROVAVEL_BARCOS_DE_PESCA";

        if (Matches(deltas, 0, 0, 0, +1))
            return "PROVAVEL_BARCOS_DE_TRANSPORTE";

        if (Matches(deltas, 0, 0, +1, +1))
            return "PROVAVEL_POPULACAO_NAVAL";

        if (Matches(deltas, +1, +1, +1, +1))
            return "PROVAVEL_TOTAL_DE_UNIDADES";

        return null;
    }

    private static bool Matches(
        double[] actual,
        params double[] expected)
    {
        if (actual.Length != expected.Length)
            return false;

        for (int i = 0; i < actual.Length; i++)
        {
            if (Math.Abs(actual[i] - expected[i]) > 0.001)
                return false;
        }

        return true;
    }

    private static double ReadValue(
        byte[] bytes,
        int offset,
        string type)
    {
        return type switch
        {
            "UInt8" =>
                bytes[offset],

            "Int16" =>
                BitConverter.ToInt16(
                    bytes,
                    offset),

            "UInt16" =>
                BitConverter.ToUInt16(
                    bytes,
                    offset),

            "Int32" =>
                BitConverter.ToInt32(
                    bytes,
                    offset),

            "UInt32" =>
                BitConverter.ToUInt32(
                    bytes,
                    offset),

            "Float32" =>
                BitConverter.ToSingle(
                    bytes,
                    offset),

            _ =>
                double.NaN
        };
    }

    private static void WriteSummary(
        string directory,
        IReadOnlyList<PhaseCapture> captures)
    {
        StringBuilder text = new();

        text.AppendLine(
            "AoE1Control 0.1.6 — NavalPopulationValidationDiagnostic");

        text.AppendLine();

        foreach (PhaseCapture capture in captures)
        {
            text.AppendLine(
                $"{capture.Phase}: " +
                $"pop={capture.PopulationCurrent}/{capture.PopulationCapacity}, " +
                $"available={capture.PopulationAvailable}, " +
                $"villagers={capture.VillagerCount}, " +
                $"nonVillagers={capture.NonVillagerCount}");
        }

        text.AppendLine();
        text.AppendLine(
            "Consulte naval-candidates.csv para campos que respondem isoladamente a barcos.");

        File.WriteAllText(
            Path.Combine(directory, "summary.txt"),
            text.ToString(),
            new UTF8Encoding(false));
    }

    private static string Sanitize(
        string value) =>
        value.Replace('\r', ' ')
             .Replace('\n', ' ')
             .Trim();

    private sealed record ValidationPhase(
        string Name,
        string Instruction);

    private sealed record PhaseCapture(
        string Phase,
        DateTimeOffset Timestamp,
        uint PlayerBase,
        uint PlayerState,
        byte PopulationAvailable,
        byte PopulationCurrent,
        int PopulationCapacity,
        byte VillagerCount,
        int NonVillagerCount,
        byte[] Bytes);
}
