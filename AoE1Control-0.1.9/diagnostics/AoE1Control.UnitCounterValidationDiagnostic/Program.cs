using System.Globalization;
using System.Text;
using AoE1Control;
using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control.UnitCounterValidationDiagnostic;

internal static class Program
{
    private const uint PlayerContainerModuleOffset = 0x16D604;
    private const uint PlayerBaseFromContainerOffset = 0x04BC;
    private const uint PlayerStatePointerOffset = 0x0100;

    private const uint PopulationAvailableOffset = 0x0008;
    private const uint PopulationCurrentOffset = 0x0016;
    private const uint ScoutShipCandidateOffset = 0x002A;
    private const uint CivilianShipCandidateOffset = 0x004A;
    private const uint MilitaryPopulationOffset = 0x0050;
    private const uint VillagerCountOffset = 0x0052;
    private const uint EconomicShipCandidateOffset = 0x0066;

    private const int ScanWindowSize = 0x300;

    private static readonly ValidationPhase[] Phases =
    [
        new(
            "BASELINE",
            "Deixe a partida estável. Não conclua, converta nem perca unidades."),

        new(
            "ALDEAO_ADICIONADO",
            "Treine ou converta exatamente um aldeão."),

        new(
            "TRADE_BOAT_CONCLUIDO",
            "Conclua exatamente um Trade Boat."),

        new(
            "LIGHT_TRANSPORT_CONCLUIDO",
            "Conclua exatamente um Light Transport."),

        new(
            "FISHING_BOAT_CONCLUIDO",
            "Conclua exatamente um Fishing Boat."),

        new(
            "SCOUT_SHIP_CONCLUIDO",
            "Conclua exatamente um Scout Ship."),

        new(
            "MILITAR_TERRESTRE_CONCLUIDO",
            "Conclua exatamente uma unidade militar terrestre.")
    ];

    private static int Main()
    {
        Console.Title =
            "AoE1Control 0.1.9 — UnitCounterValidationDiagnostic";

        Console.WriteLine(
            "[AoE1Control] Carregado | " +
            "versao=0.1.9 | " +
            "diagnostico=UnitCounterValidationDiagnostic");

        Console.WriteLine(
            "[UnitCounterValidationDiagnostic] Campos | " +
            "available=Int8(+0x0008) | " +
            "population=UInt8(+0x0016) | " +
            "scoutShipCandidate=UInt8(+0x002A) | " +
            "civilianShipCandidate=UInt8(+0x004A) | " +
            "militaryPopulation=UInt8(+0x0050) | " +
            "villagers=UInt8(+0x0052) | " +
            "economicShipCandidate=UInt8(+0x0066) | " +
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

            Console.WriteLine(
                $"[UnitCounterValidationDiagnostic] Processo conectado | " +
                $"perfil={connection.Profile.ProfileId}");

            WaitForReadableState(connection);

            List<PhaseCapture> captures = [];

            foreach (ValidationPhase phase in Phases)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"[UnitCounterValidationDiagnostic] FASE | nome={phase.Name}");

                Console.WriteLine(phase.Instruction);
                Console.WriteLine(
                    "Confirme visualmente que a ação terminou e pressione ENTER.");

                Console.ReadLine();

                DirectPlayerPointers pointers =
                    ResolveDirectPlayerPointers(connection);

                byte[] bytes =
                    connection.Memory.ReadAvailableBytes(
                        pointers.PlayerState,
                        ScanWindowSize);

                PhaseCapture capture =
                    new(
                        phase.Name,
                        DateTimeOffset.UtcNow,
                        pointers.PlayerBase,
                        pointers.PlayerState,
                        unchecked((sbyte)bytes[PopulationAvailableOffset]),
                        bytes[PopulationCurrentOffset],
                        bytes[ScoutShipCandidateOffset],
                        bytes[CivilianShipCandidateOffset],
                        bytes[MilitaryPopulationOffset],
                        bytes[VillagerCountOffset],
                        bytes[EconomicShipCandidateOffset],
                        bytes);

                captures.Add(capture);

                int capacity =
                    capture.PopulationCurrent +
                    capture.PopulationAvailable;

                Console.WriteLine(
                    $"[UnitCounterValidationDiagnostic] Captura | " +
                    $"fase={phase.Name} | " +
                    $"pop={capture.PopulationCurrent}/{capacity} | " +
                    $"available={capture.PopulationAvailable} | " +
                    $"scoutShipCandidate={capture.ScoutShipCandidate} | " +
                    $"civilianShipCandidate={capture.CivilianShipCandidate} | " +
                    $"militaryPopulation={capture.MilitaryPopulation} | " +
                    $"villagers={capture.VillagerCount} | " +
                    $"economicShipCandidate={capture.EconomicShipCandidate}");
            }

            string outputDirectory =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "unit-counter-validation",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            Directory.CreateDirectory(outputDirectory);

