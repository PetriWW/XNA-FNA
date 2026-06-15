using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.States;
using MyGame.Engine.Networking;
using MyGame.Engine.Rendering;
using MyGame.Engine.Core;
using MyGame.Engine.Maps; // Included for access to MapLoader execution
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
        camera.Position = new Vector2(400, 300);

        // ARCHITECTURE UPGRADE: Feed spatial parameters dynamically using the new LDtk pipeline parser return path
        Vector2 spawnPoint = MapLoader.LoadLevel("Maps/Level1.json", "Level_0") ?? new Vector2(400f, 300f);

        Entity localAvatar = PlayerFactory.CreateLocal(ecsWorld, selectedClassId);

        // Re-align internal ECS representation safely to structural parsing configuration boundaries
        localAvatar.Set(new Position { X = spawnPoint.X, Y = spawnPoint.Y });
        if (localAvatar.Has<PhysicsBody>())
        {
            localAvatar.Get<PhysicsBody>().Value.Position =
                new nkast.Aether.Physics2D.Common.Vector2(spawnPoint.X / PlayerFactory.PixelsPerMeter, spawnPoint.Y / PlayerFactory.PixelsPerMeter);
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
                StartX = spawnPoint.X,
                StartY = spawnPoint.Y,
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

        // Clean out passive physics fixtures attached to the runtime level before shifting maps
        var physicalGarbageList = new List<nkast.Aether.Physics2D.Dynamics.Body>(game.PhysicsWorld.BodyList);
        foreach (var body in physicalGarbageList)
        {
            game.PhysicsWorld.Remove(body);
        }

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
           // ARCHITECTURE FIX: Absolute deterministic simulation lock.
           // Both engines use the exact float passed from the fixed accumulator step bounds.
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
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null, null, null,
            camera.GetViewMatrix()
        );

        // ARCHITECTURE FIX: Viewport Frustum Culling limits loops down to visible bounds only
        var viewport = game.GraphicsDevice.Viewport;
        int viewWidth = viewport.Width;
        int viewHeight = viewport.Height;

        int startX = (int)(camera.Position.X - viewWidth / 2f) / 100 * 100 - 100;
        int endX = (int)(camera.Position.X + viewWidth / 2f) / 100 * 100 + 100;
        int startY = (int)(camera.Position.Y - viewHeight / 2f) / 100 * 100 - 100;
        int endY = (int)(camera.Position.Y + viewHeight / 2f) / 100 * 100 + 100;

        for (int x = startX; x < endX; x += 100)
        {
            for (int y = startY; y < endY; y += 100)
            {
                spriteBatch.Draw(AssetManager.WhitePixel, new Rectangle(x, y, 4, 4), XnaColor.DimGray);
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

        // Separate user interface frame processing explicitly from space coordinate transformations
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
        pauseMenu.Draw(spriteBatch);
        spriteBatch.End();
    }
}
