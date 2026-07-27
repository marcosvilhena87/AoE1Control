namespace AoE1Control;

/// <summary>O processo do jogo foi encerrado.</summary>
public sealed class GameProcessExitedException : AoE1ControlException
{
    public GameProcessExitedException(string message) : base(message) { }

    public GameProcessExitedException(string message, Exception innerException)
        : base(message, innerException) { }
}
