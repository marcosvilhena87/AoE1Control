using AoE1Control.Internal;

namespace AoE1Control;

/// <summary>
/// Reads the validated local-player state from Age of Empires Gold Edition.
/// This reader never writes to the game process.
/// </summary>
public sealed class PlayerStateReader : IPlayerStateReader
{
    private const uint PlayerContainerModuleOffset = 0x16D604;
    private const uint PlayerBaseFromContainerOffset = 0x04BC;
    private const uint PlayerStateFromPlayerBaseOffset = 0x0100;

    private const uint PopulationAvailableOffset = 0x0008;
    private const uint PopulationCurrentOffset = 0x0016;
    private const uint LightTransportCountOffset = 0x002A;
    private const uint EconomicShipsAOffset = 0x004A;
    private const uint MilitaryPopulationOffset = 0x0050;
    private const uint VillagerCountOffset = 0x0052;
    private const uint EconomicShipsBOffset = 0x0066;

    private const uint ResourceOwnerFromPlayerBaseOffset = 0x00F8;
    private const uint ResourceBlockFromOwnerOffset = 0x0050;

    private readonly GameConnection _connection;

    internal PlayerStateReader(
        GameConnection connection)
    {
        _connection =
            connection ??
            throw new ArgumentNullException(nameof(connection));
    }

    public PlayerStateSnapshot Read()
    {
        PlayerStateReadResult result =
            TryRead();

        if (result.IsAvailable)
            return result.Snapshot!;

        throw new PlayerStateUnavailableException(
            result.Availability,
            result.Message ??
            $"Player state is not available: {result.Availability}.",
            result.Exception ??
            new InvalidOperationException(
                "No inner exception was provided."));
    }

    public PlayerStateReadResult TryRead()
    {
        if (!_connection.IsConnected)
        {
            return PlayerStateReadResult.Unavailable(
                PlayerStateAvailability.ProcessDisconnected,
                "The game process is no longer connected.");
        }

        try
        {
            AddressResolutionResult firstResult =
                TryResolveAddresses();

            if (!firstResult.IsAvailable)
                return firstResult.ToReadResult();

            ResolvedAddresses first =
                firstResult.Addresses!;

            PlayerStateSnapshot snapshot =
                ReadSnapshot(first);

            AddressResolutionResult secondResult =
                TryResolveAddresses();

            if (!secondResult.IsAvailable)
                return secondResult.ToReadResult();

            ResolvedAddresses second =
                secondResult.Addresses!;

            if (first != second)
            {
                return PlayerStateReadResult.Unavailable(
                    PlayerStateAvailability.PointerChainChanged,
                    "The player pointer chain changed while the snapshot was being read.");
            }

            Validate(snapshot);

            return PlayerStateReadResult.Available(
                snapshot);
        }
        catch (PlayerStateUnavailableException ex)
        {
            return PlayerStateReadResult.Unavailable(
                ex.Availability,
                ex.Message,
                ex);
        }
        catch (MemoryReadException ex)
        {
            return PlayerStateReadResult.Unavailable(
                PlayerStateAvailability.MemoryTemporarilyUnreadable,
                ex.Message,
                ex);
        }
        catch (PlayerStateReadException ex)
        {
            return PlayerStateReadResult.Unavailable(
                PlayerStateAvailability.ImplausibleData,
                ex.Message,
                ex);
        }
        catch (Exception ex)
            when (ex is AoE1ControlException
                or OverflowException
                or ArgumentOutOfRangeException)
        {
            return PlayerStateReadResult.Unavailable(
                PlayerStateAvailability.UnknownFailure,
                "Unable to read a consistent local-player snapshot.",
                ex);
        }
    }

