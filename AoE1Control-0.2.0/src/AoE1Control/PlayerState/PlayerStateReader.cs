using AoE1Control.Internal;
using AoE1Control.Profiles;

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

    internal PlayerStateReader(GameConnection connection)
    {
        _connection =
            connection ??
            throw new ArgumentNullException(nameof(connection));
    }

    public PlayerStateSnapshot Read()
    {
        if (!_connection.IsConnected)
        {
            throw new PlayerStateReadException(
                "The game process is no longer connected.");
        }

        try
        {
            ResolvedAddresses first =
                ResolveAddresses();

            PlayerStateSnapshot snapshot =
                ReadSnapshot(first);

            // Resolve again after reading. If the game swapped structures during
            // the read, discard this mixed snapshot.
            ResolvedAddresses second =
                ResolveAddresses();

            if (first != second)
            {
                throw new PlayerStateReadException(
                    "The player pointer chain changed while the snapshot was being read.");
            }

            Validate(snapshot);

            return snapshot;
        }
        catch (PlayerStateReadException)
        {
            throw;
        }
        catch (Exception ex)
            when (ex is AoE1ControlException
                or OverflowException
                or ArgumentOutOfRangeException)
        {
            throw new PlayerStateReadException(
                "Unable to read a consistent local-player snapshot.",
                ex);
        }
    }

    private ResolvedAddresses ResolveAddresses()
    {
        uint moduleBase =
            unchecked(
                (uint)_connection.ModuleBase.ToInt64());

        uint playerContainer =
            _connection.Memory.ReadPointer32(
                checked(
                    moduleBase +
                    PlayerContainerModuleOffset));

        uint playerBase =
            _connection.Memory.ReadPointer32(
                checked(
                    playerContainer +
                    PlayerBaseFromContainerOffset));

        uint playerState =
            _connection.Memory.ReadPointer32(
                checked(
                    playerBase +
                    PlayerStateFromPlayerBaseOffset));

        uint resourceOwner =
            _connection.Memory.ReadPointer32(
                checked(
                    playerBase +
                    ResourceOwnerFromPlayerBaseOffset));

        uint resourceBlock =
            _connection.Memory.ReadPointer32(
                checked(
                    resourceOwner +
                    ResourceBlockFromOwnerOffset));

        ValidateAddress(
            nameof(playerContainer),
            playerContainer);

        ValidateAddress(
            nameof(playerBase),
            playerBase);

        ValidateAddress(
            nameof(playerState),
            playerState);

        ValidateAddress(
            nameof(resourceBlock),
            resourceBlock);

        return new ResolvedAddresses(
            playerContainer,
            playerBase,
            playerState,
            resourceBlock);
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

        ValidateResource(
            nameof(snapshot.Resources.Food),
            snapshot.Resources.Food);

        ValidateResource(
            nameof(snapshot.Resources.Wood),
            snapshot.Resources.Wood);

        ValidateResource(
            nameof(snapshot.Resources.Stone),
            snapshot.Resources.Stone);

        ValidateResource(
            nameof(snapshot.Resources.Gold),
            snapshot.Resources.Gold);
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

    private static void ValidateAddress(
        string name,
        uint address)
    {
        if (address < 0x00010000 ||
            address >= 0x80000000)
        {
            throw new PlayerStateReadException(
                $"Implausible {name} address: 0x{address:X8}.");
        }
    }

    private sealed record ResolvedAddresses(
        uint PlayerContainer,
        uint PlayerBase,
        uint PlayerState,
        uint ResourceBlock);
}
