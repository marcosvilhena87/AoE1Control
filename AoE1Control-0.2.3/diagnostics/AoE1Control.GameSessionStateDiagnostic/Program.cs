using AoE1Control;

Console.Title =
    "AoE1Control 0.2.3 — GameSessionStateApi";

Console.WriteLine(
    "[AoE1Control] Carregado | versao=0.2.3 | api=GameSessionStateApi");

Console.WriteLine(
    "[GameSessionStateApi] Monitoramento | intervaloMs=500 | encerrar=Ctrl+C");

bool stopping = false;

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stopping = true;
};

using GameSessionStateApi api =
    GameSessionStateApi.Connect();

Console.WriteLine(
    $"[GameSessionStateApi] Conectado | perfil={api.ProfileId}");

GameSessionState? previousState =
    null;

PlayerStateAvailability? previousAvailability =
    null;

while (!stopping && api.IsConnected)
{
    GameSessionSnapshot session =
        api.Read();

    bool stateChanged =
        session.State != previousState;

    bool availabilityChanged =
        session.PlayerStateAvailability != previousAvailability;

    if (stateChanged || availabilityChanged)
    {
        Console.WriteLine(
            $"[GameSessionStateApi] SESSAO | " +
            $"estado={session.State} | " +
            $"playerState={session.PlayerStateAvailability} | " +
            $"mensagem={session.Message ?? "-"}");

        previousState =
            session.State;

        previousAvailability =
            session.PlayerStateAvailability;
    }

    if (session.IsInGame &&
        stateChanged)
    {
        PlayerStateSnapshot state =
            session.PlayerState!;

        Console.WriteLine(
            $"[GameSessionStateApi] PARTIDA | " +
            $"pop={state.Population.Current}/{state.Population.Capacity} | " +
            $"villagers={state.Units.Villagers} | " +
            $"military={state.Units.MilitaryPopulation} | " +
            $"food={state.Resources.Food:0.##} | " +
            $"wood={state.Resources.Wood:0.##} | " +
            $"stone={state.Resources.Stone:0.##} | " +
            $"gold={state.Resources.Gold:0.##}");
    }

    Thread.Sleep(500);
}

Console.WriteLine(
    "[GameSessionStateApi] Encerrado.");

return 0;
