using AoE1Control.Memory;
using AoE1Control.Profiles;

namespace AoE1Control.Internal;

internal sealed class PointerChainResolver
{
    private readonly ProcessMemoryReader _memory;
    private readonly nint _moduleBase;
    private readonly GameProfile _profile;

    private ResolvedPlayerPointers? _cached;

    internal PointerChainResolver(
        ProcessMemoryReader memory,
        nint moduleBase,
        GameProfile profile)
    {
        _memory = memory;
        _moduleBase = moduleBase;
        _profile = profile;
    }

    internal ResolvedPlayerPointers Resolve()
    {
        if (_cached is not null && IsStillValid(_cached))
            return _cached;

        _cached = ResolveFromRoot();
        return _cached;
    }

    internal void Invalidate() =>
        _cached = null;

    private ResolvedPlayerPointers ResolveFromRoot()
    {
        uint moduleBase = checked((uint)_moduleBase.ToInt64());

        uint rootOffset =
            HexParser.ParseUInt32(
                _profile.LocalPlayer.PlayerContainer.ModuleOffset);

        uint playerContainer =
            _memory.ReadPointer32(
                checked(moduleBase + rootOffset));

        uint playerBase =
            _memory.ReadPointer32(
                checked(playerContainer +
                    HexParser.ParseUInt32(
                        _profile.LocalPlayer.PlayerBaseOffset)));

        uint resourceOwner =
            _memory.ReadPointer32(
                checked(playerBase +
                    HexParser.ParseUInt32(
                        _profile.LocalPlayer.ResourceOwnerOffset)));

        uint resourceBlock =
            _memory.ReadPointer32(
                checked(resourceOwner +
                    HexParser.ParseUInt32(
                        _profile.LocalPlayer.ResourceBlockOffset)));

        ResolvedPlayerPointers result = new(
            playerContainer,
            playerBase,
            resourceOwner,
            resourceBlock);

        if (!IsStillValid(result))
            throw new PointerResolutionException(
                "A cadeia do jogador local produziu ponteiros inválidos.");

        return result;
    }

    private bool IsStillValid(ResolvedPlayerPointers pointers)
    {
        if (pointers.PlayerContainer == 0 ||
            pointers.PlayerBase == 0 ||
            pointers.ResourceOwner == 0 ||
            pointers.ResourceBlock == 0)
        {
            return false;
        }

        return _memory.CanRead(
            pointers.ResourceBlock,
            16);
    }
}

internal sealed record ResolvedPlayerPointers(
    uint PlayerContainer,
    uint PlayerBase,
    uint ResourceOwner,
    uint ResourceBlock);
