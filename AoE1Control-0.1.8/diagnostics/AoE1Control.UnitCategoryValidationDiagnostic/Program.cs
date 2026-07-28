using System.Globalization;
using System.Text;
using AoE1Control;
using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control.UnitCategoryValidationDiagnostic;

internal static class Program
{
    private const uint PlayerContainerModuleOffset = 0x16D604;
    private const uint PlayerBaseFromContainerOffset = 0x04BC;
    private const uint PlayerStatePointerOffset = 0x0100;
    private const uint PopulationAvailableOffset = 0x0008;
    private const uint PopulationCurrentOffset = 0x0016;
    private const uint CivilianCandidateOffset = 0x004A;
    private const uint LandMilitaryCandidateOffset = 0x0050;
    private const uint FishingShipCandidateOffset = 0x0066;

    private const int ScanWindowSize = 0x300;

    private static readonly ValidationPhase[] Phases =
    [
        new(
            "BASELINE_DOIS_SACERDOTES",
            "Inicie Opening Moves com os dois sacerdotes vivos. Não converta nem perca unidades."),

        new(
            "SACERDOTE_PERDIDO",
            "Exclua ou deixe morrer exatamente um dos dois sacerdotes iniciais."),

        new(
            "ALDEAO_CONVERTIDO",
            "Converta exatamente um aldeão inimigo. Não converta outra unidade."),

        new(
            "ALDEAO_ADICIONAL",
            "Treine ou converta exatamente mais um aldeão."),

        new(
            "BARCO_PESCA_CONCLUIDO",
            "Conclua exatamente um barco de pesca."),

        new(
            "BARCO_TRANSPORTE_CONCLUIDO",
            "Conclua exatamente um barco de transporte."),

        new(
            "MILITAR_TERRESTRE_CONCLUIDO",
            "Conclua exatamente uma unidade militar terrestre."),

        new(
            "NAVIO_MILITAR_CONCLUIDO",
            "Conclua exatamente um navio militar.")
    ];

    private static int Main()
    {
        Console.Title =
            "AoE1Control 0.1.8 — UnitCategoryValidationDiagnostic";

        Console.WriteLine(
            "[AoE1Control] Carregado | " +
            "versao=0.1.8 | " +
            "diagnostico=UnitCategoryValidationDiagnostic");

        Console.WriteLine(
            "[UnitCategoryValidationDiagnostic] Cenario | " +
            "nome=Opening Moves");

        Console.WriteLine(
            "[UnitCategoryValidationDiagnostic] Campos | " +
            "available=Int8(+0x0008) | " +
            "population=UInt8(+0x0016) | " +
            "civilian=UInt8(+0x004A) | " +
            "landMilitary=UInt8(+0x0050) | " +
            "fishingShip=UInt8(+0x0066) | " +
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
                $"[UnitCategoryValidationDiagnostic] Processo conectado | " +
                $"perfil={connection.Profile.ProfileId}");

            WaitForReadablePlayerState(
                session,
                connection);

            List<PhaseCapture> captures = [];

            foreach (ValidationPhase phase in Phases)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"[UnitCategoryValidationDiagnostic] FASE | nome={phase.Name}");

                Console.WriteLine(phase.Instruction);
                Console.WriteLine(
                    "Confirme visualmente que a ação terminou e pressione ENTER.");

                Console.ReadLine();

                WaitForReadablePlayerState(
                    session,
                    connection);

                DirectPlayerPointers pointers =
                    ResolveDirectPlayerPointers(
                        connection);

                uint playerState =
                    pointers.PlayerState;

                byte[] bytes =
                    connection.Memory.ReadAvailableBytes(
                        playerState,
                        ScanWindowSize);

                PhaseCapture capture =
                    new(
                        phase.Name,
                        DateTimeOffset.UtcNow,
                        pointers.PlayerBase,
                        playerState,
                        unchecked((sbyte)bytes[PopulationAvailableOffset]),
                        bytes[PopulationCurrentOffset],
                        bytes[CivilianCandidateOffset],
                        bytes[LandMilitaryCandidateOffset],
                        bytes[FishingShipCandidateOffset],
                        bytes);

                captures.Add(capture);

                int capacity =
                    capture.PopulationCurrent +
                    capture.PopulationAvailable;

