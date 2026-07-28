using AoE1Control.Memory;
using AoE1Control.Profiles;

namespace AoE1Control.Internal;

internal sealed class LocalPlayerReader
{
    private const float MaximumPlausibleResource = 1_000_000f;

    private readonly ProcessMemoryReader _memory;
    private readonly PointerChainResolver _pointerResolver;
    private readonly GameProfile _profile;

    internal LocalPlayerReader(
        ProcessMemoryReader memory,
        PointerChainResolver pointerResolver,
        GameProfile profile)
    {
        _memory = memory;
        _pointerResolver = pointerResolver;
        _profile = profile;
    }

    internal PlayerSnapshot Read()
    {
        try
        {
            return ReadCore();
        }
        catch (AoE1ControlException)
        {
            _pointerResolver.Invalidate();
            return ReadCore();
        }
    }

    internal void InvalidateResolvedPointers() =>
        _pointerResolver.Invalidate();

    private PlayerSnapshot ReadCore()
    {
        ResolvedPlayerPointers pointers =
            _pointerResolver.Resolve();

        uint block = pointers.ResourceBlock;

        float food = ReadResource(
            block,
            _profile.Resources.FoodOffset);

        float wood = ReadResource(
            block,
            _profile.Resources.WoodOffset);

        float stone = ReadResource(
            block,
            _profile.Resources.StoneOffset);

        float gold = ReadResource(
            block,
            _profile.Resources.GoldOffset);

        ResourceSnapshot resources =
            new(food, wood, stone, gold);

        Validate(resources);

        return new PlayerSnapshot(
            _profile.LocalPlayer.PlayerId,
            resources);
    }

    private float ReadResource(uint block, string offset)
    {
        uint parsedOffset = HexParser.ParseUInt32(offset);
        return _memory.ReadSingle(checked(block + parsedOffset));
    }

    private static void Validate(ResourceSnapshot resources)
    {
        if (!IsValid(resources.Food) ||
            !IsValid(resources.Wood) ||
            !IsValid(resources.Stone) ||
            !IsValid(resources.Gold))
        {
            throw new MemoryReadException(
                "O bloco de recursos retornou valores inválidos.");
        }
    }

    private static bool IsValid(float value) =>
        !float.IsNaN(value) &&
        !float.IsInfinity(value) &&
        value >= 0 &&
        value <= MaximumPlausibleResource;
}
