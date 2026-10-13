using Architect.Multiplayer.Hkmp.Data;
using Hkmp.Networking.Packet;

namespace Architect.Multiplayer.Hkmp;

public enum PacketId
{
    Clear,
    Move,
    Erase,
    Tiles,
    Lock,
    Place,
    Event,
    LockEditor
}

public static class PacketIdExtension
{
    public static IPacketData GetData(this PacketId packetId)
    {
        return packetId switch
        {
            PacketId.Clear => new ClearPacketData(),
            PacketId.Move => new MovePacketData(),
            PacketId.Erase => new ErasePacketData(),
            PacketId.Tiles => new TilePacketData(),
            PacketId.Lock => new LockPacketData(),
            PacketId.Place => new PlacePacketData(),
            PacketId.Event => new EventPacketData(),
            PacketId.LockEditor => new LockEditorPacketData(),
            _ => null
        };
    }
}
