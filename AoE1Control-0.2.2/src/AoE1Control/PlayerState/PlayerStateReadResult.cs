namespace AoE1Control;

/// <summary>
/// Non-throwing result returned by <see cref="PlayerStateApi.TryRead"/>.
/// </summary>
public sealed record PlayerStateReadResult
{
    public required PlayerStateAvailability Availability { get; init; }

    public PlayerStateSnapshot? Snapshot { get; init; }

    public string? Message { get; init; }

    public Exception? Exception { get; init; }

    public bool IsAvailable =>
        Availability == PlayerStateAvailability.Available &&
        Snapshot is not null;

    public static PlayerStateReadResult Available(
        PlayerStateSnapshot snapshot) =>
        new()
        {
            Availability =
                PlayerStateAvailability.Available,

            Snapshot =
                snapshot
        };

    public static PlayerStateReadResult Unavailable(
        PlayerStateAvailability availability,
        string message,
        Exception? exception = null) =>
        new()
        {
            Availability =
                availability,

            Message =
                message,

            Exception =
                exception
        };
}
