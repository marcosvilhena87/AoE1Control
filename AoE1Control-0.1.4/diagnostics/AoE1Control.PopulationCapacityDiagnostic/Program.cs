using System.Globalization;
using System.Text;
using AoE1Control;
using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control.PopulationCapacityDiagnostic;

internal static class Program
{
    private const uint PlayerStatePointerOffset = 0x0100;
    private const int TargetWindowSize = 0x100;

    private static readonly ValidationPhase[] Phases =
    [
        new(
            "BASELINE",
            "Comece sem fundação de casa em andamento. Não conclua nem perca unidades."),

        new(
            "FUNDACAO_INICIADA",
            "Inicie a construção de exatamente uma casa, mas não a conclua."),

        new(
            "FUNDACAO_CANCELADA",
            "Cancele a fundação da casa."),

        new(
            "CASA_CONCLUIDA",
            "Construa e conclua exatamente uma casa."),

        new(
            "CASA_DESTRUIDA",
            "Destrua exatamente a casa concluída.")
    ];

    private static int Main()
    {
        Console.Title =
            "AoE1Control 0.1.4 — PopulationCapacityDiagnostic";

        Console.WriteLine(
            "[AoE1Control] Carregado | " +
            "versao=0.1.4 | " +
            "diagnostico=PopulationCapacityDiagnostic");

        Console.WriteLine(
            "[PopulationCapacityDiagnostic] Configuracao | " +
            "playerState=[PlayerBase+0x0100] | " +
            $"janela=0x{TargetWindowSize:X}");

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
                $"[PopulationCapacityDiagnostic] Processo conectado | " +
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
                    $"[PopulationCapacityDiagnostic] FASE | " +
                    $"nome={phase.Name}");

                Console.WriteLine(phase.Instruction);
                Console.WriteLine(
                    "Espere a ação refletir no jogo e pressione ENTER.");

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
                        TargetWindowSize);

                captures.Add(
                    new PhaseCapture(
                        phase.Name,
                        DateTimeOffset.UtcNow,
                        pointers.PlayerBase,
                        playerState,
                        bytes));

                Console.WriteLine(
                    $"[PopulationCapacityDiagnostic] Captura | " +
                    $"fase={phase.Name} | " +
                    $"playerBase=0x{pointers.PlayerBase:X8} | " +
                    $"playerState=0x{playerState:X8} | " +
                    $"bytes=0x{bytes.Length:X}");
            }

            string outputDirectory =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "population-capacity",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            Directory.CreateDirectory(outputDirectory);

            WriteCaptures(
                outputDirectory,
                captures);

            WriteCandidates(
                outputDirectory,
                captures);

            Console.WriteLine();
            Console.WriteLine(
                "[PopulationCapacityDiagnostic] RESULTADO | " +
                "status=SUCESSO");

            Console.WriteLine(
                $"[PopulationCapacityDiagnostic] Arquivos | " +
                $"diretorio={outputDirectory}");

            Console.WriteLine(
                "[PopulationCapacityDiagnostic] Consulte: " +
                "capacity-candidates.csv");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"[PopulationCapacityDiagnostic] ERRO_FATAL | " +
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
            "phase,timestampUtc,playerBase,playerState,size");

        foreach (PhaseCapture capture in captures)
        {
            csv.Append(capture.Phase).Append(',')
               .Append(capture.Timestamp.UtcDateTime.ToString(
                    "O",
                    CultureInfo.InvariantCulture)).Append(',')
               .Append($"0x{capture.PlayerBase:X8}").Append(',')
               .Append($"0x{capture.PlayerState:X8}").Append(',')
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

                string valuesText =
                    string.Join(
                        "|",
                        values.Select(v =>
                            v.ToString(
                                "0.###",
                                CultureInfo.InvariantCulture)));

                string deltasText =
                    string.Join(
                        "|",
                        deltas.Select(v =>
                            v.ToString(
                                "+0.###;-0.###;0",
                                CultureInfo.InvariantCulture)));

                csv.Append($"0x{offset:X4}").Append(',')
                   .Append(offset).Append(',')
                   .Append(type).Append(',')
                   .Append(valuesText).Append(',')
                   .Append(deltasText).Append(',')
                   .Append(pattern).Append(',')
                   .Append("CANDIDATO")
                   .AppendLine();
            }
        }

        File.WriteAllText(
            Path.Combine(directory, "capacity-candidates.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static string? Classify(double[] deltas)
    {
        bool foundationOnly =
            deltas[0] > 0.001 &&
            deltas[1] < -0.001 &&
            deltas[2] > 0.001 &&
            Nearly(deltas[3], 0);

        if (foundationOnly)
            return "CONTADOR_DE_CASAS_OU_FUNDACOES";

        bool completionCapacity =
            Nearly(deltas[0], 0) &&
            Nearly(deltas[1], 0) &&
            deltas[2] > 0.001 &&
            deltas[3] < -0.001;

        if (completionCapacity)
            return "PROVAVEL_CAPACIDADE_POPULACIONAL";

        bool completionPersistent =
            Nearly(deltas[0], 0) &&
            Nearly(deltas[1], 0) &&
            deltas[2] > 0.001 &&
            Nearly(deltas[3], 0);

        if (completionPersistent)
            return "MUDA_NA_CONCLUSAO_MAS_NAO_REVERTE";

        bool foundationAndCompletion =
            deltas[0] > 0.001 &&
            deltas[1] < -0.001 &&
            deltas[2] > 0.001 &&
            deltas[3] < -0.001;

        if (foundationAndCompletion)
            return "CONTADOR_DE_CASAS_ATIVAS";

        return null;
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

    private static bool Nearly(
        double actual,
        double expected) =>
        Math.Abs(actual - expected) <= 0.001;

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
        byte[] Bytes);
}
