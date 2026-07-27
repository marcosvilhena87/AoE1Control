using System.Globalization;
using System.Text;
using AoE1Control;
using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control.PopulationFieldValidationDiagnostic;

internal static class Program
{
    private const uint PlayerStatePointerOffset = 0x0100;
    private const uint CandidateAOffset = 0x0016;
    private const uint CandidateBOffset = 0x004A;
    private const int PollIntervalMs = 250;

    private static readonly ValidationPhase[] Phases =
    [
        new(
            "BASELINE",
            "Deixe a partida estável, sem unidade sendo concluída ou morrendo."),

        new(
            "ALDEAO_CONCLUIDO",
            "Conclua exatamente um aldeão."),

        new(
            "UNIDADE_MILITAR_CONCLUIDA",
            "Conclua exatamente uma unidade militar."),

        new(
            "UNIDADE_MILITAR_PERDIDA",
            "Faça exatamente a unidade militar morrer."),

        new(
            "ALDEAO_PERDIDO",
            "Faça exatamente um aldeão morrer.")
    ];

    private static int Main()
    {
        Console.Title =
            "AoE1Control 0.1.3 — PopulationFieldValidationDiagnostic";

        Console.WriteLine(
            "[AoE1Control] Carregado | " +
            "versao=0.1.3 | " +
            "diagnostico=PopulationFieldValidationDiagnostic");

        Console.WriteLine(
            "[PopulationFieldValidationDiagnostic] Candidatos | " +
            "playerState=[PlayerBase+0x0100] | " +
            "candidateA=UInt8(PlayerState+0x0016) | " +
            "candidateB=UInt8(PlayerState+0x004A)");

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
                $"[PopulationFieldValidationDiagnostic] Processo conectado | " +
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

            string outputDirectory =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "population-field-validation",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            Directory.CreateDirectory(outputDirectory);

            List<PhaseSample> samples = [];

            foreach (ValidationPhase phase in Phases)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"[PopulationFieldValidationDiagnostic] FASE | " +
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

                byte candidateA =
                    connection.Memory.ReadAvailableBytes(
                        checked(
                            playerState +
                            CandidateAOffset),
                        1)[0];

                byte candidateB =
                    connection.Memory.ReadAvailableBytes(
                        checked(
                            playerState +
                            CandidateBOffset),
                        1)[0];

                PhaseSample sample = new(
                    phase.Name,
                    DateTimeOffset.UtcNow,
                    pointers.PlayerBase,
                    playerState,
                    candidateA,
                    candidateB);

                samples.Add(sample);

                Console.WriteLine(
                    $"[PopulationFieldValidationDiagnostic] Amostra | " +
                    $"fase={phase.Name} | " +
                    $"playerBase=0x{pointers.PlayerBase:X8} | " +
                    $"playerState=0x{playerState:X8} | " +
                    $"candidateA={candidateA} | " +
                    $"candidateB={candidateB}");
            }

            WriteSamples(
                outputDirectory,
                samples);

            WriteAnalysis(
                outputDirectory,
                samples);

            Console.WriteLine();
            Console.WriteLine(
                "[PopulationFieldValidationDiagnostic] RESULTADO | " +
                "status=SUCESSO");

            Console.WriteLine(
                $"[PopulationFieldValidationDiagnostic] Arquivos | " +
                $"diretorio={outputDirectory}");

