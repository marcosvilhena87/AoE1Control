namespace AoE1Control;

/// <summary>
/// High-level state of the Age of Empires session.
/// </summary>
public enum GameSessionState
{
    Unknown = 0,
    Disconnected,
    Menu,
    Loading,
    InGame
}
