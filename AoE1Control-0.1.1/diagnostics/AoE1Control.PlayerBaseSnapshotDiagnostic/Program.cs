using AoE1Control;
using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control.PlayerBaseSnapshotDiagnostic;

internal static class Program
{
    private const int SnapshotSize = 0x800;

    private static readonly CapturePhase[] Phases =
    [
        new(
            "BASELINE",
            "Comece com 3 aldeões e limite populacional 4. Não altere a população.",
            null,
            null),

        new(
            "POPULACAO_MAIS_1",
            "Conclua exatamente um aldeão, passando de 3 para 4 aldeões.",
            +1,
            0),

        new(
            "CAPACIDADE_AUMENTOU",
            "Construa e conclua exatamente uma casa. Não conclua nem perca unidades nesta fase.",
            0,
            null),

        new(
            "POPULACAO_MAIS_1_NOVAMENTE",
            "Depois da casa concluída, conclua exatamente mais um aldeão.",
            +1,
            0),

        new(
            "POPULACAO_MENOS_1",
            "Faça exatamente um aldeão morrer para reduzir a população em -1.",
            -1,
            0)
    ];

    private static int Main()
    {
        Console.Title =
            "AoE1Control 0.1.1 — PlayerBaseSnapshotDiagnostic";

        Console.WriteLine(
            "[AoE1Control] Carregado | " +
            "versao=0.1.1 | " +
            "diagnostico=PlayerBaseSnapshotDiagnostic");

        Console.WriteLine(
            "[PlayerBaseSnapshotDiagnostic] Metodo | " +
            "comparacao=DELTAS_CONTROLADOS | " +
            "populacaoVisivel=NAO_NECESSARIA");

        try
        {
            AoE1ControlOptions options = new();

            string profilesDirectory =
                options.ProfilesDirectory
                ?? Path.Combine(AppContext.BaseDirectory, "profiles");

            IReadOnlyList<GameProfile> profiles =
                new ProfileRepository(profilesDirectory).LoadAll();

            using GameConnection connection =
                GameConnection.Connect(options, profiles);

            GameSessionReader sessionReader =
                new(connection.Memory, connection.Profile);

            PointerChainResolver resolver =
                new(
                    connection.Memory,
                    connection.ModuleBase,
                    connection.Profile);

            Console.WriteLine(
                $"[PlayerBaseSnapshotDiagnostic] Processo conectado | " +
                $"perfil={connection.Profile.ProfileId} | " +
                $"moduloBase=0x{connection.ModuleBase.ToInt64():X8}");

            if (!sessionReader.IsSessionActive())
            {
                Console.WriteLine(
                    "[PlayerBaseSnapshotDiagnostic] " +
                    "Entre em uma partida e pressione ENTER.");

                Console.ReadLine();
            }

            if (!sessionReader.IsSessionActive())
                throw new GameSessionNotActiveException(
                    "A sessão ainda não está ativa.");

            List<MemoryCapture> captures = [];

            foreach (CapturePhase phase in Phases)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"[PlayerBaseSnapshotDiagnostic] FASE | nome={phase.Name}");

                Console.WriteLine(phase.Instruction);
                Console.WriteLine(
                    "Espere a ação terminar completamente e pressione ENTER.");

                Console.ReadLine();

                resolver.Invalidate();

                ResolvedPlayerPointers pointers =
                    resolver.Resolve();

                byte[] bytes =
                    connection.Memory.ReadBytes(
                        pointers.PlayerBase,
                        SnapshotSize);

                MemoryCapture capture = new(
                    phase,
                    DateTimeOffset.UtcNow,
                    pointers.PlayerBase,
                    bytes);

                captures.Add(capture);

                Console.WriteLine(
                    $"[PlayerBaseSnapshotDiagnostic] Captura | " +
                    $"fase={phase.Name} | " +
                    $"playerBase=0x{pointers.PlayerBase:X8} | " +
                    $"tamanho=0x{SnapshotSize:X}");
            }

            string outputDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "player-base-snapshot",
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            DiagnosticWriter.WriteAll(
                outputDirectory,
                captures);

            Console.WriteLine();
            Console.WriteLine(
                "[PlayerBaseSnapshotDiagnostic] RESULTADO | " +
                "status=SUCESSO");

            Console.WriteLine(
                $"[PlayerBaseSnapshotDiagnostic] Arquivos | " +
                $"diretorio={outputDirectory}");

            Console.WriteLine(
                "[PlayerBaseSnapshotDiagnostic] Consulte primeiro: " +
                "population-delta-candidates.csv");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[PlayerBaseSnapshotDiagnostic] ERRO_FATAL | " +
                $"tipo={ex.GetType().Name} | " +
                $"mensagem={Sanitize(ex.Message)}");

            return 1;
        }
    }

    private static string Sanitize(string value) =>
        value.Replace('\r', ' ')
             .Replace('\n', ' ')
             .Trim();
}
