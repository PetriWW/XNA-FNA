using System;
using Flecs.NET.Core;
using Steamworks;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;
using MyGame.Gameplay.Prefabs;

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

                // Process data updates coming from peer channels
                while (SteamNetworking.IsP2PPacketAvailable(0))
                {
                    var packetData = SteamNetworking.ReadP2PPacket(0);
                    if (!packetData.HasValue) continue;

                    byte[] buffer = packetData.Value.Data;
                    SteamId senderId = packetData.Value.SteamId;

                    if (buffer.Length == 0) continue;

                    byte packetType = buffer[0];
                    switch (packetType)
                    {
                        case PacketTypes.Transform:
                            ProcessTransformPacket(world, buffer, senderId);
                            break;
                    }
                }

                // Drain and unpack the Reliable Signalling Channel (Channel 1)
                while (SteamNetworking.IsP2PPacketAvailable(1))
                {
                    var packetData = SteamNetworking.ReadP2PPacket(1);
                    if (!packetData.HasValue) continue;

                    byte[] buffer = packetData.Value.Data;
                    SteamId senderId = packetData.Value.SteamId;

                    if (buffer.Length == 0) continue;

                    byte packetType = buffer[0];
                    switch (packetType)
                    {
                        case PacketTypes.Spawn:
                            ProcessSpawnPacket(world, buffer, senderId);
                            break;
                    }
                }
            });
    }

    private static void ProcessTransformPacket(Flecs.NET.Core.World world, byte[] buffer, SteamId senderId)
    {
        if (senderId == SteamClient.SteamId) return;

        var packet = PlayerTransformPacket.Deserialize(buffer);
        if (packet.EntityNetworkSequenceId == 0) return;

        ulong netId = packet.EntityNetworkSequenceId;
        string entityLookupName = $"p_{netId}";

        Entity remoteShadow = world.Lookup(entityLookupName);

        if (!remoteShadow.IsAlive()) return;

        ref var currentSequence = ref remoteShadow.GetMut<NetworkSequence>();
        if (packet.SequenceNumber < currentSequence.LatestSequence) return;

        currentSequence.LatestSequence = packet.SequenceNumber;
        remoteShadow.Set(new TargetPosition { X = packet.X, Y = packet.Y });
        remoteShadow.Set(new Velocity { X = packet.Vx, Y = packet.Vy });
    }

    private static void ProcessSpawnPacket(Flecs.NET.Core.World world, byte[] buffer, SteamId senderId)
    {
        if (senderId == SteamClient.SteamId) return;

        var packet = PlayerSpawnPacket.Deserialize(buffer);
        if (packet.EntityNetworkSequenceId == 0) return;

        ulong netId = packet.EntityNetworkSequenceId;
        string entityLookupName = $"p_{netId}";

        Entity remoteShadow = world.Lookup(entityLookupName);

        if (!remoteShadow.IsAlive())
        {
            var mockTransform = new PlayerTransformPacket
            {
                X = packet.StartX,
                Y = packet.StartY,
                Vx = 0,
                Vy = 0,
                CharacterClassId = packet.CharacterClassId,
                EntityNetworkSequenceId = packet.EntityNetworkSequenceId
            };

            remoteShadow = PlayerFactory.CreateRemote(world, entityLookupName, mockTransform, senderId);
            Console.WriteLine($"[Network Handshake]: Spawned verified remote entity shadow proxy: {entityLookupName}");
        }
    }
}
