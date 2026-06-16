using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.States;
using MyGame.Engine.Networking;
using MyGame.Engine.Rendering;
using MyGame.Engine.Core;
using MyGame.Gameplay.Components;
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
    private readonly string targetMapPath;

    private PauseMenuOverlay pauseMenu = null!;
    private Camera2D camera = null!;
    private RenderTarget2D virtualRenderTarget = null!;

    public const int VirtualWidth = 640;
    public const int VirtualHeight = 360;
    private readonly bool isHostOrigin;

    public bool IsSimulationPaused => pauseMenu != null && pauseMenu.IsPaused;

    public GameplayState(Game1 game, StateManager stateManager, Flecs.NET.Core.World sharedWorld, int chosenClassId, string mapPath)
        : base(game, stateManager)
    {
        ecsWorld = sharedWorld;
        selectedClassId = chosenClassId;
        targetMapPath = mapPath;
        isHostOrigin = !SteamManager.KnownHostId.HasValue || SteamManager.KnownHostId.Value == SteamClient.SteamId;
    }

    public override void LoadContent()
    {
        virtualRenderTarget = new RenderTarget2D(game.GraphicsDevice, VirtualWidth, VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None);
        pauseMenu = new PauseMenuOverlay(game, stateManager, isHostOrigin);

        camera = new Camera2D();
        camera.Zoom = 1.0f;

        if (isHostOrigin && SteamManager.IsSteamActive && SteamManager.CurrentLobby.HasValue)
        {
            SteamManager.CurrentLobby.Value.SetData("GameState", "InGame");
        }

        ecsWorld.Entity().Set(new MapLoadRequest { MapPath = targetMapPath, LocalClassId = selectedClassId });
    }

    public override void UnloadContent()
    {
        pauseMenu.Unload();

        var garbageCollectionList = new List<Entity>();
        using var cleanupQuery = ecsWorld.QueryBuilder().With<MatchEntityTag>().Build();
        cleanupQuery.Each((Entity e) => { garbageCollectionList.Add(e); });

        foreach (var entity in garbageCollectionList) if (entity.IsAlive()) entity.Destruct();

        ecsWorld.Entity("GlobalMapData").Destruct();

        virtualRenderTarget?.Dispose();
        AssetManager.UnloadLevelAssets();
        game.PhysicsWorld.Clear();
        NetworkReceiverSystem.ClearShadows();
    }

    public override void Update(GameTime gameTime)
    {
        if (!SteamManager.IsSteamActive || !SteamManager.CurrentLobby.HasValue)
        {
           if (!isHostOrigin)
           {
               SteamManager.LeaveLobby();
               stateManager.ChangeState(new MainMenuState(game, stateManager));
               return;
           }
        }

        pauseMenu.Update();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (!IsSimulationPaused)
        {
           ecsWorld.Query<Position, PreviousPosition>().Each((ref Position pos, ref PreviousPosition prevPos) => {
               prevPos.X = pos.X; prevPos.Y = pos.Y;
           });

           game.PhysicsWorld.Step(dt);
           ecsWorld.Progress(dt);
        }
    }

    public override void Draw(SpriteBatch spriteBatch, float alpha = 1f)
    {
        ecsWorld.Query<Position, PreviousPosition>().Each((Iter it, int row, ref Position pos, ref PreviousPosition prevPos) =>
        {
            Entity e = it.Entity(row);
            if (e.Has<LocalPlayerTag>())
            {
                camera.Position = new Vector2(
                    MathHelper.Lerp(prevPos.X, pos.X, alpha),
                    MathHelper.Lerp(prevPos.Y, pos.Y, alpha)
                );
            }
        });

        Entity mapDataE = ecsWorld.Entity("GlobalMapData");
        if (mapDataE.Has<MapInstance>())
        {
            var rData = mapDataE.Get<MapInstance>().Data;
            camera.Limits = new Rectangle(0, 0, rData.Width, rData.Height);
        }

        game.GraphicsDevice.SetRenderTarget(virtualRenderTarget);
        game.GraphicsDevice.Clear(XnaColor.FromNonPremultiplied(40, 35, 50, 255));

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, camera.GetViewMatrix(VirtualWidth, VirtualHeight));

        TileRenderSystem.Draw(ecsWorld, camera, VirtualWidth, VirtualHeight);

        ecsWorld.Query<Position, PreviousPosition, CharacterClass>().Each((Iter it, int row, ref Position pos, ref PreviousPosition prevPos, ref CharacterClass cClass) =>
        {
            Entity e = it.Entity(row);
            XnaColor renderColor = cClass.Id == 0 ? XnaColor.Orange : XnaColor.Cyan;
            if (e.Has<RemotePlayerTag>()) renderColor = XnaColor.LightSkyBlue;

            float renderX = MathHelper.Lerp(prevPos.X, pos.X, alpha);
            float renderY = MathHelper.Lerp(prevPos.Y, pos.Y, alpha);

            spriteBatch.Draw(AssetManager.WhitePixel, new Rectangle((int)renderX - 5, (int)renderY - 12, 10, 24), renderColor);
        });

        spriteBatch.End();

        game.GraphicsDevice.SetRenderTarget(null);
        game.GraphicsDevice.Clear(Color.Black);

        float scaleX = (float)game.GraphicsDevice.PresentationParameters.BackBufferWidth / VirtualWidth;
        float scaleY = (float)game.GraphicsDevice.PresentationParameters.BackBufferHeight / VirtualHeight;
        float scale = Math.Min(scaleX, scaleY);

        int newW = (int)(VirtualWidth * scale);
        int newH = (int)(VirtualHeight * scale);
        var destRect = new Rectangle((game.GraphicsDevice.PresentationParameters.BackBufferWidth - newW) / 2, (game.GraphicsDevice.PresentationParameters.BackBufferHeight - newH) / 2, newW, newH);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
        spriteBatch.Draw(virtualRenderTarget, destRect, Color.White);
        spriteBatch.End();

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
        pauseMenu.Draw(spriteBatch);
        spriteBatch.End();
    }
}
