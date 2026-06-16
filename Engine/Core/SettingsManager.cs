using System;
using System.Linq;
using LiteDB;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame.Engine.Core;

public class EngineSettings
{
    public int Id { get; set; }
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;
    public int TargetFPS { get; set; } = 60; // 0 represents Unlimited
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
        ApplyDisplaySettings(CurrentSettings.Width, CurrentSettings.Height, CurrentSettings.Fullscreen, CurrentSettings.VSync, CurrentSettings.TargetFPS);
    }

    public static DisplayMode[] GetSupportedResolutions()
    {
        return GraphicsAdapter.DefaultAdapter.SupportedDisplayModes
            .Where(m => m.Width >= 1280)
            .OrderBy(m => m.Width)
            .ToArray();
    }

    public static void ApplyDisplaySettings(int width, int height, bool fullscreen, bool vsync, int targetFps)
    {
        CurrentSettings.Width = width;
        CurrentSettings.Height = height;
        CurrentSettings.Fullscreen = fullscreen;
        CurrentSettings.VSync = vsync;
        CurrentSettings.TargetFPS = targetFps;

        _settingsCollection.Update(CurrentSettings);

        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.IsFullScreen = fullscreen;
        _graphics.SynchronizeWithVerticalRetrace = vsync;

        if (targetFps == 0) // Unlimited
        {
            _game.IsFixedTimeStep = false;
        }
        else
        {
            _game.IsFixedTimeStep = true;
            _game.TargetElapsedTime = TimeSpan.FromSeconds(1d / targetFps);
        }

        _graphics.ApplyChanges();
    }
}
