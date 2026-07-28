namespace AoE1Control;

/// <summary>Mais de um processo do jogo foi encontrado.</summary>
public sealed class MultipleGameProcessesException : AoE1ControlException
{
    public MultipleGameProcessesException(string message) : base(message) { }

    public MultipleGameProcessesException(string message, Exception innerException)
        : base(message, innerException) { }
}
