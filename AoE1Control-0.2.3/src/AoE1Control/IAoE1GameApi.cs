namespace AoE1Control;

/// <summary>
/// API somente leitura para o Age of Empires.
/// </summary>
public interface IAoE1GameApi : IDisposable
{
    /// <summary>Indica se o processo continua conectado.</summary>
    bool IsConnected { get; }

    /// <summary>Indica se uma sessão de jogo está ativa.</summary>
    bool IsGameRunning { get; }

    /// <summary>Informações da versão validada do jogo.</summary>
    GameVersionInfo GameVersion { get; }

    /// <summary>Obtém um snapshot consistente do estado exposto pela API.</summary>
    GameSnapshot GetSnapshot();

    /// <summary>Obtém o jogador local.</summary>
    PlayerSnapshot GetLocalPlayer();

    /// <summary>Obtém os recursos do jogador local.</summary>
    ResourceSnapshot GetResources();

    /// <summary>Tenta obter um snapshot sem propagar falhas esperadas de sessão ou leitura.</summary>
    bool TryGetSnapshot(out GameSnapshot? snapshot);

    /// <summary>Atualiza o processo e invalida os ponteiros resolvidos em cache.</summary>
    void Refresh();
}
