namespace AoE1Control;

/// <summary>
/// Describes whether the local-player state is currently readable.
/// Temporary unavailability is expected while the game changes screens,
/// unloads a scenario, or creates a new player structure.
/// </summary>
public enum PlayerStateAvailability
{
    Available = 0,
    ProcessDisconnected,
    PlayerContainerUnavailable,
    PlayerBaseUnavailable,
    PlayerStateUnavailable,
    ResourceOwnerUnavailable,
    ResourceBlockUnavailable,
    PointerChainChanged,
    MemoryTemporarilyUnreadable,
    ImplausibleData,
    UnknownFailure
}
