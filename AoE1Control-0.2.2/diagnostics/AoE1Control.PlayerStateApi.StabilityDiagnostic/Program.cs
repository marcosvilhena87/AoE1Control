using System.Diagnostics;
using System.Globalization;
using System.Text;
using AoE1Control;

Console.Title =
    "AoE1Control 0.2.1 — PlayerStateApi StabilityDiagnostic";

Console.WriteLine(
    "[AoE1Control] Carregado | versao=0.2.1 | " +
    "diagnostico=PlayerStateApiStabilityDiagnostic");

Console.WriteLine(
    "[StabilityDiagnostic] Configuracao | intervaloMs=500 | " +
    "csvSomenteMudancas=true | encerrar=Ctrl+C");

string outputDirectory =
    Path.Combine(
        AppContext.BaseDirectory,
        "player-state-stability",
        DateTime.Now.ToString("yyyyMMdd-HHmmss"));

Directory.CreateDirectory(outputDirectory);

string eventsCsvPath =
    Path.Combine(outputDirectory, "events.csv");

string changesCsvPath =
    Path.Combine(outputDirectory, "state-changes.csv");

string summaryPath =
    Path.Combine(outputDirectory, "summary.txt");

using StreamWriter eventsWriter =
    CreateCsvWriter(
        eventsCsvPath,
        "timestampUtc,event,sequence,consecutiveFailures,recoveryMilliseconds,exceptionType,message,innerType,innerMessage");

using StreamWriter changesWriter =
    CreateCsvWriter(
        changesCsvPath,
        "timestampUtc,sequence,populationCurrent,populationAvailable,populationCapacity,villagers,militaryPopulation,lightTransports,economicShipsA,economicShipsB,economicShips,food,wood,stone,gold,playerContainer,playerBase,playerState,resourceBlock");

long attempts = 0;
long validSnapshots = 0;
long discardedSnapshots = 0;
long stateChanges = 0;
long recoveries = 0;
long longestFailureRun = 0;
long currentFailureRun = 0;

TimeSpan totalRecoveryDuration =
    TimeSpan.Zero;

TimeSpan longestRecoveryDuration =
    TimeSpan.Zero;

DateTimeOffset startedAt =
    DateTimeOffset.UtcNow;

DateTimeOffset? failureRunStartedAt =
    null;

PlayerStateSnapshot? previousSnapshot =
    null;

bool stopping =
    false;

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stopping = true;
};

