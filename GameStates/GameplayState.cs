using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.States;
using MyGame.Engine.Networking;
using MyGame.Engine.Rendering;
using MyGame.Engine.Core;
using MyGame.Engine.Maps;
using MyGame.Gameplay.Components;
using MyGame.Gameplay.Prefabs;
using MyGame.GameStates.UI;
using MyGame.Gameplay.Systems;
using Flecs.NET.Core;
using Steamworks;

using XnaColor = Microsoft.Xna.Framework.Color;

namespace MyGame.GameStates;

public class GameplayState : GameState
{
    private readonly Flecs.NET.Core.World ecsWorld;
    private readonly int selectedClassId;
    private PauseMenuOverlay pauseMenu = null!;
    private Camera2D camera = null!;

    private LevelData loadedMapData = null!;

    private readonly bool isHostOrigin;

    public bool IsSimulationPaused => pauseMenu != null && pauseMenu.IsPaused;

    public GameplayState(Game1 game, StateManager stateManager, Flecs.NET.Core.World sharedWorld, int chosenClassId)
        : base(game, stateManager)
    {
        ecsWorld = sharedWorld;
        selectedClassId = chosenClassId;
        isHostOrigin = !SteamManager.KnownHostId.HasValue || SteamManager.KnownHostId.Value == SteamClient.SteamId;
    }

    public override void LoadContent()
    {
        pauseMenu = new PauseMenuOverlay(game, stateManager, isHostOrigin);
        camera = new Camera2D(game.GraphicsDevice);

        // ARCHITECTURE FIX: Set initial camera parameters securely before loading assets
        camera.Zoom = 2.5f;
        camera.Position = new Vector2(400, 300);

        loadedMapData = MapLoader.LoadLevel("Maps/GameWorld/Entrance.ldtkl");

        Entity localAvatar = PlayerFactory.CreateLocal(ecsWorld, selectedClassId);

        if (loadedMapData != null)
        {
            localAvatar.Set(new Position { X = loadedMapData.SpawnPoint.X, Y = loadedMapData.SpawnPoint.Y });
            if (localAvatar.Has<PhysicsBody>())
            {
                var body = localAvatar.Get<PhysicsBody>().Value;
                if (body != null)
                {
                    body.Position = new nkast.Aether.Physics2D.Common.Vector2(
                        loadedMapData.SpawnPoint.X / PlayerFactory.PixelsPerMeter,
                        loadedMapData.SpawnPoint.Y / PlayerFactory.PixelsPerMeter
                    );
                }
            }
        }

        if (SteamManager.IsSteamActive && SteamManager.CurrentLobby.HasValue)
        {
            if (isHostOrigin)
            {
                SteamManager.CurrentLobby.Value.SetData("GameState", "InGame");
            }

            var handshakePayload = new PlayerSpawnPacket
            {
                PacketType = PacketTypes.Spawn,
                CharacterClassId = selectedClassId,
                StartX = loadedMapData?.SpawnPoint.X ?? 400f,
                StartY = loadedMapData?.SpawnPoint.Y ?? 300f,
                EntityNetworkSequenceId = localAvatar.Get<NetworkId>().Value
            };

            int bufferSize = System.Runtime.InteropServices.Marshal.SizeOf<PlayerSpawnPacket>();
            byte[] handshakeBuffer = new byte[bufferSize];
            handshakePayload.SerializeTo(handshakeBuffer);

            foreach (var peer in SteamManager.CurrentLobby.Value.Members)
            {
                if (peer.Id == SteamClient.SteamId) continue;
                SteamNetworking.SendP2PPacket(peer.Id, handshakeBuffer, handshakeBuffer.Length, 1, P2PSend.Reliable);
            }
            Console.WriteLine("[Network Handshake]: Reliable introduction frame broadcast to session lobby.");
        }
    }

    public override void UnloadContent()
    {
        pauseMenu.Unload();

        var garbageCollectionList = new List<Entity>();
        using var cleanupQuery = ecsWorld.QueryBuilder().With<MatchEntityTag>().Build();
        cleanupQuery.Each((Entity e) => { garbageCollectionList.Add(e); });

        foreach (var entity in garbageCollectionList)
        {
            if (entity.IsAlive()) entity.Destruct();
        }

        game.PhysicsWorld.Clear();

        NetworkReceiverSystem.ClearShadows();
        NetworkIdGenerator.ResetSequence();
        Console.WriteLine("[Gameplay]: Active match simulation cleared safely via Native Deferred Teardown.");
    }

    public override void Update(GameTime gameTime)
    {
        if (!SteamManager.IsSteamActive || !SteamManager.CurrentLobby.HasValue)
        {
           if (!isHostOrigin)
           {
               Console.WriteLine("[Network]: Lost connection to Host. Evacuating to Main Menu.");
               SteamManager.LeaveLobby();
               stateManager.ChangeState(new MainMenuState(game, stateManager));
               return;
           }
        }

        pauseMenu.Update();

        if (!IsSimulationPaused)
        {
           float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

           game.PhysicsWorld.Step(dt);
           ecsWorld.Progress(dt);

           ecsWorld.Query<Position>().Each((Iter it, int row, ref Position pos) =>
           {
              Entity e = it.Entity(row);
              if (e.Has<LocalPlayerTag>())
              {
                 camera.Position = Vector2.Lerp(camera.Position, new Vector2(pos.X, pos.Y), 5f * dt);
              }
           });
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        game.GraphicsDevice.Clear(XnaColor.FromNonPremultiplied(30, 30, 30, 255));

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            null, null, null,
            camera.GetViewMatrix()
        );

        var viewport = game.GraphicsDevice.Viewport;

        // ARCHITECTURE FIX: Secure fallback math to prevent zero or negative camera scale division crashes
        float currentZoom = camera.Zoom <= 0f ? 1f : camera.Zoom;

        float viewLeft = camera.Position.X - (viewport.Width / 2f / currentZoom) - 64f;
        float viewRight = camera.Position.X + (viewport.Width / 2f / currentZoom) + 64f;
        float viewTop = camera.Position.Y - (viewport.Height / 2f / currentZoom) - 64f;
        float viewBottom = camera.Position.Y + (viewport.Height / 2f / currentZoom) + 64f;

        if (loadedMapData != null && loadedMapData.Tiles != null)
        {
            foreach (var tile in loadedMapData.Tiles)
            {
                // ARCHITECTURE FIX: Strict null texture boundary verification before drawing
                if (tile.Texture != null && !tile.Texture.IsDisposed)
                {
                    if (tile.Position.X >= viewLeft && tile.Position.X <= viewRight &&
                        tile.Position.Y >= viewTop && tile.Position.Y <= viewBottom)
                    {
                        spriteBatch.Draw(tile.Texture, tile.Position, tile.Source, XnaColor.White, 0f, Vector2.Zero, 1f, tile.Effects, 0f);
                    }
                }
            }
        }

        ecsWorld.Query<Position, CharacterClass>().Each((Iter it, int row, ref Position pos, ref CharacterClass cClass) =>
        {
            Entity e = it.Entity(row);
            XnaColor renderColor = cClass.Id == 0 ? XnaColor.Orange : XnaColor.Cyan;
            if (e.Has<RemotePlayerTag>()) renderColor = XnaColor.LightSkyBlue;

            Rectangle agentDestRect = new Rectangle((int)pos.X - 16, (int)pos.Y - 16, 32, 32);
            spriteBatch.Draw(AssetManager.WhitePixel, agentDestRect, renderColor);
        });

        spriteBatch.End();

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
        pauseMenu.Draw(spriteBatch);
        spriteBatch.End();
    }
}
