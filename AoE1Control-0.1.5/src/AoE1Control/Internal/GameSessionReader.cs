using AoE1Control.Memory;
using AoE1Control.Profiles;

namespace AoE1Control.Internal;

internal sealed class GameSessionReader
{
    private readonly ProcessMemoryReader _memory;
    private readonly GameProfile _profile;

    internal GameSessionReader(
        ProcessMemoryReader memory,
        GameProfile profile)
    {
        _memory = memory;
        _profile = profile;
    }

    internal bool IsSessionActive()
    {
        try
        {
            uint address = HexParser.ParseUInt32(
                _profile.Session.Address);

            uint actual = _memory.ReadUInt32(address);
            return actual == _profile.Session.ActiveValue;
        }
        catch
        {
            return false;
        }
    }
}
