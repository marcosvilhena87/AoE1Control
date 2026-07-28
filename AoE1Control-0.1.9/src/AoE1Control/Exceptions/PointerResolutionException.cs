namespace AoE1Control;

/// <summary>Falha ao resolver a cadeia de ponteiros.</summary>
public sealed class PointerResolutionException : AoE1ControlException
{
    public PointerResolutionException(string message) : base(message) { }

    public PointerResolutionException(string message, Exception innerException)
        : base(message, innerException) { }
}
