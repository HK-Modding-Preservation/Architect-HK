using Hkmp.Networking.Packet;

namespace Architect.Multiplayer.Hkmp.Data;

public class LockEditorPacketData : IPacketData
{
    public EditorLockType LockType = EditorLockType.Unknown;

    public void WriteData(IPacket packet)
    {
        packet.Write((int)LockType);
    }

    public void ReadData(IPacket packet)
    { 
        LockType = (EditorLockType)packet.ReadInt();
    }

    public bool IsReliable => true;
    public bool DropReliableDataIfNewerExists => false;
}