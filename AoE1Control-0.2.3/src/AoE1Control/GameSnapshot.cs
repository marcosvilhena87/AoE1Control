namespace AoE1Control;

/// <summary>
/// Snapshot imutável da parte do estado exposta pelo AoE1Control 0.1.0.
/// </summary>
public sealed record GameSnapshot(
    DateTimeOffset Timestamp,
    bool IsGameRunning,
    PlayerSnapshot LocalPlayer);
