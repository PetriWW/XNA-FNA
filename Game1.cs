using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.States;
using MyGame.Engine.Core;
using MyGame.GameStates;
using MyGame.Gameplay.Systems;
using LiteDB;
using MyGame.Engine.Networking;

using FlecsWorld = Flecs.NET.Core.World;
using PhysicsWorld2D = nkast.Aether.Physics2D.Dynamics.World;

namespace MyGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager graphics;
    private SpriteBatch? spriteBatch;
    private readonly StateManager stateManager;

    public static Game1 Instance { get; private set; } = null!;

    public FlecsWorld EcsWorld { get; private set; }
    public PhysicsWorld2D PhysicsWorld { get; private set; }
    public LiteDatabase LocalDatabase { get; private set; }

    public Game1()
    {
        Instance = this;
        EcsWorld = FlecsWorld.Create();
        PhysicsWorld = new PhysicsWorld2D(new nkast.Aether.Physics2D.Common.Vector2(0f, 0f));

        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyGame", "SaveData.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        LocalDatabase = new LiteDatabase(dbPath);

        graphics = new GraphicsDeviceManager(this)
        {
           PreferredBackBufferWidth = 1280,
           PreferredBackBufferHeight = 720
        };

        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        stateManager = new StateManager();
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        AssetManager.Initialize(GraphicsDevice, spriteBatch);
        SettingsManager.Initialize(this, graphics);

        try
        {
           AssetManager.LoadFont("Fonts/DefaultFont.ttf");
        }
        catch (System.Exception ex)
        {
           System.Console.WriteLine($"[Engine Warning]: Font load failed. Text rendering disabled. {ex.Message}");
        }

        LocalPlayerSystems.Register(EcsWorld);
        RemotePlayerSystems.Register(EcsWorld);
        NetworkReceiverSystem.Register(EcsWorld);
        NetworkBroadcastSystem.Register(EcsWorld);
        NetworkCleanupSystem.Register(EcsWorld);

        stateManager.ChangeState(new MainMenuState(this, stateManager));
    }

    protected override void Update(GameTime gameTime)
    {
        SteamManager.Update();
        stateManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (spriteBatch != null)
        {
            stateManager.Draw(spriteBatch);
        }
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            spriteBatch?.Dispose();
            SteamManager.Shutdown();
            EcsWorld.Dispose();
            LocalDatabase.Dispose();
            AssetManager.UnloadAll();
        }
        base.Dispose(disposing);
    }
}
