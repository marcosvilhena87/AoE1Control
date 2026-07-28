namespace AoE1Control;

/// <summary>
/// Maps PlayerState availability to only two states:
/// InGame or NotInGame.
/// </summary>
public sealed class GamePresenceReader : IGamePresenceReader
{
    private readonly PlayerStateApi _playerStateApi;

    internal GamePresenceReader(
        PlayerStateApi playerStateApi)
    {
        _playerStateApi =
            playerStateApi ??
            throw new ArgumentNullException(nameof(playerStateApi));
    }

    public GamePresenceSnapshot Read()
    {
        PlayerStateReadResult result =
            _playerStateApi.TryRead();

        GamePresenceState state =
            result.IsAvailable
                ? GamePresenceState.InGame
                : GamePresenceState.NotInGame;

        return new GamePresenceSnapshot
        {
            Timestamp =
                DateTimeOffset.UtcNow,

            State =
                state,

            PlayerStateAvailability =
                result.Availability,

            Message =
                result.Message,

            PlayerState =
                result.Snapshot
        };
    }
}
