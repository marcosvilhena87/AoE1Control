namespace AoE1Control;

/// <summary>O processo do jogo não foi encontrado.</summary>
public sealed class GameProcessNotFoundException : AoE1ControlException
{
    public GameProcessNotFoundException(string message) : base(message) { }

    public GameProcessNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}