            Console.WriteLine(
                "[PopulationFieldValidationDiagnostic] Consulte: " +
                "analysis.txt e samples.csv");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"[PopulationFieldValidationDiagnostic] ERRO_FATAL | " +
                $"tipo={ex.GetType().Name} | " +
                $"mensagem={Sanitize(ex.Message)}");

            Console.Error.WriteLine(
                "Pressione ENTER para encerrar.");

            Console.ReadLine();
            return 1;
        }
    }

    private static void WriteSamples(
        string directory,
        IReadOnlyList<PhaseSample> samples)
    {
        StringBuilder csv = new();

        csv.AppendLine(
            "phase,timestampUtc,playerBase,playerState,candidateA,candidateB,deltaA,deltaB");

        for (int i = 0; i < samples.Count; i++)
        {
            PhaseSample sample = samples[i];

            int? deltaA =
                i == 0
                    ? null
                    : sample.CandidateA -
                      samples[i - 1].CandidateA;

            int? deltaB =
                i == 0
                    ? null
                    : sample.CandidateB -
                      samples[i - 1].CandidateB;

            csv.Append(sample.Phase).Append(',')
               .Append(sample.Timestamp.UtcDateTime.ToString(
                    "O",
                    CultureInfo.InvariantCulture)).Append(',')
               .Append($"0x{sample.PlayerBase:X8}").Append(',')
               .Append($"0x{sample.PlayerState:X8}").Append(',')
               .Append(sample.CandidateA).Append(',')
               .Append(sample.CandidateB).Append(',')
               .Append(deltaA?.ToString(
                    CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
               .Append(deltaB?.ToString(
                    CultureInfo.InvariantCulture) ?? string.Empty)
               .AppendLine();
        }

        File.WriteAllText(
            Path.Combine(directory, "samples.csv"),
            csv.ToString(),
            new UTF8Encoding(false));
    }

    private static void WriteAnalysis(
        string directory,
        IReadOnlyList<PhaseSample> samples)
    {
        int[] deltaA = BuildDeltas(samples, x => x.CandidateA);
        int[] deltaB = BuildDeltas(samples, x => x.CandidateB);

        string interpretationA =
            Interpret(deltaA);

        string interpretationB =
            Interpret(deltaB);

        StringBuilder text = new();

        text.AppendLine(
            "AoE1Control 0.1.3 — PopulationFieldValidationDiagnostic");

        text.AppendLine();
        text.AppendLine(
            $"Candidate A: PlayerState + 0x{CandidateAOffset:X4}");

        text.AppendLine(
            $"Valores: {string.Join(" -> ", samples.Select(s => s.CandidateA))}");

        text.AppendLine(
            $"Deltas: {string.Join(", ", deltaA.Select(FormatDelta))}");

        text.AppendLine(
            $"Interpretação: {interpretationA}");

        text.AppendLine();
        text.AppendLine(
            $"Candidate B: PlayerState + 0x{CandidateBOffset:X4}");

        text.AppendLine(
            $"Valores: {string.Join(" -> ", samples.Select(s => s.CandidateB))}");

        text.AppendLine(
            $"Deltas: {string.Join(", ", deltaB.Select(FormatDelta))}");

        text.AppendLine(
            $"Interpretação: {interpretationB}");

        File.WriteAllText(
            Path.Combine(directory, "analysis.txt"),
            text.ToString(),
            new UTF8Encoding(false));

        Console.WriteLine();
        Console.WriteLine(
            $"[PopulationFieldValidationDiagnostic] Analise | " +
            $"candidateA={interpretationA}");

        Console.WriteLine(
            $"[PopulationFieldValidationDiagnostic] Analise | " +
            $"candidateB={interpretationB}");
    }

    private static int[] BuildDeltas(
        IReadOnlyList<PhaseSample> samples,
        Func<PhaseSample, int> selector)
    {
        int[] result =
            new int[samples.Count - 1];

        for (int i = 1; i < samples.Count; i++)
        {
            result[i - 1] =
                selector(samples[i]) -
                selector(samples[i - 1]);
        }

        return result;
    }

    private static string Interpret(int[] deltas)
    {
        if (deltas.Length != 4)
            return "INCONCLUSIVO";

        bool villagerPattern =
            deltas[0] == +1 &&
            deltas[1] == 0 &&
            deltas[2] == 0 &&
            deltas[3] == -1;

        bool totalPopulationPattern =
            deltas[0] == +1 &&
            deltas[1] == +1 &&
            deltas[2] == -1 &&
            deltas[3] == -1;

        if (villagerPattern)
            return "PROVAVEL_CONTAGEM_DE_ALDEOES";

        if (totalPopulationPattern)
            return "PROVAVEL_POPULACAO_TOTAL";

        if (deltas.SequenceEqual(new[] { 0, +1, -1, 0 }))
            return "PROVAVEL_CONTAGEM_MILITAR";

        return "INCONCLUSIVO";
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

    private sealed record PhaseSample(
        string Phase,
        DateTimeOffset Timestamp,
        uint PlayerBase,
        uint PlayerState,
        byte CandidateA,
        byte CandidateB);
}