    private AddressResolutionResult TryResolveAddresses()
    {
        uint moduleBase =
            unchecked(
                (uint)_connection.ModuleBase.ToInt64());

        uint playerContainer =
            ReadPointer(
                checked(
                    moduleBase +
                    PlayerContainerModuleOffset),
                PlayerStateAvailability.PlayerContainerUnavailable,
                "PlayerContainer");

        if (!IsPlausibleAddress(playerContainer))
        {
            return AddressResolutionResult.Unavailable(
                PlayerStateAvailability.PlayerContainerUnavailable,
                $"PlayerContainer is not available: 0x{playerContainer:X8}.");
        }

        uint playerBase =
            ReadPointer(
                checked(
                    playerContainer +
                    PlayerBaseFromContainerOffset),
                PlayerStateAvailability.PlayerBaseUnavailable,
                "PlayerBase");

        if (!IsPlausibleAddress(playerBase))
        {
            return AddressResolutionResult.Unavailable(
                PlayerStateAvailability.PlayerBaseUnavailable,
                $"PlayerBase is not available: 0x{playerBase:X8}.");
        }

        uint playerState =
            ReadPointer(
                checked(
                    playerBase +
                    PlayerStateFromPlayerBaseOffset),
                PlayerStateAvailability.PlayerStateUnavailable,
                "PlayerState");

        if (!IsPlausibleAddress(playerState))
        {
            return AddressResolutionResult.Unavailable(
                PlayerStateAvailability.PlayerStateUnavailable,
                $"PlayerState is not available: 0x{playerState:X8}.");
        }

        uint resourceOwner =
            ReadPointer(
                checked(
                    playerBase +
                    ResourceOwnerFromPlayerBaseOffset),
                PlayerStateAvailability.ResourceOwnerUnavailable,
                "ResourceOwner");

        if (!IsPlausibleAddress(resourceOwner))
        {
            return AddressResolutionResult.Unavailable(
                PlayerStateAvailability.ResourceOwnerUnavailable,
                $"ResourceOwner is not available: 0x{resourceOwner:X8}.");
        }

        uint resourceBlock =
            ReadPointer(
                checked(
                    resourceOwner +
                    ResourceBlockFromOwnerOffset),
                PlayerStateAvailability.ResourceBlockUnavailable,
                "ResourceBlock");

        if (!IsPlausibleAddress(resourceBlock))
        {
            return AddressResolutionResult.Unavailable(
                PlayerStateAvailability.ResourceBlockUnavailable,
                $"ResourceBlock is not available: 0x{resourceBlock:X8}.");
        }

        return AddressResolutionResult.Available(
            new ResolvedAddresses(
                playerContainer,
                playerBase,
                playerState,
                resourceBlock));
    }

    private uint ReadPointer(
        uint address,
        PlayerStateAvailability availability,
        string name)
    {
        try
        {
            return _connection.Memory.ReadPointer32(
                address);
        }
        catch (MemoryReadException ex)
        {
            throw new PlayerStateUnavailableException(
                availability,
                $"{name} pointer could not be read at 0x{address:X8}.",
                ex);
        }
    }

    private PlayerStateSnapshot ReadSnapshot(
        ResolvedAddresses addresses)
    {
        sbyte available =
            unchecked(
                (sbyte)_connection.Memory.ReadAvailableBytes(
                    checked(
                        addresses.PlayerState +
                        PopulationAvailableOffset),
                    1)[0]);

        byte current =
            ReadByte(
                addresses.PlayerState,
                PopulationCurrentOffset);

        byte lightTransports =
            ReadByte(
                addresses.PlayerState,
                LightTransportCountOffset);

        byte economicShipsA =
            ReadByte(
                addresses.PlayerState,
                EconomicShipsAOffset);

        byte militaryPopulation =
            ReadByte(
                addresses.PlayerState,
                MilitaryPopulationOffset);

        byte villagers =
            ReadByte(
                addresses.PlayerState,
                VillagerCountOffset);

        byte economicShipsB =
            ReadByte(
                addresses.PlayerState,
                EconomicShipsBOffset);

        PlayerResources resources =
            new(
                Food:
                    _connection.Memory.ReadSingle(
                        checked(
                            addresses.ResourceBlock +
                            0x00)),
                Wood:
                    _connection.Memory.ReadSingle(
                        checked(
                            addresses.ResourceBlock +
                            0x04)),
                Stone:
                    _connection.Memory.ReadSingle(
                        checked(
                            addresses.ResourceBlock +
                            0x08)),
                Gold:
                    _connection.Memory.ReadSingle(
                        checked(
                            addresses.ResourceBlock +
                            0x0C)));

        return new PlayerStateSnapshot
        {
            Timestamp =
                DateTimeOffset.UtcNow,

            Resources =
                resources,

            Population =
                new PlayerPopulation
                {
                    Current =
                        current,

                    Available =
                        available
                },

            Units =
                new PlayerUnitCounters
                {
                    Villagers =
                        villagers,

                    MilitaryPopulation =
                        militaryPopulation,

                    LightTransports =
                        lightTransports,

                    EconomicShipsA =
                        economicShipsA,

                    EconomicShipsB =
                        economicShipsB
                },

            Addresses =
                new PlayerStateAddresses(
                    addresses.PlayerContainer,
                    addresses.PlayerBase,
                    addresses.PlayerState,
                    addresses.ResourceBlock)
        };
    }

