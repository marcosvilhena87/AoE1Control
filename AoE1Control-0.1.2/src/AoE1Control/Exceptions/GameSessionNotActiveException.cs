namespace AoE1Control;

/// <summary>Nenhuma sessão de jogo está ativa.</summary>
public sealed class GameSessionNotActiveException : AoE1ControlException
{
    public GameSessionNotActiveException(string message) : base(message) { }

    public GameSessionNotActiveException(string message, Exception innerException)
        : base(message, innerException) { }
}
