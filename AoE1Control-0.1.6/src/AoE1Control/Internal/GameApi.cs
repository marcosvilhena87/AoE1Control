namespace AoE1Control.Internal;

internal sealed class GameApi : IAoE1GameApi
{
    private readonly GameConnection _connection;
    private readonly GameSessionReader _sessionReader;
    private readonly LocalPlayerReader _playerReader;
    private bool _disposed;

    internal GameApi(
        GameConnection connection,
        GameSessionReader sessionReader,
        LocalPlayerReader playerReader)
    {
        _connection = connection;
        _sessionReader = sessionReader;
        _playerReader = playerReader;
    }

    public bool IsConnected =>
        !_disposed &&
        _connection.IsConnected;

    public bool IsGameRunning =>
        IsConnected &&
        _sessionReader.IsSessionActive();

    public GameVersionInfo GameVersion =>
        _connection.GameVersion;

    public GameSnapshot GetSnapshot()
    {
        ThrowIfDisposed();

        if (!_connection.IsConnected)
            throw new GameProcessExitedException(
                "O processo do Age of Empires não está mais disponível.");

        if (!_sessionReader.IsSessionActive())
            throw new GameSessionNotActiveException(
                "Nenhuma partida ativa foi detectada.");

        PlayerSnapshot player = _playerReader.Read();

        return new GameSnapshot(
            DateTimeOffset.UtcNow,
            true,
            player);
    }

    public PlayerSnapshot GetLocalPlayer() =>
        GetSnapshot().LocalPlayer;

    public ResourceSnapshot GetResources() =>
        GetLocalPlayer().Resources;

    public bool TryGetSnapshot(out GameSnapshot? snapshot)
    {
        try
        {
            snapshot = GetSnapshot();
            return true;
        }
        catch (AoE1ControlException)
        {
            snapshot = null;
            return false;
        }
    }

    public void Refresh()
    {
        ThrowIfDisposed();
        _connection.Refresh();
        _playerReader.InvalidateResolvedPointers();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _connection.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            nameof(GameApi));
    }
}