try
{
    using PlayerStateApi api =
        PlayerStateApi.Connect();

    Console.WriteLine(
        $"[StabilityDiagnostic] Conectado | perfil={api.ProfileId}");

    Console.WriteLine(
        $"[StabilityDiagnostic] Arquivos | diretorio={outputDirectory}");

    Stopwatch uptime =
        Stopwatch.StartNew();

    while (!stopping && api.IsConnected)
    {
        attempts++;

        try
        {
            PlayerStateSnapshot snapshot =
                api.Read();

            validSnapshots++;

            if (currentFailureRun > 0)
            {
                TimeSpan recoveryDuration =
                    DateTimeOffset.UtcNow -
                    failureRunStartedAt!.Value;

                recoveries++;
                totalRecoveryDuration += recoveryDuration;

                if (recoveryDuration > longestRecoveryDuration)
                    longestRecoveryDuration = recoveryDuration;

                WriteEvent(
                    eventsWriter,
                    snapshot.Timestamp,
                    "RECOVERED",
                    attempts,
                    currentFailureRun,
                    recoveryDuration,
                    null);

                Console.WriteLine(
                    $"[StabilityDiagnostic] RECUPERADO | " +
                    $"falhasConsecutivas={currentFailureRun} | " +
                    $"tempoMs={recoveryDuration.TotalMilliseconds:0}");

                currentFailureRun = 0;
                failureRunStartedAt = null;
            }

            bool changed =
                previousSnapshot is null ||
                HasMeaningfulChange(previousSnapshot, snapshot);

            if (changed)
            {
                stateChanges++;

                WriteStateChange(
                    changesWriter,
                    attempts,
                    snapshot);

                Console.WriteLine(
                    $"[StabilityDiagnostic] ESTADO | " +
                    $"seq={attempts} | " +
                    $"pop={snapshot.Population.Current}/{snapshot.Population.Capacity} | " +
                    $"available={snapshot.Population.Available} | " +
                    $"villagers={snapshot.Units.Villagers} | " +
                    $"military={snapshot.Units.MilitaryPopulation} | " +
                    $"lightTransport={snapshot.Units.LightTransports} | " +
                    $"economicShips={FormatEconomicShips(snapshot)} | " +
                    $"food={snapshot.Resources.Food:0.##} | " +
                    $"wood={snapshot.Resources.Wood:0.##} | " +
                    $"stone={snapshot.Resources.Stone:0.##} | " +
                    $"gold={snapshot.Resources.Gold:0.##}");
            }

            previousSnapshot =
                snapshot;
        }
        catch (PlayerStateReadException ex)
        {
            discardedSnapshots++;
            currentFailureRun++;

            if (currentFailureRun > longestFailureRun)
                longestFailureRun = currentFailureRun;

            failureRunStartedAt ??=
                DateTimeOffset.UtcNow;

            WriteEvent(
                eventsWriter,
                DateTimeOffset.UtcNow,
                currentFailureRun == 1
                    ? "UNSTABLE_STARTED"
                    : "SNAPSHOT_DISCARDED",
                attempts,
                currentFailureRun,
                null,
                ex);

            Console.WriteLine(
                $"[StabilityDiagnostic] DESCARTADO | " +
                $"seq={attempts} | " +
                $"consecutivas={currentFailureRun} | " +
                $"tipo={ex.GetType().Name} | " +
                $"causa={FlattenException(ex)}");
        }

        Thread.Sleep(500);
    }

    uptime.Stop();

    WriteSummary(
        summaryPath,
        startedAt,
        DateTimeOffset.UtcNow,
        uptime.Elapsed,
        attempts,
        validSnapshots,
        discardedSnapshots,
        stateChanges,
        recoveries,
        longestFailureRun,
        totalRecoveryDuration,
        longestRecoveryDuration,
        api.IsConnected,
        stopping);

    Console.WriteLine();
    Console.WriteLine(
        "[StabilityDiagnostic] RESULTADO | status=CONCLUIDO");

    Console.WriteLine(
        $"[StabilityDiagnostic] Resumo | " +
        $"tentativas={attempts} | " +
        $"validos={validSnapshots} | " +
        $"descartados={discardedSnapshots} | " +
        $"mudancas={stateChanges} | " +
        $"recuperacoes={recoveries} | " +
        $"maiorSequenciaFalhas={longestFailureRun}");

    Console.WriteLine(
        $"[StabilityDiagnostic] Arquivos | diretorio={outputDirectory}");

    return 0;
}
catch (Exception ex)
{
    WriteEvent(
        eventsWriter,
        DateTimeOffset.UtcNow,
        "FATAL",
        attempts,
        currentFailureRun,
        null,
        ex);

    Console.Error.WriteLine();
    Console.Error.WriteLine(
        $"[StabilityDiagnostic] ERRO_FATAL | " +
        $"tipo={ex.GetType().Name} | " +
        $"causa={FlattenException(ex)}");

    Console.Error.WriteLine(
        $"[StabilityDiagnostic] Arquivos | diretorio={outputDirectory}");

    Console.Error.WriteLine(
        "Pressione ENTER para encerrar.");

    Console.ReadLine();
    return 1;
}

static StreamWriter CreateCsvWriter(
    string path,
    string header)
{
    StreamWriter writer =
        new(
            path,
            append: false,
            new UTF8Encoding(false));

    writer.AutoFlush = true;
    writer.WriteLine(header);
    return writer;
}

static void WriteEvent(
    StreamWriter writer,
    DateTimeOffset timestamp,
    string eventName,
    long sequence,
    long consecutiveFailures,
    TimeSpan? recoveryDuration,
    Exception? exception)
{
    writer.Write(timestamp.UtcDateTime.ToString(
        "O",
        CultureInfo.InvariantCulture));
    writer.Write(',');
    writer.Write(Csv(eventName));
    writer.Write(',');
    writer.Write(sequence);
    writer.Write(',');
    writer.Write(consecutiveFailures);
    writer.Write(',');
    writer.Write(
        recoveryDuration?.TotalMilliseconds.ToString(
            "0",
            CultureInfo.InvariantCulture) ??
        "");
    writer.Write(',');
    writer.Write(Csv(exception?.GetType().Name ?? ""));
    writer.Write(',');
    writer.Write(Csv(exception?.Message ?? ""));
    writer.Write(',');
    writer.Write(Csv(exception?.InnerException?.GetType().Name ?? ""));
    writer.Write(',');
    writer.WriteLine(Csv(exception?.InnerException?.Message ?? ""));
}