                Console.WriteLine(
                    $"[UnitCategoryValidationDiagnostic] Captura | " +
                    $"fase={phase.Name} | " +
                    $"pop={capture.PopulationCurrent}/{capacity} | " +
                    $"available={capture.PopulationAvailable} | " +
                    $"civilian={capture.CivilianCandidate} | " +
                    $"landMilitary={capture.LandMilitaryCandidate} | " +
                    $"fishingShip={capture.FishingShipCandidate}");
            }

            string outputDirectory =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "unit-category-validation",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            Directory.CreateDirectory(outputDirectory);

            WriteCaptures(outputDirectory, captures);
            WriteKnownFieldAnalysis(outputDirectory, captures);
            WriteCandidates(outputDirectory, captures);

            Console.WriteLine();
            Console.WriteLine(
                "[UnitCategoryValidationDiagnostic] RESULTADO | status=SUCESSO");

            Console.WriteLine(
                $"[UnitCategoryValidationDiagnostic] Arquivos | diretorio={outputDirectory}");

            Console.WriteLine(
                "[UnitCategoryValidationDiagnostic] Consulte known-fields-analysis.txt e unit-category-candidates.csv");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"[UnitCategoryValidationDiagnostic] ERRO_FATAL | " +
                $"tipo={ex.GetType().Name} | mensagem={Sanitize(ex.Message)}");

            Console.Error.WriteLine("Pressione ENTER para encerrar.");
            Console.ReadLine();
            return 1;
        }
    }

    private static void WaitForReadablePlayerState(
        GameSessionReader session,
        GameConnection connection)
    {
        int attempts = 0;

        while (true)
        {
            attempts++;

            bool sessionFlagActive =
                session.IsSessionActive();

            try
            {
                DirectPlayerPointers pointers =
                    ResolveDirectPlayerPointers(
                        connection);

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

                byte civilian =
                    connection.Memory.ReadAvailableBytes(
                        checked(
                            pointers.PlayerState +
                            CivilianCandidateOffset),
                        1)[0];

                int capacity =
                    population +
                    available;

                bool plausible =
                    pointers.PlayerContainer >= 0x00010000 &&
                    pointers.PlayerContainer < 0x80000000 &&
                    pointers.PlayerBase >= 0x00010000 &&
                    pointers.PlayerBase < 0x80000000 &&
                    pointers.PlayerState >= 0x00010000 &&
                    pointers.PlayerState < 0x80000000 &&
                    population <= 250 &&
                    available >= -250 &&
                    available <= 250 &&
                    capacity >= 0 &&
                    capacity <= 250 &&
                    civilian <= population;

                if (plausible)
                {
                    Console.WriteLine(
                        "[UnitCategoryValidationDiagnostic] " +
                        $"Estado legivel detectado | " +
                        $"sessionFlag={(sessionFlagActive ? 1 : 0)} | " +
                        $"playerContainer=0x{pointers.PlayerContainer:X8} | " +
                        $"playerBase=0x{pointers.PlayerBase:X8} | " +
                        $"playerState=0x{pointers.PlayerState:X8} | " +
                        $"pop={population}/{capacity}");

                    return;
                }

                if (attempts == 1 || attempts % 10 == 0)
                {
                    Console.WriteLine(
                        "[UnitCategoryValidationDiagnostic] " +
                        $"Estado ainda implausivel | " +
                        $"sessionFlag={(sessionFlagActive ? 1 : 0)} | " +
                        $"playerContainer=0x{pointers.PlayerContainer:X8} | " +
                        $"playerBase=0x{pointers.PlayerBase:X8} | " +
                        $"playerState=0x{pointers.PlayerState:X8} | " +
                        $"population={population} | " +
                        $"available={available} | " +
                        $"civilian={civilian}");
                }
            }
            catch (Exception ex)
                when (ex is AoE1ControlException
                    or OverflowException
                    or ArgumentOutOfRangeException)
            {
                if (attempts == 1 || attempts % 10 == 0)
                {
                    Console.WriteLine(
                        "[UnitCategoryValidationDiagnostic] " +
                        $"Cadeia ainda indisponivel | " +
                        $"sessionFlag={(sessionFlagActive ? 1 : 0)} | " +
                        $"tipo={ex.GetType().Name} | " +
                        $"mensagem={Sanitize(ex.Message)}");
                }
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

        uint playerContainerAddress =
            checked(
                moduleBase +
                PlayerContainerModuleOffset);

        uint playerContainer =
            connection.Memory.ReadPointer32(
                playerContainerAddress);

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
            "phase,timestampUtc,playerBase,playerState,populationAvailable,populationCurrent,populationCapacity,civilianCandidate,landMilitaryCandidate,fishingShipCandidate,size");

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
               .Append(capture.CivilianCandidate).Append(',')
               .Append(capture.LandMilitaryCandidate).Append(',')
               .Append(capture.FishingShipCandidate).Append(',')
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
            "AoE1Control 0.1.8 — Known Field Analysis");

        WriteSeries(
            text,
            "Population current (+0x0016)",
            captures.Select(x => (int)x.PopulationCurrent).ToArray());

        WriteSeries(
            text,
            "Civilian candidate (+0x004A)",
            captures.Select(x => (int)x.CivilianCandidate).ToArray());

        WriteSeries(
            text,
            "Land military candidate (+0x0050)",
            captures.Select(x => (int)x.LandMilitaryCandidate).ToArray());

        WriteSeries(
            text,
            "Fishing ship candidate (+0x0066)",
            captures.Select(x => (int)x.FishingShipCandidate).ToArray());

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
                {
                    continue;
                }

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
            Path.Combine(directory, "unit-category-candidates.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static string? Classify(double[] d)
    {
        // Fases:
        // sacerdote perdido, aldeão convertido, aldeão adicional,
        // pesca, transporte, militar terrestre, navio militar.
        if (Matches(d, -1, 0, 0, 0, 0, 0, 0))
            return "SACERDOTE";

        if (Matches(d, 0, +1, +1, 0, 0, 0, 0))
            return "ALDEOES";

        if (Matches(d, 0, 0, 0, +1, 0, 0, 0))
            return "BARCO_PESCA";

        if (Matches(d, 0, 0, 0, 0, +1, 0, 0))
            return "BARCO_TRANSPORTE";

        if (Matches(d, 0, 0, 0, 0, 0, +1, 0))
            return "MILITAR_TERRESTRE";

        if (Matches(d, 0, 0, 0, 0, 0, 0, +1))
            return "NAVIO_MILITAR";

        if (Matches(d, 0, +1, +1, +1, +1, 0, 0))
            return "POPULACAO_CIVIL";

        if (Matches(d, 0, 0, 0, 0, 0, +1, +1))
            return "POPULACAO_MILITAR";

        if (Matches(d, -1, +1, +1, +1, +1, +1, +1))
            return "TOTAL_DE_UNIDADES";

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
        byte CivilianCandidate,
        byte LandMilitaryCandidate,
        byte FishingShipCandidate,
        byte[] Bytes);
}
