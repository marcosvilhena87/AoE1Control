namespace AoE1Control;

/// <summary>A versão do jogo não é suportada.</summary>
public sealed class UnsupportedGameVersionException : AoE1ControlException
{
    public UnsupportedGameVersionException(string message) : base(message) { }

    public UnsupportedGameVersionException(string message, Exception innerException)
        : base(message, innerException) { }
}