static void WriteStateChange(
    StreamWriter writer,
    long sequence,
    PlayerStateSnapshot state)
{
    writer.Write(state.Timestamp.UtcDateTime.ToString(
        "O",
        CultureInfo.InvariantCulture));
    writer.Write(',');
    writer.Write(sequence);
    writer.Write(',');
    writer.Write(state.Population.Current);
    writer.Write(',');
    writer.Write(state.Population.Available);
    writer.Write(',');
    writer.Write(state.Population.Capacity);
    writer.Write(',');
    writer.Write(state.Units.Villagers);
    writer.Write(',');
    writer.Write(state.Units.MilitaryPopulation);
    writer.Write(',');
    writer.Write(state.Units.LightTransports);
    writer.Write(',');
    writer.Write(state.Units.EconomicShipsA);
    writer.Write(',');
    writer.Write(state.Units.EconomicShipsB);
    writer.Write(',');
    writer.Write(state.Units.EconomicShips?.ToString(
        CultureInfo.InvariantCulture) ?? "");
    writer.Write(',');
    writer.Write(state.Resources.Food.ToString(
        "0.###",
        CultureInfo.InvariantCulture));
    writer.Write(',');
    writer.Write(state.Resources.Wood.ToString(
        "0.###",
        CultureInfo.InvariantCulture));
    writer.Write(',');
    writer.Write(state.Resources.Stone.ToString(
        "0.###",
        CultureInfo.InvariantCulture));
    writer.Write(',');
    writer.Write(state.Resources.Gold.ToString(
        "0.###",
        CultureInfo.InvariantCulture));
    writer.Write(',');
    writer.Write($"0x{state.Addresses.PlayerContainer:X8}");
    writer.Write(',');
    writer.Write($"0x{state.Addresses.PlayerBase:X8}");
    writer.Write(',');
    writer.Write($"0x{state.Addresses.PlayerState:X8}");
    writer.Write(',');
    writer.WriteLine($"0x{state.Addresses.ResourceBlock:X8}");
}

static bool HasMeaningfulChange(
    PlayerStateSnapshot previous,
    PlayerStateSnapshot current)
{
    return
        previous.Population != current.Population ||
        previous.Units != current.Units ||
        previous.Resources != current.Resources ||
        previous.Addresses != current.Addresses;
}

static string FormatEconomicShips(
    PlayerStateSnapshot state) =>
    state.Units.EconomicShips?.ToString(
        CultureInfo.InvariantCulture) ??
    $"{state.Units.EconomicShipsA}/{state.Units.EconomicShipsB}";

static string FlattenException(
    Exception exception)
{
    List<string> parts = [];

    Exception? current =
        exception;

    while (current is not null)
    {
        parts.Add(
            $"{current.GetType().Name}: " +
            Sanitize(current.Message));

        current =
            current.InnerException;
    }

    return string.Join(" -> ", parts);
}

static string Csv(
    string value) =>
    "\"" +
    value.Replace("\"", "\"\"") +
    "\"";

static string Sanitize(
    string value) =>
    value.Replace('\r', ' ')
         .Replace('\n', ' ')
         .Trim();

static void WriteSummary(
    string path,
    DateTimeOffset startedAt,
    DateTimeOffset endedAt,
    TimeSpan duration,
    long attempts,
    long validSnapshots,
    long discardedSnapshots,
    long stateChanges,
    long recoveries,
    long longestFailureRun,
    TimeSpan totalRecoveryDuration,
    TimeSpan longestRecoveryDuration,
    bool connectionStillActive,
    bool stoppedByUser)
{
    double successRate =
        attempts == 0
            ? 0
            : validSnapshots * 100.0 / attempts;

    double averageRecoveryMs =
        recoveries == 0
            ? 0
            : totalRecoveryDuration.TotalMilliseconds / recoveries;

    StringBuilder text =
        new();

    text.AppendLine(
        "AoE1Control 0.2.1 — PlayerStateApi StabilityDiagnostic");

    text.AppendLine();
    text.AppendLine($"Started UTC: {startedAt:O}");
    text.AppendLine($"Ended UTC: {endedAt:O}");
    text.AppendLine($"Duration: {duration}");
    text.AppendLine($"Stopped by user: {stoppedByUser}");
    text.AppendLine($"Connection still active: {connectionStillActive}");

    text.AppendLine();
    text.AppendLine($"Read attempts: {attempts}");
    text.AppendLine($"Valid snapshots: {validSnapshots}");
    text.AppendLine($"Discarded snapshots: {discardedSnapshots}");
    text.AppendLine($"Success rate: {successRate:0.000}%");
    text.AppendLine($"State changes written: {stateChanges}");

    text.AppendLine();
    text.AppendLine($"Recoveries: {recoveries}");
    text.AppendLine($"Longest failure run: {longestFailureRun}");
    text.AppendLine($"Average recovery milliseconds: {averageRecoveryMs:0}");
    text.AppendLine(
        $"Longest recovery milliseconds: " +
        $"{longestRecoveryDuration.TotalMilliseconds:0}");

    File.WriteAllText(
        path,
        text.ToString(),
        new UTF8Encoding(false));
}
