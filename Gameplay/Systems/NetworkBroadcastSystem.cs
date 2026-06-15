using System;
using System.Runtime.InteropServices;
using Flecs.NET.Core;
using Steamworks;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;

namespace MyGame.Gameplay.Systems;

public static class NetworkBroadcastSystem
{
    private static uint _localSequenceCounter = 0;

    // ARCHITECTURE FIX: Physics Epsilon threshold to ignore micro-gravity jitters
    private const float NetworkDeadzoneEpsilon = 0.05f;

    public static void Register(Flecs.NET.Core.World world)
    {
        world.System<Position, Velocity, PreviousVelocity, NetworkId, NetworkOwner>("NetworkBroadcastSystem")
            .Kind(Ecs.PostUpdate)
            .Each((Iter it, int i, ref Position pos, ref Velocity vel, ref PreviousVelocity prevVel, ref NetworkId netId, ref NetworkOwner owner) =>
            {
                if (!SteamManager.IsSteamActive || !SteamManager.CurrentLobby.HasValue) return;
                if (owner.Value != SteamClient.SteamId) return;

                // Only trigger network sends if movement is perceptually meaningful
                bool isMoving = Math.Abs(vel.X) > NetworkDeadzoneEpsilon || Math.Abs(vel.Y) > NetworkDeadzoneEpsilon;
                bool wasMoving = Math.Abs(prevVel.X) > NetworkDeadzoneEpsilon || Math.Abs(prevVel.Y) > NetworkDeadzoneEpsilon;

                if (!isMoving && !wasMoving) return;

                bool isStopping = !isMoving && wasMoving;
                int channel = isStopping ? 1 : 0;
                P2PSend sendType = isStopping ? P2PSend.Reliable : P2PSend.Unreliable;

                _localSequenceCounter++;

                var packet = new PlayerTransformPacket
                {
                    PacketType = PacketTypes.Transform,
                    SequenceNumber = _localSequenceCounter,
                    CharacterClassId = 0,
                    X = pos.X,
                    Y = pos.Y,
                    Vx = vel.X,
                    Vy = vel.Y,
                    EntityNetworkSequenceId = netId.Value
                };

                int bufferSize = Marshal.SizeOf<PlayerTransformPacket>();
                byte[] buffer = new byte[bufferSize];
                packet.SerializeTo(buffer);

                foreach (var peer in SteamManager.CurrentLobby.Value.Members)
                {
                    if (peer.Id != SteamClient.SteamId)
                    {
                        SteamNetworking.SendP2PPacket(peer.Id, buffer, buffer.Length, channel, sendType);
                    }
                }

                prevVel.X = vel.X;
                prevVel.Y = vel.Y;
            });
    }
}
