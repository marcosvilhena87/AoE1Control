namespace AoE1Control;

/// <summary>
/// Strict-read exception that preserves the semantic availability reason.
/// </summary>
public sealed class PlayerStateUnavailableException : PlayerStateReadException
{
    public PlayerStateUnavailableException(
        PlayerStateAvailability availability,
        string message)
        : base(message)
    {
        Availability = availability;
    }

    public PlayerStateUnavailableException(
        PlayerStateAvailability availability,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Availability = availability;
    }

    public PlayerStateAvailability Availability { get; }
}
