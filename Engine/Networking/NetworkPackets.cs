using MemoryPack;

namespace MyGame.Engine.Networking;

public static class PacketTypes
{
	// Packet Routing Headers
	public const byte Transform = 1;
	public const byte Spawn = 2;
	public const byte CombatEvent = 5;
	public const byte ProjectileSpawn = 6;

	// Raw Signals (No payload attached)
	public const byte LobbyStart = 10;
	public const byte PauseGame = 11;
	public const byte ResumeGame = 12;
	public const byte PlayerReady = 13;
}

[MemoryPackable]
public partial struct PlayerTransformPacket
{
	public uint SequenceNumber;
	public int CharacterClassId;
	public float X;
	public float Y;
	public float Vx;
	public float Vy;
	public ulong EntityNetworkSequenceId;
}

[MemoryPackable]
public partial struct PlayerSpawnPacket
{
	public int CharacterClassId;
	public float StartX;
	public float StartY;
	public ulong EntityNetworkSequenceId;
}

[MemoryPackable]
public partial struct CombatEventPacket
{
	public ulong AttackerNetworkId;
	public ulong VictimNetworkId;
	public int DamageAmount;
	public byte DamageType;
}

[MemoryPackable]
public partial struct ProjectileSpawnPacket
{
	public float StartX;
	public float StartY;
	public float VelocityX;
	public float VelocityY;
	public ulong EntityNetworkSequenceId;
	public ulong OwnerSteamId;
}
