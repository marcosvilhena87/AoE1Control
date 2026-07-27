namespace AoE1Control;

/// <summary>
/// Identificação da versão validada do jogo.
/// </summary>
public sealed record GameVersionInfo(
    string ProfileId,
    string Game,
    string Edition,
    string ExecutableName,
    string Sha256);
