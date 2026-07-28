using AoE1Control;

Console.Title = "AoE1Control 0.1.0 — ReadOnlyGameApi";

Console.WriteLine(
    "[AoE1Control] Carregado | versao=0.1.0 | api=ReadOnlyGameApi");

try
{
    using IAoE1GameApi game = AoE1GameApi.Connect();

    Console.WriteLine(
        $"[ReadOnlyGameApi] Conectado | " +
        $"perfil={game.GameVersion.ProfileId} | " +
        $"jogo={game.GameVersion.Game} | " +
        $"edicao={game.GameVersion.Edition}");

    Console.WriteLine(
        "[ReadOnlyGameApi] Monitoramento | " +
        "intervaloMs=500 | encerrar=Ctrl+C");

    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = false;
    };

    while (game.IsConnected)
    {
        if (!game.TryGetSnapshot(out GameSnapshot? snapshot))
        {
            Console.Write(
                "\r[ReadOnlyGameApi] Aguardando partida ativa...          ");

            Thread.Sleep(500);
            continue;
        }

        ResourceSnapshot resources =
            snapshot!.LocalPlayer.Resources;

        Console.Write(
            $"\r[ReadOnlyGameApi] Recursos | " +
            $"food={resources.Food,8:0.##} | " +
            $"wood={resources.Wood,8:0.##} | " +
            $"stone={resources.Stone,8:0.##} | " +
            $"gold={resources.Gold,8:0.##}");

        Thread.Sleep(500);
    }

    Console.WriteLine();
    Console.WriteLine(
        "[ReadOnlyGameApi] Processo encerrado.");
}
catch (GameProcessNotFoundException ex)
{
    Console.Error.WriteLine(
        $"[ReadOnlyGameApi] PROCESSO_NAO_ENCONTRADO | {ex.Message}");
}
catch (UnsupportedGameVersionException ex)
{
    Console.Error.WriteLine(
        $"[ReadOnlyGameApi] VERSAO_NAO_SUPORTADA | {ex.Message}");
}
catch (AoE1ControlException ex)
{
    Console.Error.WriteLine(
        $"[ReadOnlyGameApi] FALHA | " +
        $"tipo={ex.GetType().Name} | mensagem={ex.Message}");
}
catch (Exception ex)
{
    Console.Error.WriteLine(
        $"[ReadOnlyGameApi] ERRO_FATAL | " +
        $"tipo={ex.GetType().Name} | mensagem={ex.Message}");
}
