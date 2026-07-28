namespace AoE1Control;

/// <summary>
/// Population values for the local player.
/// </summary>
public sealed record PlayerPopulation
{
    /// <summary>Current population used by all population-consuming units.</summary>
    public required int Current { get; init; }

    /// <summary>
    /// Remaining population slots. This value can be negative in campaign
    /// scenarios that begin with units but no built population capacity.
    /// </summary>
    public required int Available { get; init; }

    /// <summary>Total capacity derived as Current + Available.</summary>
    public int Capacity => Current + Available;

    /// <summary>True when Current is greater than Capacity.</summary>
    public bool IsOverCapacity => Available < 0;

    /// <summary>True when no additional population slot is available.</summary>
    public bool IsAtOrOverCapacity => Available <= 0;
}
