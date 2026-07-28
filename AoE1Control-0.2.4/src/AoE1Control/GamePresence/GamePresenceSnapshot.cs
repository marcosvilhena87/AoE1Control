namespace AoE1Control;

/// <summary>
/// Minimal snapshot indicating whether the game is currently in a match.
/// </summary>
public sealed record GamePresenceSnapshot
{
    public required DateTimeOffset Timestamp { get; init; }

    public required GamePresenceState State { get; init; }

    public required PlayerStateAvailability PlayerStateAvailability { get; init; }

    public string? Message { get; init; }

    public PlayerStateSnapshot? PlayerState { get; init; }

    public bool IsInGame =>
        State == GamePresenceState.InGame &&
        PlayerState is not null;
}
