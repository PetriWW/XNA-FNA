using System;
using LiteDB;
using Microsoft.Xna.Framework;

namespace MyGame.Engine.Core;

public class EngineSettings
{
    public int Id { get; set; }
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;
}

public static class SettingsManager
{
    private static Game1 _game = null!;
    private static GraphicsDeviceManager _graphics = null!;
    private static ILiteCollection<EngineSettings> _settingsCollection = null!;

    public static EngineSettings CurrentSettings { get; private set; } = new EngineSettings();

    public static void Initialize(Game1 game, GraphicsDeviceManager graphics)
    {
        _game = game;
        _graphics = graphics;

        _settingsCollection = game.LocalDatabase.GetCollection<EngineSettings>("settings");

        var savedSettings = _settingsCollection.FindById(1);
        if (savedSettings == null)
        {
            savedSettings = new EngineSettings { Id = 1 };
            _settingsCollection.Insert(savedSettings);
        }

        CurrentSettings = savedSettings;
        ApplyDisplaySettings(CurrentSettings.Width, CurrentSettings.Height, CurrentSettings.Fullscreen, CurrentSettings.VSync);
    }

    public static void ApplyDisplaySettings(int width, int height, bool fullscreen, bool vsync)
    {
        CurrentSettings.Width = width;
        CurrentSettings.Height = height;
        CurrentSettings.Fullscreen = fullscreen;
        CurrentSettings.VSync = vsync;

        _settingsCollection.Update(CurrentSettings);

        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.IsFullScreen = fullscreen;
        _graphics.SynchronizeWithVerticalRetrace = vsync;

        _game.IsFixedTimeStep = true;
        _game.TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);

        _graphics.ApplyChanges();
        Console.WriteLine($"[Settings Engine]: Video parameters applied and saved to LiteDB ({width}x{height}).");
    }
}
