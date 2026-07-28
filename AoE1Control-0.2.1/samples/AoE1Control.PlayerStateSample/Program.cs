using AoE1Control;

Console.Title =
    "AoE1Control 0.2.0 — PlayerStateApi Sample";

Console.WriteLine(
    "[AoE1Control] Carregado | versao=0.2.0 | api=PlayerStateApi");

try
{
    using PlayerStateApi api =
        PlayerStateApi.Connect();

    Console.WriteLine(
        $"[PlayerStateApi] Conectado | perfil={api.ProfileId}");

    Console.WriteLine(
        "[PlayerStateApi] Monitoramento | intervaloMs=500 | encerrar=Ctrl+C");

    while (api.IsConnected)
    {
        try
        {
            PlayerStateSnapshot state =
                api.Read();

            string economicShips =
                state.Units.EconomicShips?.ToString() ??
                $"{state.Units.EconomicShipsA}/{state.Units.EconomicShipsB}";

            Console.Write(
                $"\r[PlayerStateApi] Estado | " +
                $"pop={state.Population.Current}/{state.Population.Capacity} | " +
                $"available={state.Population.Available} | " +
                $"villagers={state.Units.Villagers} | " +
                $"military={state.Units.MilitaryPopulation} | " +
                $"lightTransport={state.Units.LightTransports} | " +
                $"economicShips={economicShips} | " +
                $"food={state.Resources.Food,7:0.##} | " +
                $"wood={state.Resources.Wood,7:0.##} | " +
                $"stone={state.Resources.Stone,7:0.##} | " +
                $"gold={state.Resources.Gold,7:0.##}");
        }
        catch (PlayerStateReadException ex)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"[PlayerStateApi] Snapshot ignorado | mensagem={ex.Message}");
        }

        Thread.Sleep(500);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        $"[PlayerStateApi] ERRO_FATAL | " +
        $"tipo={ex.GetType().Name} | " +
        $"mensagem={ex.Message.Replace('\r', ' ').Replace('\n', ' ')}");

    Console.Error.WriteLine(
        "Pressione ENTER para encerrar.");

    Console.ReadLine();
    return 1;
}

return 0;
