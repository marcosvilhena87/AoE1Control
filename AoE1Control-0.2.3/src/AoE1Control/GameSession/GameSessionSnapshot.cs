namespace AoE1Control;

/// <summary>
/// High-level session snapshot derived from PlayerState availability.
/// </summary>
public sealed record GameSessionSnapshot
{
    public required DateTimeOffset Timestamp { get; init; }

    public required GameSessionState State { get; init; }

    public required PlayerStateAvailability PlayerStateAvailability { get; init; }

    public string? Message { get; init; }

    public PlayerStateSnapshot? PlayerState { get; init; }

    public bool IsInGame =>
        State == GameSessionState.InGame &&
        PlayerState is not null;
}
