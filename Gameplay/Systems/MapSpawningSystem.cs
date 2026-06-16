using Flecs.NET.Core;
using MyGame.Engine.Maps;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;
using MyGame.Gameplay.Prefabs;
using Steamworks;
using MemoryPack;
using nkast.Aether.Physics2D.Dynamics;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;
using System;
using MyGame.Engine.Core;

namespace MyGame.Gameplay.Systems;

public static class MapSpawningSystem
{
    public static void Register(Flecs.NET.Core.World world)
    {
        world.Observer<MapLoadRequest>()
            .Event(Ecs.OnSet)
            .Each((Iter it, int i, ref MapLoadRequest request) =>
            {
                Entity reqEntity = it.Entity(i);
                LevelData loadedRoom = MapLoader.LoadSingleLevel(request.MapPath);

                EngineLogger.Log($"Spawn Point calculated: {loadedRoom.SpawnPoint.X}, {loadedRoom.SpawnPoint.Y}", "MAP");

                world.Entity("GlobalMapData").Set(new MapInstance { Data = loadedRoom });

                // Create level physics
                var levelStaticBody = Game1.Instance.PhysicsWorld.CreateBody(AetherVector2.Zero, 0f, BodyType.Static);
                foreach (var col in loadedRoom.Collisions)
                {
                    float physX = (col.X + col.Width / 2f) / PlayerFactory.PixelsPerMeter;
                    float physY = (col.Y + col.Height / 2f) / PlayerFactory.PixelsPerMeter;
                    float physW = col.Width / PlayerFactory.PixelsPerMeter;
                    float physH = col.Height / PlayerFactory.PixelsPerMeter;

                    var fixture = levelStaticBody.CreateRectangle(physW, physH, 1f, new AetherVector2(physX, physY));
                    fixture.Friction = 0.3f;
                    fixture.CollisionCategories = PhysicsLayers.Environment;
                }

                Entity localAvatar = PlayerFactory.CreateLocal(world, request.LocalClassId, loadedRoom.SpawnPoint.X, loadedRoom.SpawnPoint.Y);

                // Set initial ECS position components
                localAvatar.Set(new Position { X = loadedRoom.SpawnPoint.X, Y = loadedRoom.SpawnPoint.Y });
                localAvatar.Set(new PreviousPosition { X = loadedRoom.SpawnPoint.X, Y = loadedRoom.SpawnPoint.Y });

                // Network Sync
                if (SteamManager.IsSteamActive && SteamManager.CurrentLobby.HasValue)
                {
                    var handshakePayload = new PlayerSpawnPacket
                    {
                        CharacterClassId = request.LocalClassId,
                        StartX = loadedRoom.SpawnPoint.X,
                        StartY = loadedRoom.SpawnPoint.Y,
                        EntityNetworkSequenceId = localAvatar.Get<NetworkId>().Value
                    };

                    byte[] payload = MemoryPackSerializer.Serialize(handshakePayload);
                    byte[] networkBuffer = new byte[payload.Length + 1];
                    networkBuffer[0] = PacketTypes.Spawn;
                    Buffer.BlockCopy(payload, 0, networkBuffer, 1, payload.Length);

                    foreach (var peer in SteamManager.CurrentLobby.Value.Members)
                    {
                        if (peer.Id != SteamClient.SteamId)
                        {
                            SteamNetworking.SendP2PPacket(peer.Id, networkBuffer, networkBuffer.Length, 1, P2PSend.Reliable);
                        }
                    }
                }

                reqEntity.Destruct();
            });
    }
}
