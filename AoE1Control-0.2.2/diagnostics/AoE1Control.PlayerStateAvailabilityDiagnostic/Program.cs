using AoE1Control;

Console.Title =
    "AoE1Control 0.2.2 — PlayerStateAvailabilityApi";

Console.WriteLine(
    "[AoE1Control] Carregado | versao=0.2.2 | api=PlayerStateAvailabilityApi");

Console.WriteLine(
    "[PlayerStateAvailabilityApi] Monitoramento | intervaloMs=500 | encerrar=Ctrl+C");

bool stopping = false;

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stopping = true;
};

using PlayerStateApi api =
    PlayerStateApi.Connect();

Console.WriteLine(
    $"[PlayerStateAvailabilityApi] Conectado | perfil={api.ProfileId}");

PlayerStateAvailability? previousAvailability =
    null;

PlayerStateSnapshot? previousSnapshot =
    null;

while (!stopping && api.IsConnected)
{
    PlayerStateReadResult result =
        api.TryRead();

    if (result.Availability != previousAvailability)
    {
        Console.WriteLine(
            $"[PlayerStateAvailabilityApi] DISPONIBILIDADE | " +
            $"estado={result.Availability} | " +
            $"mensagem={result.Message ?? "-"}");

        previousAvailability =
            result.Availability;
    }

    if (result.IsAvailable)
    {
        PlayerStateSnapshot state =
            result.Snapshot!;

        if (previousSnapshot is null ||
            previousSnapshot.Population != state.Population ||
            previousSnapshot.Units != state.Units ||
            previousSnapshot.Resources != state.Resources ||
            previousSnapshot.Addresses != state.Addresses)
        {
            Console.WriteLine(
                $"[PlayerStateAvailabilityApi] ESTADO | " +
                $"pop={state.Population.Current}/{state.Population.Capacity} | " +
                $"available={state.Population.Available} | " +
                $"villagers={state.Units.Villagers} | " +
                $"military={state.Units.MilitaryPopulation} | " +
                $"lightTransport={state.Units.LightTransports} | " +
                $"economicShips={state.Units.EconomicShips?.ToString() ?? $"{state.Units.EconomicShipsA}/{state.Units.EconomicShipsB}"} | " +
                $"food={state.Resources.Food:0.##} | " +
                $"wood={state.Resources.Wood:0.##} | " +
                $"stone={state.Resources.Stone:0.##} | " +
                $"gold={state.Resources.Gold:0.##}");
        }

        previousSnapshot =
            state;
    }

    Thread.Sleep(500);
}

Console.WriteLine(
    "[PlayerStateAvailabilityApi] Encerrado.");

return 0;
