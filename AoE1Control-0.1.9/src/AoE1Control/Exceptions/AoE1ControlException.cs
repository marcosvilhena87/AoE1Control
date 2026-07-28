namespace AoE1Control;

/// <summary>Classe-base das falhas controladas da API.</summary>
public class AoE1ControlException : Exception
{
    public AoE1ControlException(string message) : base(message) { }

    public AoE1ControlException(string message, Exception innerException)
        : base(message, innerException) { }
}
