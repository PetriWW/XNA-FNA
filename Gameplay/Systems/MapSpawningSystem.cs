using System;
using Flecs.NET.Core;
using MyGame.Engine.Maps;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;
using MyGame.Gameplay.Prefabs;
using Steamworks;
using MemoryPack;
using nkast.Aether.Physics2D.Dynamics;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;
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

                // Safely copy values from the ref struct immediately
                string mapPath = request.MapPath;
                int localClassId = request.LocalClassId;

                LevelData loadedRoom = MapLoader.LoadSingleLevel(mapPath);

                EngineLogger.Log($"Spawn Point calculated: {loadedRoom.SpawnPoint.X}, {loadedRoom.SpawnPoint.Y}", "MAP");

                world.Entity("GlobalMapData").Set(new MapInstance { Data = loadedRoom });

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

                Entity localAvatar = PlayerFactory.CreateLocal(world, localClassId, loadedRoom.SpawnPoint.X, loadedRoom.SpawnPoint.Y);

                localAvatar.Set(new Position { X = loadedRoom.SpawnPoint.X, Y = loadedRoom.SpawnPoint.Y });
                localAvatar.Set(new PreviousPosition { X = loadedRoom.SpawnPoint.X, Y = loadedRoom.SpawnPoint.Y });

                ulong netId = localAvatar.Has<NetworkId>() ? localAvatar.Get<NetworkId>().Value : 0;

                var lobby = SteamManager.CurrentLobby;
                if (SteamManager.IsSteamActive && lobby.HasValue)
                {
                    var handshakePayload = new PlayerSpawnPacket
                    {
                        CharacterClassId = localClassId,
                        StartX = loadedRoom.SpawnPoint.X,
                        StartY = loadedRoom.SpawnPoint.Y,
                        EntityNetworkSequenceId = netId
                    };

                    byte[] payload = MemoryPackSerializer.Serialize(handshakePayload);
                    byte[] networkBuffer = new byte[payload.Length + 1];
                    networkBuffer[0] = PacketTypes.Spawn;
                    Buffer.BlockCopy(payload, 0, networkBuffer, 1, payload.Length);

                    // ARCHITECTURE FIX: Safely wrap Facepunch's internal iterator to prevent NREs
                    // when the lobby member list hasn't finished downloading from Steam.
                    try
                    {
                        foreach (var peer in lobby.Value.Members)
                        {
                            if (peer.Id != SteamClient.SteamId)
                            {
                                SteamNetworking.SendP2PPacket(peer.Id, networkBuffer, networkBuffer.Length, 1, P2PSend.Reliable);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        EngineLogger.LogError("Steamworks delayed member sync. Handshake broadcast safely bypassed.", ex);
                    }
                }

                reqEntity.Destruct();
            });
    }
}
