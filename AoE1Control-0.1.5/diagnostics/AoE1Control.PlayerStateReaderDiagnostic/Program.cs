using System.Globalization;
using System.Text;
using AoE1Control;
using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control.PlayerStateReaderDiagnostic;

internal static class Program
{
    private const uint PlayerStatePointerOffset = 0x0100;

    private const uint PopulationAvailableOffset = 0x0008;
    private const uint PopulationCurrentOffset = 0x0016;
    private const uint VillagerCountOffset = 0x004A;

    private const int PollIntervalMs = 500;

    private static int Main()
    {
        Console.Title =
            "AoE1Control 0.1.5 — PlayerStateReaderDiagnostic";

        Console.WriteLine(
            "[AoE1Control] Carregado | " +
            "versao=0.1.5 | " +
            "diagnostico=PlayerStateReaderDiagnostic");

        Console.WriteLine(
            "[PlayerStateReaderDiagnostic] Campos | " +
            "playerState=[PlayerBase+0x0100] | " +
            "available=UInt8(+0x0008) | " +
            "population=UInt8(+0x0016) | " +
            "villagers=UInt8(+0x004A)");

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
                $"[PlayerStateReaderDiagnostic] Processo conectado | " +
                $"perfil={connection.Profile.ProfileId}");

            Console.WriteLine(
                "[PlayerStateReaderDiagnostic] Monitoramento | " +
                $"intervaloMs={PollIntervalMs} | " +
                "encerrar=Ctrl+C");

