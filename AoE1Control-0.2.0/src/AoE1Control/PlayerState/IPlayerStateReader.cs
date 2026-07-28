namespace AoE1Control;

/// <summary>
/// Reads a consistent read-only snapshot of the local player.
/// </summary>
public interface IPlayerStateReader
{
    PlayerStateSnapshot Read();
}
