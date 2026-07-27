namespace AoE1Control;

/// <summary>Falha ao ler a memória do jogo.</summary>
public sealed class MemoryReadException : AoE1ControlException
{
    public MemoryReadException(string message) : base(message) { }

    public MemoryReadException(string message, Exception innerException)
        : base(message, innerException) { }
}
