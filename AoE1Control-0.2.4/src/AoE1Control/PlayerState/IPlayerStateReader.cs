namespace AoE1Control;

/// <summary>
/// Reads the local player's state.
/// </summary>
public interface IPlayerStateReader
{
    /// <summary>
    /// Strict read. Throws when the state is not available.
    /// </summary>
    PlayerStateSnapshot Read();

    /// <summary>
    /// Non-throwing read for normal scenario/menu transitions.
    /// </summary>
    PlayerStateReadResult TryRead();
}
