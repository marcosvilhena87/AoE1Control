namespace AoE1Control;

/// <summary>
/// Raised when a consistent player-state snapshot cannot be produced.
/// </summary>
public class PlayerStateReadException : AoE1ControlException
{
    public PlayerStateReadException(string message)
        : base(message)
    {
    }

    public PlayerStateReadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
