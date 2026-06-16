using System;
using System.Buffers;
using Flecs.NET.Core;
using Steamworks;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;
using MyGame.Gameplay.Prefabs;
using MemoryPack;

namespace MyGame.Gameplay.Systems;

public static class ProjectileSystem
{
    private static readonly ArrayBufferWriter<byte> _bufferWriter = new ArrayBufferWriter<byte>(128);
    private static readonly byte[] _reusableBuffer = new byte[128];

    public static void Register(Flecs.NET.Core.World world)
    {
        world.Observer<ProjectileSpawnRequest>("ProjectileSpawnObserver")
            .Event(Ecs.OnSet)
            .Each((Iter it, int i, ref ProjectileSpawnRequest req) =>
            {
                Entity reqEntity = it.Entity(i);
                ulong uniqueNetId = NetworkIdGenerator.GetNext();

                ProjectileFactory.Create(it.World(), req.StartX, req.StartY, req.VelocityX, req.VelocityY, uniqueNetId, SteamClient.SteamId);

                if (SteamManager.IsSteamActive && SteamManager.CurrentLobby.HasValue)
                {
                    var packet = new ProjectileSpawnPacket
                    {
                        StartX = req.StartX, StartY = req.StartY,
                        VelocityX = req.VelocityX, VelocityY = req.VelocityY,
                        EntityNetworkSequenceId = uniqueNetId,
                        OwnerSteamId = SteamClient.SteamId.Value
                    };

                    _bufferWriter.Clear();
                    var headerSpan = _bufferWriter.GetSpan(1);
                    headerSpan[0] = PacketTypes.ProjectileSpawn;
                    _bufferWriter.Advance(1);

                    MemoryPackSerializer.Serialize(_bufferWriter, packet);
                    int packetLength = _bufferWriter.WrittenCount;
                    _bufferWriter.WrittenSpan.CopyTo(_reusableBuffer);

                    foreach (var member in SteamManager.CurrentLobby.Value.Members)
                    {
                        if (member.Id != SteamClient.SteamId)
                        {
                            // Correct 2.3.4 API usage
                            SteamNetworking.SendP2PPacket(member.Id, _reusableBuffer, packetLength, 1, P2PSend.Reliable);
                        }
                    }
                }

                reqEntity.Destruct();
            });

        world.System<Lifetime, PhysicsBody>("ProjectileLifetimeSystem")
            .Kind(Ecs.OnUpdate)
            .Each((Iter it, int row, ref Lifetime life, ref PhysicsBody pBody) =>
            {
                life.Remaining -= it.DeltaTime();
                if (life.Remaining <= 0f)
                {
                    if (pBody.Value != null) Game1.Instance.PhysicsWorld.Remove(pBody.Value);
                    it.Entity(row).Destruct();
                }
            });

        world.System<PhysicsBody, Position>("SyncProjectilePhysicsSystem")
            .Kind(Ecs.PostUpdate)
            .With<ProjectileTag>()
            .Each((ref PhysicsBody pBody, ref Position pos) =>
            {
                if (pBody.Value == null) return;
                pos.X = pBody.Value.Position.X * PlayerFactory.PixelsPerMeter;
                pos.Y = pBody.Value.Position.Y * PlayerFactory.PixelsPerMeter;
            });
    }
}