            WriteCaptures(outputDirectory, captures);
            WriteKnownFieldAnalysis(outputDirectory, captures);
            WriteCandidates(outputDirectory, captures);

            Console.WriteLine();
            Console.WriteLine(
                "[UnitCounterValidationDiagnostic] RESULTADO | status=SUCESSO");

            Console.WriteLine(
                $"[UnitCounterValidationDiagnostic] Arquivos | diretorio={outputDirectory}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"[UnitCounterValidationDiagnostic] ERRO_FATAL | " +
                $"tipo={ex.GetType().Name} | mensagem={Sanitize(ex.Message)}");

            Console.Error.WriteLine("Pressione ENTER para encerrar.");
            Console.ReadLine();
            return 1;
        }
    }

    private static void WaitForReadableState(GameConnection connection)
    {
        int attempts = 0;

        while (true)
        {
            attempts++;

            try
            {
                DirectPlayerPointers pointers =
                    ResolveDirectPlayerPointers(connection);

                sbyte available =
                    unchecked(
                        (sbyte)connection.Memory.ReadAvailableBytes(
                            checked(
                                pointers.PlayerState +
                                PopulationAvailableOffset),
                            1)[0]);

                byte population =
                    connection.Memory.ReadAvailableBytes(
                        checked(
                            pointers.PlayerState +
                            PopulationCurrentOffset),
                        1)[0];

                int capacity =
                    population +
                    available;

                if (population <= 250 &&
                    available >= -250 &&
                    available <= 250 &&
                    capacity >= 0 &&
                    capacity <= 250)
                {
                    Console.WriteLine(
                        "[UnitCounterValidationDiagnostic] " +
                        $"Estado legivel detectado | " +
                        $"playerState=0x{pointers.PlayerState:X8} | " +
                        $"pop={population}/{capacity}");

                    return;
                }
            }
            catch
            {
            }

            if (attempts == 1 || attempts % 10 == 0)
            {
                Console.WriteLine(
                    "[UnitCounterValidationDiagnostic] " +
                    "Aguardando PlayerState legivel...");
            }

            Thread.Sleep(500);
        }
    }

    private static DirectPlayerPointers ResolveDirectPlayerPointers(
        GameConnection connection)
    {
        uint moduleBase =
            unchecked(
                (uint)connection.ModuleBase.ToInt64());

        uint playerContainer =
            connection.Memory.ReadPointer32(
                checked(
                    moduleBase +
                    PlayerContainerModuleOffset));

        uint playerBase =
            connection.Memory.ReadPointer32(
                checked(
                    playerContainer +
                    PlayerBaseFromContainerOffset));

        uint playerState =
            connection.Memory.ReadPointer32(
                checked(
                    playerBase +
                    PlayerStatePointerOffset));

        return new DirectPlayerPointers(
            playerContainer,
            playerBase,
            playerState);
    }

    private static void WriteCaptures(
        string directory,
        IReadOnlyList<PhaseCapture> captures)
    {
        StringBuilder csv = new();

        csv.AppendLine(
            "phase,timestampUtc,playerBase,playerState,populationAvailable,populationCurrent,populationCapacity,scoutShipCandidate,civilianShipCandidate,militaryPopulation,villagerCount,economicShipCandidate,size");

        foreach (PhaseCapture capture in captures)
        {
            int capacity =
                capture.PopulationCurrent +
                capture.PopulationAvailable;

            csv.Append(capture.Phase).Append(',')
               .Append(capture.Timestamp.UtcDateTime.ToString(
                    "O",
                    CultureInfo.InvariantCulture)).Append(',')
               .Append($"0x{capture.PlayerBase:X8}").Append(',')
               .Append($"0x{capture.PlayerState:X8}").Append(',')
               .Append(capture.PopulationAvailable).Append(',')
               .Append(capture.PopulationCurrent).Append(',')
               .Append(capacity).Append(',')
               .Append(capture.ScoutShipCandidate).Append(',')
               .Append(capture.CivilianShipCandidate).Append(',')
               .Append(capture.MilitaryPopulation).Append(',')
               .Append(capture.VillagerCount).Append(',')
               .Append(capture.EconomicShipCandidate).Append(',')
               .Append(capture.Bytes.Length)
               .AppendLine();

            File.WriteAllBytes(
                Path.Combine(directory, $"{capture.Phase}.bin"),
                capture.Bytes);
        }

        File.WriteAllText(
            Path.Combine(directory, "captures.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static void WriteKnownFieldAnalysis(
        string directory,
        IReadOnlyList<PhaseCapture> captures)
    {
        StringBuilder text = new();

        text.AppendLine(
            "AoE1Control 0.1.9 — Unit Counter Analysis");

        WriteSeries(
            text,
            "Population current (+0x0016)",
            captures.Select(x => (int)x.PopulationCurrent).ToArray());

        WriteSeries(
            text,
            "Scout ship candidate (+0x002A)",
            captures.Select(x => (int)x.ScoutShipCandidate).ToArray());

        WriteSeries(
            text,
            "Civilian ship candidate (+0x004A)",
            captures.Select(x => (int)x.CivilianShipCandidate).ToArray());

        WriteSeries(
            text,
            "Military population (+0x0050)",
            captures.Select(x => (int)x.MilitaryPopulation).ToArray());

        WriteSeries(
            text,
            "Villager count (+0x0052)",
            captures.Select(x => (int)x.VillagerCount).ToArray());

        WriteSeries(
            text,
            "Economic ship candidate (+0x0066)",
            captures.Select(x => (int)x.EconomicShipCandidate).ToArray());

        File.WriteAllText(
            Path.Combine(directory, "known-fields-analysis.txt"),
            text.ToString(),
            new UTF8Encoding(false));
    }

    private static void WriteSeries(
        StringBuilder text,
        string name,
        int[] values)
    {
        int[] deltas = new int[values.Length - 1];

        for (int i = 1; i < values.Length; i++)
            deltas[i - 1] = values[i] - values[i - 1];

        text.AppendLine();
        text.AppendLine($"{name}: {string.Join(" -> ", values)}");
        text.AppendLine(
            $"Deltas: {string.Join(", ", deltas.Select(FormatDelta))}");
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
                        .Select(c => ReadValue(c.Bytes, offset, type))
                        .ToArray();

                if (values.Any(double.IsNaN) ||
                    values.Any(double.IsInfinity))
                    continue;

                double[] deltas =
                    new double[captures.Count - 1];

                for (int i = 1; i < captures.Count; i++)
                    deltas[i - 1] = values[i] - values[i - 1];

                string? pattern =
                    Classify(deltas);

                if (pattern is null)
                    continue;

                csv.Append($"0x{offset:X4}").Append(',')
                   .Append(offset).Append(',')
                   .Append(type).Append(',')
                   .Append(string.Join(
                        "|",
                        values.Select(v => v.ToString(
                            "0.###",
                            CultureInfo.InvariantCulture)))).Append(',')
                   .Append(string.Join(
                        "|",
                        deltas.Select(v => v.ToString(
                            "+0.###;-0.###;0",
                            CultureInfo.InvariantCulture)))).Append(',')
                   .Append(pattern).Append(',')
                   .Append("CANDIDATO")
                   .AppendLine();
            }
        }

        File.WriteAllText(
            Path.Combine(directory, "unit-counter-candidates.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static string? Classify(double[] d)
    {
        // aldeão, trade boat, light transport, fishing boat, scout ship, militar terrestre
        if (Matches(d, +1, 0, 0, 0, 0, 0))
            return "ALDEAO";

        if (Matches(d, 0, +1, 0, 0, 0, 0))
            return "TRADE_BOAT";

        if (Matches(d, 0, 0, +1, 0, 0, 0))
            return "LIGHT_TRANSPORT";

        if (Matches(d, 0, 0, 0, +1, 0, 0))
            return "FISHING_BOAT";

        if (Matches(d, 0, 0, 0, 0, +1, 0))
            return "SCOUT_SHIP";

        if (Matches(d, 0, 0, 0, 0, 0, +1))
            return "MILITAR_TERRESTRE";

        if (Matches(d, 0, +1, 0, +1, 0, 0))
            return "ECONOMIC_SHIPS";

        if (Matches(d, 0, +1, +1, +1, 0, 0))
            return "CIVILIAN_SHIPS";

        if (Matches(d, 0, 0, 0, 0, +1, +1))
            return "MILITARY_POPULATION";

        if (Matches(d, +1, +1, +1, +1, +1, +1))
            return "TOTAL_POPULATION";

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
            "UInt8" => bytes[offset],
            "Int16" => BitConverter.ToInt16(bytes, offset),
            "UInt16" => BitConverter.ToUInt16(bytes, offset),
            "Int32" => BitConverter.ToInt32(bytes, offset),
            "UInt32" => BitConverter.ToUInt32(bytes, offset),
            "Float32" => BitConverter.ToSingle(bytes, offset),
            _ => double.NaN
        };
    }

    private static string FormatDelta(int value) =>
        value switch
        {
            > 0 => $"+{value}",
            < 0 => value.ToString(CultureInfo.InvariantCulture),
            _ => "0"
        };

    private static string Sanitize(string value) =>
        value.Replace('\r', ' ')
             .Replace('\n', ' ')
             .Trim();

    private sealed record DirectPlayerPointers(
        uint PlayerContainer,
        uint PlayerBase,
        uint PlayerState);

    private sealed record ValidationPhase(
        string Name,
        string Instruction);

    private sealed record PhaseCapture(
        string Phase,
        DateTimeOffset Timestamp,
        uint PlayerBase,
        uint PlayerState,
        sbyte PopulationAvailable,
        byte PopulationCurrent,
        byte ScoutShipCandidate,
        byte CivilianShipCandidate,
        byte MilitaryPopulation,
        byte VillagerCount,
        byte EconomicShipCandidate,
        byte[] Bytes);
}
