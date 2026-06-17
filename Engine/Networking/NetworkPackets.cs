using MemoryPack;

namespace MyGame.Engine.Networking;

public static class PacketTypes
{
	// Packet Routing Headers
	public const byte Transform = 1;
	public const byte Spawn = 2;
	public const byte DistributedEvent = 5; // Upgraded from CombatEvent
	public const byte ProjectileSpawn = 6;

	// Raw Signals
	public const byte LobbyStart = 10;
	public const byte PauseGame = 11;
	public const byte ResumeGame = 12;
	public const byte PlayerReady = 13;
}

public static class GameEventType
{
	public const byte Damage = 1;
	public const byte Heal = 2;
	public const byte InteractSwitch = 3;
	public const byte PickupItem = 4;
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
	public int FacingDirection;
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

// ARCHITECTURE FIX: Generic Distributed Event for RPG Mechanics
[MemoryPackable]
public partial struct DistributedEventPacket
{
	public ulong InstigatorNetworkId;
	public ulong TargetNetworkId;
	public byte EventType;
	public int IntPayload;   // E.g., Damage amount, or Door ID
	public float FloatPayload; // E.g., specific status effect duration
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
