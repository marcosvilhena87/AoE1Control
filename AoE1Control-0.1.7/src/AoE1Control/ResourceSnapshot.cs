namespace AoE1Control;

/// <summary>
/// Recursos do jogador local em um instante específico.
/// </summary>
public sealed record ResourceSnapshot(
    float Food,
    float Wood,
    float Stone,
    float Gold);
