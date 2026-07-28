namespace AoE1Control;

/// <summary>
/// Validated and experimental unit-category counters.
/// </summary>
public sealed record PlayerUnitCounters
{
    /// <summary>Validated villager count.</summary>
    public required int Villagers { get; init; }

    /// <summary>
    /// Validated military population. Confirmed to include at least Clubman
    /// and Scout Ship.
    /// </summary>
    public required int MilitaryPopulation { get; init; }

    /// <summary>Validated Light Transport count.</summary>
    public required int LightTransports { get; init; }

    /// <summary>
    /// Experimental counter at PlayerState + 0x004A.
    /// It increased with Trade Boat and Fishing Boat in the validated scenario.
    /// </summary>
    public required int EconomicShipsA { get; init; }

    /// <summary>
    /// Experimental counter at PlayerState + 0x0066.
    /// It increased with Trade Boat and Fishing Boat in the validated scenario.
    /// </summary>
    public required int EconomicShipsB { get; init; }

    /// <summary>
    /// Convenience value. Returns the shared economic-ship value when both
    /// experimental counters agree; otherwise returns null.
    /// </summary>
    public int? EconomicShips =>
        EconomicShipsA == EconomicShipsB
            ? EconomicShipsA
            : null;
}
