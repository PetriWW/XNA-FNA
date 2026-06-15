using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.States;
using MyGame.Engine.Core;
using MyGame.Engine.Input;
using MyGame.Engine.Debug; // Included for ImGui Debug Controller tracking
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

    // ARCHITECTURE FIX: Clean float-based constant tracking for the accumulator boundaries
    private const float LogicTickRate = 1f / 60f;
    private double _timeAccumulator = 0.0;

    public static Game1 Instance { get; private set; } = null!;

    public FlecsWorld EcsWorld { get; private set; }
    public PhysicsWorld2D PhysicsWorld { get; private set; }
    public LiteDatabase LocalDatabase { get; private set; }

    // ARCHITECTURE ADDITION: ImGui Modular Interface Endpoint
    public DebugUIManager DebugUI { get; private set; } = null!;

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

        // ARCHITECTURE FIX: Unlocks FNA graphics rendering performance for high refresh rate monitors
        IsFixedTimeStep = false;
        graphics.SynchronizeWithVerticalRetrace = true;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        stateManager = new StateManager();
    }

    protected override void Initialize()
    {
        graphics.SynchronizeWithVerticalRetrace = SettingsManager.CurrentSettings.VSync;
        graphics.ApplyChanges();
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

        // ARCHITECTURE ADDITION: Load the interactive debug overlay window context
        DebugUI = new DebugUIManager();
        DebugUI.Initialize(this);

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
        InputManager.Update(); // Hardware polled cleanly ONCE at structural head of thread tick

        // ARCHITECTURE FIX: Implements Fixed Timestep Accumulator loop boundary logic
        _timeAccumulator += gameTime.ElapsedGameTime.TotalSeconds;

        // ARCHITECTURE FIX: The "Spiral of Death" Cap.
        // If the OS freezes the window for a long time, drop the lost frames instead of crashing the CPU.
        if (_timeAccumulator > LogicTickRate * 10)
        {
           _timeAccumulator = LogicTickRate * 10;
        }

        while (_timeAccumulator >= LogicTickRate)
        {
           stateManager.Update(LogicTickRate);
           _timeAccumulator -= LogicTickRate;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Prevent layout artifacts if state switches midway through rendering pipeline frames
        if (spriteBatch != null && !stateManager.IsTransitioning)
        {
            stateManager.Draw(spriteBatch);
        }

        base.Draw(gameTime);

        // ARCHITECTURE ADDITION: Render the ImGui system cleanly directly over the baseline game view elements
        if (spriteBatch != null && !stateManager.IsTransitioning)
        {
            DebugUI.Draw(gameTime);
        }
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
