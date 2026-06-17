using System;
using Flecs.NET.Core;
using Steamworks;
using Steamworks.Data;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;
using MyGame.Gameplay.Prefabs;
using MemoryPack;

namespace MyGame.Gameplay.Systems;

public static class NetworkReceiverSystem
{
    public static void Register(Flecs.NET.Core.World world)
    {
        world.System("NetworkReceiverSystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter _) =>
            {
                if (!SteamManager.IsSteamActive) return;

                while (SteamNetworking.IsP2PPacketAvailable(0))
                {
                    var packet = SteamNetworking.ReadP2PPacket(0);
                    if (packet.HasValue) ProcessTransformPacket(_.World(), packet.Value);
                }

                while (SteamNetworking.IsP2PPacketAvailable(1))
                {
                    var packet = SteamNetworking.ReadP2PPacket(1);
                    if (packet.HasValue)
                    {
                        if (packet.Value.Data[0] == PacketTypes.DistributedEvent)
                            ProcessDistributedEvent(_.World(), packet.Value);
                        else
                            ProcessSpawnOrProjectilePacket(_.World(), packet.Value);
                    }
                }
            });
    }

    private static void ProcessDistributedEvent(Flecs.NET.Core.World world, P2Packet packet)
    {
        if (packet.SteamId == SteamClient.SteamId || packet.Data.Length == 0) return;

        var payloadSpan = new ReadOnlySpan<byte>(packet.Data, 1, packet.Data.Length - 1);
        var p = MemoryPackSerializer.Deserialize<DistributedEventPacket>(payloadSpan);

        Entity? targetEntity = NetworkRegistry.GetEntity(p.TargetNetworkId);
        if (targetEntity.HasValue && targetEntity.Value.IsAlive())
        {
            Entity entity = targetEntity.Value;
            switch (p.EventType)
            {
                case GameEventType.Damage:
                    if (entity.Has<Health>())
                    {
                        ref var health = ref entity.GetMut<Health>();
                        health.Current -= p.IntPayload;
                    }
                    break;
                case GameEventType.InteractSwitch:
                    break;
            }
        }
    }

    private static void ProcessTransformPacket(Flecs.NET.Core.World world, P2Packet packet)
    {
        if (packet.SteamId == SteamClient.SteamId) return;
        if (packet.Data.Length == 0 || packet.Data[0] != PacketTypes.Transform) return;

        var payloadSpan = new ReadOnlySpan<byte>(packet.Data, 1, packet.Data.Length - 1);
        var p = MemoryPackSerializer.Deserialize<PlayerTransformPacket>(payloadSpan);

        if (p.EntityNetworkSequenceId == 0) return;
        ulong netId = p.EntityNetworkSequenceId;

        Entity? remoteShadow = NetworkRegistry.GetEntity(netId);
        if (!remoteShadow.HasValue)
        {
            string entityLookupName = $"p_{netId}";
            // Factory internally registers entity via NetworkRegistry.Add
            remoteShadow = PlayerFactory.CreateRemote(world, entityLookupName, p, packet.SteamId);
        }

        Entity entity = remoteShadow.Value;
        if (!entity.IsAlive()) return;

        ref var currentSequence = ref entity.GetMut<NetworkSequence>();
        if (p.SequenceNumber < currentSequence.LatestSequence) return;

        currentSequence.LatestSequence = p.SequenceNumber;
        currentSequence.TimeSinceLastPacket = 0f;

        entity.Set(new TargetPosition { X = p.X, Y = p.Y });
        entity.Set(new Velocity { X = p.Vx, Y = p.Vy });
        entity.Set(new FacingDirection { Value = p.FacingDirection });
    }

    private static void ProcessSpawnOrProjectilePacket(Flecs.NET.Core.World world, P2Packet packet)
    {
        if (packet.SteamId == SteamClient.SteamId || packet.Data.Length == 0) return;

        if (packet.Data[0] == PacketTypes.Spawn)
        {
            var payloadSpan = new ReadOnlySpan<byte>(packet.Data, 1, packet.Data.Length - 1);
            var p = MemoryPackSerializer.Deserialize<PlayerSpawnPacket>(payloadSpan);

            if (p.EntityNetworkSequenceId == 0) return;
            ulong netId = p.EntityNetworkSequenceId;

            Entity? existingEntity = NetworkRegistry.GetEntity(netId);
            if (existingEntity.HasValue && existingEntity.Value.IsAlive()) return;

            string entityLookupName = $"p_{netId}";
            var mockTransform = new PlayerTransformPacket { X = p.StartX, Y = p.StartY, EntityNetworkSequenceId = p.EntityNetworkSequenceId };
            PlayerFactory.CreateRemote(world, entityLookupName, mockTransform, packet.SteamId);
        }
        else if (packet.Data[0] == PacketTypes.ProjectileSpawn)
        {
            var payloadSpan = new ReadOnlySpan<byte>(packet.Data, 1, packet.Data.Length - 1);
            var p = MemoryPackSerializer.Deserialize<ProjectileSpawnPacket>(payloadSpan);
            ProjectileFactory.Create(world, p.StartX, p.StartY, p.VelocityX, p.VelocityY, p.EntityNetworkSequenceId, packet.SteamId);
        }
    }
}
