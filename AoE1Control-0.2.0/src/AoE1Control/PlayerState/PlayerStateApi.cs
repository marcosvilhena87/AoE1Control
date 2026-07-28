using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control;

/// <summary>
/// Convenience factory for the read-only PlayerState API.
/// </summary>
public sealed class PlayerStateApi : IDisposable
{
    private readonly GameConnection _connection;

    private PlayerStateApi(
        GameConnection connection)
    {
        _connection = connection;
        Reader = new PlayerStateReader(connection);
    }

    public IPlayerStateReader Reader { get; }

    public bool IsConnected =>
        _connection.IsConnected;

    public string ProfileId =>
        _connection.Profile.ProfileId;

    public static PlayerStateApi Connect(
        AoE1ControlOptions? options = null)
    {
        options ??=
            new AoE1ControlOptions();

        string profilesPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "profiles");

        IReadOnlyList<GameProfile> profiles =
            new ProfileRepository(
                profilesPath)
                .LoadAll();

        GameConnection connection =
            GameConnection.Connect(
                options,
                profiles);

        return new PlayerStateApi(
            connection);
    }

    public PlayerStateSnapshot Read() =>
        Reader.Read();

    public void Dispose() =>
        _connection.Dispose();
}
