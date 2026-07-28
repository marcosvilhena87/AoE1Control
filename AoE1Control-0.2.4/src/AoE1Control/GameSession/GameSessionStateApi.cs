namespace AoE1Control;

/// <summary>
/// Public high-level API for session-state monitoring.
/// </summary>
public sealed class GameSessionStateApi : IDisposable
{
    private readonly PlayerStateApi _playerStateApi;

    private GameSessionStateApi(
        PlayerStateApi playerStateApi)
    {
        _playerStateApi = playerStateApi;
        Reader = new GameSessionStateReader(playerStateApi);
    }

    public IGameSessionStateReader Reader { get; }

    public bool IsConnected =>
        _playerStateApi.IsConnected;

    public string ProfileId =>
        _playerStateApi.ProfileId;

    public static GameSessionStateApi Connect(
        AoE1ControlOptions? options = null) =>
        new(
            PlayerStateApi.Connect(options));

    public GameSessionSnapshot Read() =>
        Reader.Read();

    public void Dispose() =>
        _playerStateApi.Dispose();
}
