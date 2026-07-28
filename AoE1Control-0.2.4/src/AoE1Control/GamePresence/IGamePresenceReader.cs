namespace AoE1Control;

/// <summary>
/// Reads whether the game is currently in a playable match.
/// </summary>
public interface IGamePresenceReader
{
    GamePresenceSnapshot Read();
}
