using System.Globalization;
using System.Text;
using AoE1Control;
using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control.CivilianPopulationValidationDiagnostic;

internal static class Program
{
    private const uint PlayerStatePointerOffset = 0x0100;
    private const uint PopulationAvailableOffset = 0x0008;
    private const uint PopulationCurrentOffset = 0x0016;
    private const uint CivilianCandidateOffset = 0x004A;
    private const uint MilitaryCandidateAOffset = 0x0050;
    private const uint MilitaryCandidateBOffset = 0x0066;

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
            "BARCO_PESCA_CONCLUIDO",
            "Conclua exatamente um barco de pesca."),

        new(
            "BARCO_TRANSPORTE_CONCLUIDO",
            "Conclua exatamente um barco de transporte."),

        new(
            "SACERDOTE_CONCLUIDO",
            "Conclua exatamente um sacerdote."),

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
            "AoE1Control 0.1.7 — CivilianPopulationValidationDiagnostic";

        Console.WriteLine(
            "[AoE1Control] Carregado | " +
            "versao=0.1.7 | " +
            "diagnostico=CivilianPopulationValidationDiagnostic");

        Console.WriteLine(
            "[CivilianPopulationValidationDiagnostic] Campos | " +
            "available=UInt8(+0x0008) | " +
            "population=UInt8(+0x0016) | " +
            "civilianCandidate=UInt8(+0x004A) | " +
            "militaryCandidateA=UInt8(+0x0050) | " +
            "militaryCandidateB=UInt8(+0x0066)");

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
                $"[CivilianPopulationValidationDiagnostic] Processo conectado | " +
                $"perfil={connection.Profile.ProfileId}");

            WaitForActiveSession(
                session);

            List<PhaseCapture> captures = [];

            foreach (ValidationPhase phase in Phases)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"[CivilianPopulationValidationDiagnostic] FASE | " +
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

                PhaseCapture capture =
                    new(
                        phase.Name,
                        DateTimeOffset.UtcNow,
                        pointers.PlayerBase,
                        playerState,
                        bytes[PopulationAvailableOffset],
                        bytes[PopulationCurrentOffset],
                        bytes[CivilianCandidateOffset],
                        bytes[MilitaryCandidateAOffset],
                        bytes[MilitaryCandidateBOffset],
                        bytes);

                captures.Add(capture);

                int capacity =
                    capture.PopulationCurrent +
                    capture.PopulationAvailable;

                Console.WriteLine(
                    $"[CivilianPopulationValidationDiagnostic] Captura | " +
                    $"fase={phase.Name} | " +
                    $"pop={capture.PopulationCurrent}/{capacity} | " +
                    $"available={capture.PopulationAvailable} | " +
                    $"civilianCandidate={capture.CivilianCandidate} | " +
                    $"militaryA={capture.MilitaryCandidateA} | " +
                    $"militaryB={capture.MilitaryCandidateB}");
            }

            string outputDirectory =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "civilian-population-validation",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            Directory.CreateDirectory(outputDirectory);

            WriteCaptures(
                outputDirectory,
                captures);

            WriteKnownFieldAnalysis(
                outputDirectory,
                captures);

            WriteCandidates(
                outputDirectory,
                captures);

            Console.WriteLine();
            Console.WriteLine(
                "[CivilianPopulationValidationDiagnostic] RESULTADO | " +
                "status=SUCESSO");

            Console.WriteLine(
                $"[CivilianPopulationValidationDiagnostic] Arquivos | " +
                $"diretorio={outputDirectory}");

            Console.WriteLine(
                "[CivilianPopulationValidationDiagnostic] Consulte: " +
                "known-fields-analysis.txt e civilian-candidates.csv");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"[CivilianPopulationValidationDiagnostic] ERRO_FATAL | " +
                $"tipo={ex.GetType().Name} | " +
                $"mensagem={Sanitize(ex.Message)}");

            Console.Error.WriteLine(
                "Pressione ENTER para encerrar.");

            Console.ReadLine();
            return 1;
        }
    }

    private static void WaitForActiveSession(
        GameSessionReader session)
    {
        Console.WriteLine(
            "[CivilianPopulationValidationDiagnostic] " +
            "Aguardando partida ativa...");

        int attempts = 0;

        while (!session.IsSessionActive())
        {
            attempts++;

            if (attempts % 10 == 0)
            {
                Console.WriteLine(
                    "[CivilianPopulationValidationDiagnostic] " +
                    "Sessao ainda inativa. Entre em uma partida e aguarde o carregamento.");
            }

            Thread.Sleep(500);
        }

        Console.WriteLine(
            "[CivilianPopulationValidationDiagnostic] " +
            "Sessao ativa detectada.");
    }

    private static void WriteCaptures(
        string directory,
        IReadOnlyList<PhaseCapture> captures)
    {
        StringBuilder csv = new();

        csv.AppendLine(
            "phase,timestampUtc,playerBase,playerState,populationAvailable,populationCurrent,populationCapacity,civilianCandidate,militaryCandidateA,militaryCandidateB,size");

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
               .Append(capture.MilitaryCandidateA).Append(',')
               .Append(capture.MilitaryCandidateB).Append(',')
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

    private static void WriteKnownFieldAnalysis(
        string directory,
        IReadOnlyList<PhaseCapture> captures)
    {
        int[] civilianDeltas =
            BuildDeltas(
                captures,
                x => x.CivilianCandidate);

        int[] militaryADeltas =
            BuildDeltas(
                captures,
                x => x.MilitaryCandidateA);

        int[] militaryBDeltas =
            BuildDeltas(
                captures,
                x => x.MilitaryCandidateB);

        StringBuilder text = new();

        text.AppendLine(
            "AoE1Control 0.1.7 — Known Field Analysis");

        text.AppendLine();
        text.AppendLine(
            $"Civilian candidate (+0x004A): " +
            $"{string.Join(" -> ", captures.Select(x => x.CivilianCandidate))}");

        text.AppendLine(
            $"Deltas: {string.Join(", ", civilianDeltas.Select(FormatDelta))}");

        text.AppendLine(
            $"Interpretação: {InterpretCivilian(civilianDeltas)}");

        text.AppendLine();
        text.AppendLine(
            $"Military candidate A (+0x0050): " +
            $"{string.Join(" -> ", captures.Select(x => x.MilitaryCandidateA))}");

        text.AppendLine(
            $"Deltas: {string.Join(", ", militaryADeltas.Select(FormatDelta))}");

        text.AppendLine(
            $"Interpretação: {InterpretMilitary(militaryADeltas)}");

        text.AppendLine();
        text.AppendLine(
            $"Military candidate B (+0x0066): " +
            $"{string.Join(" -> ", captures.Select(x => x.MilitaryCandidateB))}");

        text.AppendLine(
            $"Deltas: {string.Join(", ", militaryBDeltas.Select(FormatDelta))}");

        text.AppendLine(
            $"Interpretação: {InterpretMilitary(militaryBDeltas)}");

        File.WriteAllText(
            Path.Combine(
                directory,
                "known-fields-analysis.txt"),
            text.ToString(),
            new UTF8Encoding(false));
    }

    private static int[] BuildDeltas(
        IReadOnlyList<PhaseCapture> captures,
        Func<PhaseCapture, int> selector)
    {
        int[] deltas =
            new int[captures.Count - 1];

        for (int i = 1; i < captures.Count; i++)
        {
            deltas[i - 1] =
                selector(captures[i]) -
                selector(captures[i - 1]);
        }

        return deltas;
    }

    private static string InterpretCivilian(int[] deltas)
    {
        if (deltas.SequenceEqual(
            new[] { +1, +1, +1, 0, 0, 0 }))
        {
            return "ALDEAO_PESCA_TRANSPORTE";
        }

        if (deltas.SequenceEqual(
            new[] { +1, +1, +1, +1, 0, 0 }))
        {
            return "POPULACAO_CIVIL_INCLUI_SACERDOTE";
        }

        return "INCONCLUSIVO";
    }

    private static string InterpretMilitary(int[] deltas)
    {
        if (deltas.SequenceEqual(
            new[] { 0, 0, 0, 0, +1, +1 }))
        {
            return "POPULACAO_MILITAR_TERRESTRE_E_NAVAL";
        }

        if (deltas.SequenceEqual(
            new[] { 0, 0, 0, 0, +1, 0 }))
        {
            return "MILITAR_TERRESTRE";
        }

        if (deltas.SequenceEqual(
            new[] { 0, 0, 0, 0, 0, +1 }))
        {
            return "NAVIO_MILITAR";
        }

        return "INCONCLUSIVO";
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
                    new double[captures.Count - 1];

                for (int i = 1; i < captures.Count; i++)
                {
                    deltas[i - 1] =
                        values[i] -
                        values[i - 1];
                }

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
            Path.Combine(
                directory,
                "civilian-candidates.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static string? Classify(double[] deltas)
    {
        if (Matches(deltas, +1, +1, +1, 0, 0, 0))
            return "ALDEAO_PESCA_TRANSPORTE";

        if (Matches(deltas, +1, +1, +1, +1, 0, 0))
            return "CIVIL_INCLUI_SACERDOTE";

        if (Matches(deltas, 0, 0, 0, +1, 0, 0))
            return "SACERDOTE";

        if (Matches(deltas, 0, 0, 0, 0, +1, 0))
            return "MILITAR_TERRESTRE";

        if (Matches(deltas, 0, 0, 0, 0, 0, +1))
            return "NAVIO_MILITAR";

        if (Matches(deltas, 0, 0, 0, 0, +1, +1))
            return "MILITAR_TERRESTRE_E_NAVAL";

        if (Matches(deltas, +1, +1, +1, +1, +1, +1))
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

    private static string FormatDelta(int value) =>
        value switch
        {
            > 0 => $"+{value}",
            < 0 => value.ToString(
                CultureInfo.InvariantCulture),
            _ => "0"
        };

    private static string Sanitize(string value) =>
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
        byte CivilianCandidate,
        byte MilitaryCandidateA,
        byte MilitaryCandidateB,
        byte[] Bytes);
}
