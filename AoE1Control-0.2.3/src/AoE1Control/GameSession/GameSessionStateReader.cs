namespace AoE1Control;

/// <summary>
/// Maps PlayerState availability to a stable high-level session state.
/// </summary>
public sealed class GameSessionStateReader : IGameSessionStateReader
{
    private readonly PlayerStateApi _playerStateApi;

    internal GameSessionStateReader(
        PlayerStateApi playerStateApi)
    {
        _playerStateApi =
            playerStateApi ??
            throw new ArgumentNullException(nameof(playerStateApi));
    }

    public GameSessionSnapshot Read()
    {
        PlayerStateReadResult result =
            _playerStateApi.TryRead();

        GameSessionState state =
            Map(result.Availability);

        return new GameSessionSnapshot
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

    private static GameSessionState Map(
        PlayerStateAvailability availability) =>
        availability switch
        {
            PlayerStateAvailability.Available =>
                GameSessionState.InGame,

            PlayerStateAvailability.ProcessDisconnected =>
                GameSessionState.Disconnected,

            PlayerStateAvailability.PlayerContainerUnavailable =>
                GameSessionState.Menu,

            PlayerStateAvailability.PlayerBaseUnavailable =>
                GameSessionState.Loading,

            PlayerStateAvailability.PlayerStateUnavailable =>
                GameSessionState.Loading,

            PlayerStateAvailability.ResourceOwnerUnavailable =>
                GameSessionState.Loading,

            PlayerStateAvailability.ResourceBlockUnavailable =>
                GameSessionState.Loading,

            PlayerStateAvailability.PointerChainChanged =>
                GameSessionState.Loading,

            PlayerStateAvailability.MemoryTemporarilyUnreadable =>
                GameSessionState.Loading,

            PlayerStateAvailability.ImplausibleData =>
                GameSessionState.Loading,

            _ =>
                GameSessionState.Unknown
        };
}
