namespace AoE1Control;

/// <summary>
/// Immutable read-only snapshot of the local player's validated state.
/// </summary>
public sealed record PlayerStateSnapshot
{
    public required DateTimeOffset Timestamp { get; init; }

    public required PlayerResources Resources { get; init; }

    public required PlayerPopulation Population { get; init; }

    public required PlayerUnitCounters Units { get; init; }

    public required PlayerStateAddresses Addresses { get; init; }
}
