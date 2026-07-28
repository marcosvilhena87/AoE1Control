namespace AoE1Control;

/// <summary>
/// Reads the high-level game session state.
/// </summary>
public interface IGameSessionStateReader
{
    GameSessionSnapshot Read();
}
