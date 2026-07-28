namespace AoE1Control;

/// <summary>
/// Public minimal API for monitoring whether the game is in a match.
/// </summary>
public sealed class GamePresenceApi : IDisposable
{
    private readonly PlayerStateApi _playerStateApi;

    private GamePresenceApi(
        PlayerStateApi playerStateApi)
    {
        _playerStateApi = playerStateApi;
        Reader = new GamePresenceReader(playerStateApi);
    }

    public IGamePresenceReader Reader { get; }

    public bool IsConnected =>
        _playerStateApi.IsConnected;

    public string ProfileId =>
        _playerStateApi.ProfileId;

    public static GamePresenceApi Connect(
        AoE1ControlOptions? options = null) =>
        new(
            PlayerStateApi.Connect(options));

    public GamePresenceSnapshot Read() =>
        Reader.Read();

    public bool IsInGame() =>
        Read().IsInGame;

    public void Dispose() =>
        _playerStateApi.Dispose();
}