    private byte ReadByte(
        uint baseAddress,
        uint offset) =>
        _connection.Memory.ReadAvailableBytes(
            checked(
                baseAddress +
                offset),
            1)[0];

    private static void Validate(
        PlayerStateSnapshot snapshot)
    {
        if (snapshot.Population.Current is < 0 or > 250)
        {
            throw new PlayerStateReadException(
                $"Implausible population current value: {snapshot.Population.Current}.");
        }

        if (snapshot.Population.Available is < -250 or > 250)
        {
            throw new PlayerStateReadException(
                $"Implausible population available value: {snapshot.Population.Available}.");
        }

        if (snapshot.Population.Capacity is < 0 or > 250)
        {
            throw new PlayerStateReadException(
                $"Implausible population capacity value: {snapshot.Population.Capacity}.");
        }

        if (snapshot.Units.Villagers > snapshot.Population.Current)
        {
            throw new PlayerStateReadException(
                "Villager count is greater than current population.");
        }

        if (snapshot.Units.MilitaryPopulation > snapshot.Population.Current)
        {
            throw new PlayerStateReadException(
                "Military population is greater than current population.");
        }

        ValidateResource(nameof(snapshot.Resources.Food), snapshot.Resources.Food);
        ValidateResource(nameof(snapshot.Resources.Wood), snapshot.Resources.Wood);
        ValidateResource(nameof(snapshot.Resources.Stone), snapshot.Resources.Stone);
        ValidateResource(nameof(snapshot.Resources.Gold), snapshot.Resources.Gold);
    }

    private static void ValidateResource(
        string name,
        float value)
    {
        if (float.IsNaN(value) ||
            float.IsInfinity(value) ||
            value < 0 ||
            value > 1_000_000)
        {
            throw new PlayerStateReadException(
                $"Implausible resource value: {name}={value}.");
        }
    }

    private static bool IsPlausibleAddress(
        uint address) =>
        address >= 0x00010000 &&
        address < 0x80000000;

    private sealed record ResolvedAddresses(
        uint PlayerContainer,
        uint PlayerBase,
        uint PlayerState,
        uint ResourceBlock);

    private sealed record AddressResolutionResult
    {
        public required PlayerStateAvailability Availability { get; init; }

        public ResolvedAddresses? Addresses { get; init; }

        public string? Message { get; init; }

        public bool IsAvailable =>
            Availability == PlayerStateAvailability.Available &&
            Addresses is not null;

        public static AddressResolutionResult Available(
            ResolvedAddresses addresses) =>
            new()
            {
                Availability =
                    PlayerStateAvailability.Available,

                Addresses =
                    addresses
            };

        public static AddressResolutionResult Unavailable(
            PlayerStateAvailability availability,
            string message) =>
            new()
            {
                Availability =
                    availability,

                Message =
                    message
            };

        public PlayerStateReadResult ToReadResult() =>
            PlayerStateReadResult.Unavailable(
                Availability,
                Message ??
                $"Address resolution failed: {Availability}.");
    }
}
