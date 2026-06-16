using System;
using System.Collections.Generic;
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
    private static readonly Dictionary<ulong, Entity> NetworkShadows = new();

    public static void ClearShadows() => NetworkShadows.Clear();
    public static void RemoveShadow(ulong networkId) => NetworkShadows.Remove(networkId);

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
                    if (packet.HasValue) ProcessSpawnOrProjectilePacket(_.World(), packet.Value);
                }
            });
    }

    private static void ProcessTransformPacket(Flecs.NET.Core.World world, P2Packet packet)
    {
        if (packet.SteamId == SteamClient.SteamId) return;
        if (packet.Data.Length == 0 || packet.Data[0] != PacketTypes.Transform) return;

        var payloadSpan = new ReadOnlySpan<byte>(packet.Data, 1, packet.Data.Length - 1);
        var p = MemoryPackSerializer.Deserialize<PlayerTransformPacket>(payloadSpan);

        if (p.EntityNetworkSequenceId == 0) return;
        ulong netId = p.EntityNetworkSequenceId;

        if (!NetworkShadows.TryGetValue(netId, out Entity remoteShadow))
        {
            string entityLookupName = $"p_{netId}";
            remoteShadow = PlayerFactory.CreateRemote(world, entityLookupName, p, packet.SteamId);
            NetworkShadows[netId] = remoteShadow;
        }

        if (!remoteShadow.IsAlive())
        {
            NetworkShadows.Remove(netId);
            return;
        }

        ref var currentSequence = ref remoteShadow.GetMut<NetworkSequence>();
        if (p.SequenceNumber < currentSequence.LatestSequence) return;

        currentSequence.LatestSequence = p.SequenceNumber;
        currentSequence.TimeSinceLastPacket = 0f;

        remoteShadow.Set(new TargetPosition { X = p.X, Y = p.Y });
        remoteShadow.Set(new Velocity { X = p.Vx, Y = p.Vy });
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

            if (NetworkShadows.TryGetValue(netId, out Entity existingEntity))
            {
                if (existingEntity.IsAlive()) return;
                NetworkShadows.Remove(netId);
            }

            string entityLookupName = $"p_{netId}";
            var mockTransform = new PlayerTransformPacket { X = p.StartX, Y = p.StartY, EntityNetworkSequenceId = p.EntityNetworkSequenceId };
            Entity newShadow = PlayerFactory.CreateRemote(world, entityLookupName, mockTransform, packet.SteamId);
            NetworkShadows[netId] = newShadow;
        }
        else if (packet.Data[0] == PacketTypes.ProjectileSpawn)
        {
            var payloadSpan = new ReadOnlySpan<byte>(packet.Data, 1, packet.Data.Length - 1);
            var p = MemoryPackSerializer.Deserialize<ProjectileSpawnPacket>(payloadSpan);
            ProjectileFactory.Create(world, p.StartX, p.StartY, p.VelocityX, p.VelocityY, p.EntityNetworkSequenceId, packet.SteamId);
        }
    }
}
