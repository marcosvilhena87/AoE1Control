namespace AoE1Control;

/// <summary>
/// Resolved addresses used for a snapshot. Useful for diagnostics only.
/// </summary>
public sealed record PlayerStateAddresses(
    uint PlayerContainer,
    uint PlayerBase,
    uint PlayerState,
    uint ResourceBlock);
