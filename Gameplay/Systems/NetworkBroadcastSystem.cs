using System;
using System.Buffers;
using Flecs.NET.Core;
using Steamworks;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;
using MemoryPack;

namespace MyGame.Gameplay.Systems;

public static class NetworkBroadcastSystem
{
    private static uint _localSequenceCounter = 0;
    private const float NetworkDeadzoneEpsilon = 0.05f;

    private static readonly ArrayBufferWriter<byte> _bufferWriter = new ArrayBufferWriter<byte>(256);

    private static readonly byte[] _reusableBuffer = new byte[256];

    public static void Register(Flecs.NET.Core.World world)
    {
        world.System<Position, Velocity, PreviousVelocity, NetworkId, NetworkOwner>("NetworkBroadcastSystem")
            .Kind(Ecs.PostUpdate)
            .Each((Iter it, int i, ref Position pos, ref Velocity vel, ref PreviousVelocity prevVel, ref NetworkId netId, ref NetworkOwner owner) =>
            {
                if (!SteamManager.IsSteamActive || !SteamManager.CurrentLobby.HasValue) return;

                if (owner.Value != SteamClient.SteamId) return;

                bool isMoving = Math.Abs(vel.X) > NetworkDeadzoneEpsilon || Math.Abs(vel.Y) > NetworkDeadzoneEpsilon;
                bool wasMoving = Math.Abs(prevVel.X) > NetworkDeadzoneEpsilon || Math.Abs(prevVel.Y) > NetworkDeadzoneEpsilon;

                if (!isMoving && !wasMoving) return;

                _localSequenceCounter++;
                var packet = new PlayerTransformPacket
                {
                    SequenceNumber = _localSequenceCounter,
                    CharacterClassId = 0,
                    X = pos.X,
                    Y = pos.Y,
                    Vx = vel.X,
                    Vy = vel.Y,
                    EntityNetworkSequenceId = netId.Value
                };

                _bufferWriter.Clear();

                var headerSpan = _bufferWriter.GetSpan(1);
                headerSpan[0] = PacketTypes.Transform;
                _bufferWriter.Advance(1);

                MemoryPackSerializer.Serialize(_bufferWriter, packet);

                // Zero-Allocation Memory Copy
                int packetLength = _bufferWriter.WrittenCount;
                _bufferWriter.WrittenSpan.CopyTo(_reusableBuffer);

                foreach (var member in SteamManager.CurrentLobby.Value.Members)
                {
                    if (member.Id != SteamClient.SteamId)
                    {
                        SteamNetworking.SendP2PPacket(member.Id, _reusableBuffer, packetLength, 0, P2PSend.Unreliable);
                    }
                }

                prevVel.X = vel.X;
                prevVel.Y = vel.Y;
            });
    }
}
