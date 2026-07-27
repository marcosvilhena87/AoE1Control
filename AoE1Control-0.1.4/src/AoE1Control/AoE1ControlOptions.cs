namespace AoE1Control;

/// <summary>
/// Configurações da conexão com o jogo.
/// </summary>
public sealed class AoE1ControlOptions
{
    /// <summary>Nome do processo sem extensão.</summary>
    public string ProcessName { get; init; } = "EMPIRES";

    /// <summary>Diretório alternativo para os perfis JSON.</summary>
    public string? ProfilesDirectory { get; init; }

    /// <summary>Exige que o SHA-256 esteja cadastrado em um perfil.</summary>
    public bool RequireValidatedProfile { get; init; } = true;

    /// <summary>Permite resolver novamente a cadeia quando o cache ficar inválido.</summary>
    public bool AutoResolvePointers { get; init; } = true;
}
