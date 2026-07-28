using System.Globalization;
using System.Text;
using AoE1Control;

Console.Title =
    "AoE1Control 0.2.4 — GamePresenceApi";

Console.WriteLine(
    "[AoE1Control] Carregado | versao=0.2.4 | api=GamePresenceApi");

Console.WriteLine(
    "[GamePresenceApi] Monitoramento | intervaloMs=500 | encerrar=Ctrl+C");

bool stopping = false;

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stopping = true;
};

string outputDirectory =
    Path.Combine(
        AppContext.BaseDirectory,
        "game-presence",
        DateTime.Now.ToString("yyyyMMdd-HHmmss"));

Directory.CreateDirectory(outputDirectory);

string eventsPath =
    Path.Combine(outputDirectory, "presence-events.csv");

using StreamWriter writer =
    new(
        eventsPath,
        append: false,
        new UTF8Encoding(false));

writer.AutoFlush = true;

writer.WriteLine(
    "timestampUtc,previousState,currentState,playerStateAvailability,durationPreviousMilliseconds,message");

using GamePresenceApi api =
    GamePresenceApi.Connect();

Console.WriteLine(
    $"[GamePresenceApi] Conectado | perfil={api.ProfileId}");

Console.WriteLine(
    $"[GamePresenceApi] Arquivos | diretorio={outputDirectory}");

GamePresenceState? previousState =
    null;

DateTimeOffset previousStateStartedAt =
    DateTimeOffset.UtcNow;

while (!stopping && api.IsConnected)
{
    GamePresenceSnapshot presence =
        api.Read();

    if (presence.State != previousState)
    {
        DateTimeOffset now =
            presence.Timestamp;

        double durationPreviousMilliseconds =
            previousState is null
                ? 0
                : (now - previousStateStartedAt).TotalMilliseconds;

        Console.WriteLine(
            $"[GamePresenceApi] ESTADO | " +
            $"anterior={previousState?.ToString() ?? "-"} | " +
            $"atual={presence.State} | " +
            $"playerState={presence.PlayerStateAvailability} | " +
            $"duracaoAnteriorMs={durationPreviousMilliseconds:0}");

        writer.Write(now.UtcDateTime.ToString(
            "O",
            CultureInfo.InvariantCulture));
        writer.Write(',');
        writer.Write(Csv(previousState?.ToString() ?? ""));
        writer.Write(',');
        writer.Write(Csv(presence.State.ToString()));
        writer.Write(',');
        writer.Write(Csv(presence.PlayerStateAvailability.ToString()));
        writer.Write(',');
        writer.Write(durationPreviousMilliseconds.ToString(
            "0",
            CultureInfo.InvariantCulture));
        writer.Write(',');
        writer.WriteLine(Csv(presence.Message ?? ""));

        previousState =
            presence.State;

        previousStateStartedAt =
            now;
    }

    Thread.Sleep(500);
}

Console.WriteLine(
    "[GamePresenceApi] Encerrado.");

return 0;

static string Csv(
    string value) =>
    "\"" +
    value.Replace("\"", "\"\"") +
    "\"";