            string outputDirectory =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "player-state-reader");

            Directory.CreateDirectory(outputDirectory);

            string csvPath =
                Path.Combine(
                    outputDirectory,
                    $"PlayerStateReaderDiagnostic-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

            using StreamWriter writer =
                new(
                    csvPath,
                    append: false,
                    new UTF8Encoding(false));

            writer.WriteLine(
                "timestampUtc,session,playerBase,playerState,food,wood,stone,gold,populationCurrent,populationAvailable,populationCapacity,villagerCount,nonVillagerCount");

            PlayerStateSample? previous = null;

            while (connection.IsConnected)
            {
                if (!session.IsSessionActive())
                {
                    Console.Write(
                        "\r[PlayerStateReaderDiagnostic] " +
                        "Aguardando partida ativa...                              ");

                    Thread.Sleep(PollIntervalMs);
                    continue;
                }

                try
                {
                    resolver.Invalidate();

                    ResolvedPlayerPointers pointers =
                        resolver.Resolve();

                    uint playerState =
                        connection.Memory.ReadPointer32(
                            checked(
                                pointers.PlayerBase +
                                PlayerStatePointerOffset));

                    byte populationAvailable =
                        connection.Memory.ReadAvailableBytes(
                            checked(
                                playerState +
                                PopulationAvailableOffset),
                            1)[0];

                    byte populationCurrent =
                        connection.Memory.ReadAvailableBytes(
                            checked(
                                playerState +
                                PopulationCurrentOffset),
                            1)[0];

                    byte villagerCount =
                        connection.Memory.ReadAvailableBytes(
                            checked(
                                playerState +
                                VillagerCountOffset),
                            1)[0];

                    int populationCapacity =
                        populationCurrent +
                        populationAvailable;

                    int nonVillagerCount =
                        Math.Max(
                            0,
                            populationCurrent -
                            villagerCount);

                    float food =
                        connection.Memory.ReadSingle(
                            checked(
                                pointers.ResourceBlock +
                                0x00));

                    float wood =
                        connection.Memory.ReadSingle(
                            checked(
                                pointers.ResourceBlock +
                                0x04));

                    float stone =
                        connection.Memory.ReadSingle(
                            checked(
                                pointers.ResourceBlock +
                                0x08));

                    float gold =
                        connection.Memory.ReadSingle(
                            checked(
                                pointers.ResourceBlock +
                                0x0C));

                    PlayerStateSample current =
                        new(
                            DateTimeOffset.UtcNow,
                            pointers.PlayerBase,
                            playerState,
                            food,
                            wood,
                            stone,
                            gold,
                            populationCurrent,
                            populationAvailable,
                            populationCapacity,
                            villagerCount,
                            nonVillagerCount);

                    Validate(current);

                    WriteSample(
                        writer,
                        current);

                    PrintState(
                        current);

                    if (previous is not null)
                    {
                        PrintChanges(
                            previous,
                            current);
                    }

                    previous = current;
                }
                catch (AoE1ControlException ex)
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        $"[PlayerStateReaderDiagnostic] Leitura ignorada | " +
                        $"tipo={ex.GetType().Name} | " +
                        $"mensagem={Sanitize(ex.Message)}");
                }

                Thread.Sleep(PollIntervalMs);
            }

            Console.WriteLine();
            Console.WriteLine(
                "[PlayerStateReaderDiagnostic] Processo encerrado.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"[PlayerStateReaderDiagnostic] ERRO_FATAL | " +
                $"tipo={ex.GetType().Name} | " +
                $"mensagem={Sanitize(ex.Message)}");

            Console.Error.WriteLine(
                "Pressione ENTER para encerrar.");

            Console.ReadLine();
            return 1;
        }
    }

    private static void PrintState(
        PlayerStateSample sample)
    {
        Console.Write(
            $"\r[PlayerStateReaderDiagnostic] Estado | " +
            $"pop={sample.PopulationCurrent}/{sample.PopulationCapacity} | " +
            $"available={sample.PopulationAvailable} | " +
            $"villagers={sample.VillagerCount} | " +
            $"nonVillagers={sample.NonVillagerCount} | " +
            $"food={sample.Food,7:0.##} | " +
            $"wood={sample.Wood,7:0.##} | " +
            $"stone={sample.Stone,7:0.##} | " +
            $"gold={sample.Gold,7:0.##}");
    }

    private static void PrintChanges(
        PlayerStateSample previous,
        PlayerStateSample current)
    {
        List<string> changes = [];

        AddChange(
            changes,
            "population",
            previous.PopulationCurrent,
            current.PopulationCurrent);

        AddChange(
            changes,
            "available",
            previous.PopulationAvailable,
            current.PopulationAvailable);

        AddChange(
            changes,
            "capacity",
            previous.PopulationCapacity,
            current.PopulationCapacity);

        AddChange(
            changes,
            "villagers",
            previous.VillagerCount,
            current.VillagerCount);

        AddChange(
            changes,
            "nonVillagers",
            previous.NonVillagerCount,
            current.NonVillagerCount);

        AddChange(
            changes,
            "food",
            previous.Food,
            current.Food);

        AddChange(
            changes,
            "wood",
            previous.Wood,
            current.Wood);

        AddChange(
            changes,
            "stone",
            previous.Stone,
            current.Stone);

        AddChange(
            changes,
            "gold",
            previous.Gold,
            current.Gold);

        if (changes.Count == 0)
            return;

        Console.WriteLine();
        Console.WriteLine(
            "[PlayerStateReaderDiagnostic] Mudanca | " +
            string.Join(" | ", changes));
    }

    private static void AddChange(
        List<string> changes,
        string name,
        int before,
        int after)
    {
        if (before == after)
            return;

        int delta = after - before;

        changes.Add(
            $"{name}={before}->{after} " +
            $"delta={FormatDelta(delta)}");
    }

    private static void AddChange(
        List<string> changes,
        string name,
        float before,
        float after)
    {
        if (Math.Abs(before - after) < 0.001f)
            return;

        float delta = after - before;

        changes.Add(
            $"{name}={before:0.##}->{after:0.##} " +
            $"delta={FormatDelta(delta)}");
    }

    private static void Validate(
        PlayerStateSample sample)
    {
        if (sample.PopulationCurrent > 250)
            throw new MemoryReadException(
                "População atual implausível.");

        if (sample.PopulationAvailable > 250)
            throw new MemoryReadException(
                "Vagas populacionais implausíveis.");

        if (sample.PopulationCapacity > 250)
            throw new MemoryReadException(
                "Capacidade populacional implausível.");

        if (sample.VillagerCount > sample.PopulationCurrent)
            throw new MemoryReadException(
                "Quantidade de aldeões maior que a população.");

        if (sample.NonVillagerCount < 0)
            throw new MemoryReadException(
                "Quantidade não aldeã negativa.");

        ValidateResource(
            "food",
            sample.Food);

        ValidateResource(
            "wood",
            sample.Wood);

        ValidateResource(
            "stone",
            sample.Stone);

        ValidateResource(
            "gold",
            sample.Gold);
    }

    private static void ValidateResource(
        string name,
        float value)
    {
        if (float.IsNaN(value) ||
            float.IsInfinity(value) ||
            value < 0 ||
            value > 1_000_000)
        {
            throw new MemoryReadException(
                $"Recurso inválido: {name}={value}");
        }
    }

    private static void WriteSample(
        StreamWriter writer,
        PlayerStateSample sample)
    {
        writer.Write(
            sample.Timestamp.UtcDateTime.ToString(
                "O",
                CultureInfo.InvariantCulture));

        writer.Write(",1,");
        writer.Write(
            $"0x{sample.PlayerBase:X8}");

        writer.Write(',');
        writer.Write(
            $"0x{sample.PlayerState:X8}");

        writer.Write(',');
        writer.Write(
            sample.Food.ToString(
                "R",
                CultureInfo.InvariantCulture));

        writer.Write(',');
        writer.Write(
            sample.Wood.ToString(
                "R",
                CultureInfo.InvariantCulture));

        writer.Write(',');
        writer.Write(
            sample.Stone.ToString(
                "R",
                CultureInfo.InvariantCulture));

        writer.Write(',');
        writer.Write(
            sample.Gold.ToString(
                "R",
                CultureInfo.InvariantCulture));

        writer.Write(',');
        writer.Write(
            sample.PopulationCurrent);

        writer.Write(',');
        writer.Write(
            sample.PopulationAvailable);

        writer.Write(',');
        writer.Write(
            sample.PopulationCapacity);

        writer.Write(',');
        writer.Write(
            sample.VillagerCount);

        writer.Write(',');
        writer.WriteLine(
            sample.NonVillagerCount);

        writer.Flush();
    }

    private static string FormatDelta(
        int value) =>
        value switch
        {
            > 0 => $"+{value}",
            < 0 => value.ToString(
                CultureInfo.InvariantCulture),
            _ => "0"
        };

    private static string FormatDelta(
        float value) =>
        value.ToString(
            "+0.##;-0.##;0",
            CultureInfo.InvariantCulture);

    private static string Sanitize(
        string value) =>
        value.Replace('\r', ' ')
             .Replace('\n', ' ')
             .Trim();

    private sealed record PlayerStateSample(
        DateTimeOffset Timestamp,
        uint PlayerBase,
        uint PlayerState,
        float Food,
        float Wood,
        float Stone,
        float Gold,
        byte PopulationCurrent,
        byte PopulationAvailable,
        int PopulationCapacity,
        byte VillagerCount,
        int NonVillagerCount);
}
