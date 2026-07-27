namespace AoE1Control;

/// <summary>
/// Estado mínimo do jogador local.
/// </summary>
public sealed record PlayerSnapshot(
    int Id,
    ResourceSnapshot Resources);
