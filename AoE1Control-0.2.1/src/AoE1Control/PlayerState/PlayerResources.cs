namespace AoE1Control;

/// <summary>
/// Resource values read from the local player's resource block.
/// </summary>
public sealed record PlayerResources(
    float Food,
    float Wood,
    float Stone,
    float Gold);
