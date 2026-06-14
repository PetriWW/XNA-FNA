using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.States;
using MyGame.Engine.UI;
using MyGame.Engine.Core;

namespace MyGame.GameStates;

public class OptionsState : GameState
{
    private Button res720pButton = null!;
    private Button res1080pButton = null!;
    private Button toggleFullscreenButton = null!;
    private Button backButton = null!;

    private bool _pendingFullscreen;

    public OptionsState(Game1 game, StateManager stateManager) : base(game, stateManager) { }

    public override void LoadContent()
    {
        Texture2D uiTex = AssetManager.WhitePixel;
        _pendingFullscreen = SettingsManager.CurrentSettings.Fullscreen;

        res720pButton = new Button(uiTex, Rectangle.Empty) { Text = "Set 1280 x 720" };
        res720pButton.OnClick += () => { SettingsManager.ApplyDisplaySettings(1280, 720, _pendingFullscreen, SettingsManager.CurrentSettings.VSync); };

        res1080pButton = new Button(uiTex, Rectangle.Empty) { Text = "Set 1920 x 1080" };
        res1080pButton.OnClick += () => { SettingsManager.ApplyDisplaySettings(1920, 1080, _pendingFullscreen, SettingsManager.CurrentSettings.VSync); };

        toggleFullscreenButton = new Button(uiTex, Rectangle.Empty) { Text = "Toggle Fullscreen" };
        toggleFullscreenButton.OnClick += () =>
        {
            _pendingFullscreen = !_pendingFullscreen;
            SettingsManager.ApplyDisplaySettings(SettingsManager.CurrentSettings.Width, SettingsManager.CurrentSettings.Height, _pendingFullscreen, SettingsManager.CurrentSettings.VSync);
        };

        backButton = new Button(uiTex, Rectangle.Empty) { Text = "Back", NormalColor = Color.DarkRed, HoverColor = Color.Red };
        backButton.OnClick += () => { stateManager.PopState(); };
    }

    public override void Update(GameTime gameTime)
    {
        var viewport = game.GraphicsDevice.Viewport;
        int centerX = (viewport.Width / 2) - 150;
        int startY = (viewport.Height / 2) - 120;

        res720pButton.Bounds = new Rectangle(centerX, startY, 300, 45);
        res1080pButton.Bounds = new Rectangle(centerX, startY + 60, 300, 45);
        toggleFullscreenButton.Bounds = new Rectangle(centerX, startY + 120, 300, 45);
        backButton.Bounds = new Rectangle(centerX, startY + 190, 300, 45);

        int currentWidth = SettingsManager.CurrentSettings.Width;

        res720pButton.NormalColor = currentWidth == 1280 ? Color.DarkGreen : Color.Black;
        res720pButton.HoverColor = currentWidth == 1280 ? Color.Green : Color.DarkGray;

        res1080pButton.NormalColor = currentWidth == 1920 ? Color.DarkGreen : Color.Black;
        res1080pButton.HoverColor = currentWidth == 1920 ? Color.Green : Color.DarkGray;

        toggleFullscreenButton.NormalColor = _pendingFullscreen ? Color.DarkGreen : Color.Black;
        toggleFullscreenButton.HoverColor = _pendingFullscreen ? Color.Green : Color.DarkGray;

        res720pButton.Update();
        res1080pButton.Update();
        toggleFullscreenButton.Update();
        backButton.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        game.GraphicsDevice.Clear(Color.DimGray);
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

        res720pButton.Draw(spriteBatch);
        res1080pButton.Draw(spriteBatch);
        toggleFullscreenButton.Draw(spriteBatch);
        backButton.Draw(spriteBatch);

        spriteBatch.End();
    }
}
