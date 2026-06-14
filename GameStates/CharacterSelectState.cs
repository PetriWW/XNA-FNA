using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.States;
using MyGame.Engine.UI;
using MyGame.Engine.Core;
using MyGame.Engine.Networking;

namespace MyGame.GameStates;

public class CharacterSelectState : GameState
{
    private Button startRunButton = null!;
    private Button inviteButton = null!;
    private Button backButton = null!;

    private const int DefaultClassId = 0;

    public CharacterSelectState(Game1 game, StateManager stateManager) : base(game, stateManager) { }

    public override void LoadContent()
    {
        Texture2D uiTex = AssetManager.WhitePixel;

        startRunButton = new Button(uiTex, Rectangle.Empty)
            { Text = "Start Run", NormalColor = Color.DarkGreen, HoverColor = Color.Green };
        inviteButton = new Button(uiTex, Rectangle.Empty)
            { Text = "Invite Friends", NormalColor = Color.DarkGreen, HoverColor = Color.Green };
        backButton = new Button(uiTex, Rectangle.Empty)
            { Text = "Back", NormalColor = Color.DarkGreen, HoverColor = Color.Green };

        startRunButton.OnClick += () =>
        {
            stateManager.ChangeState(new GameplayState(game, stateManager, game.EcsWorld, DefaultClassId));
        };

        inviteButton.OnClick += () => SteamManager.OpenInviteOverlay();
        backButton.OnClick += () => stateManager.ChangeState(new MainMenuState(game, stateManager));
    }

    public override void Update(GameTime gameTime)
    {
        var viewport = game.GraphicsDevice.Viewport;
        int centerX = (viewport.Width / 2) - 100;
        int startY = (viewport.Height / 2) - 80;

        startRunButton.Bounds = new Rectangle(centerX, startY, 200, 50);
        inviteButton.Bounds = new Rectangle(centerX, startY + 70, 200, 50);
        backButton.Bounds = new Rectangle(centerX, startY + 140, 200, 50);

        startRunButton.Update();
        inviteButton.Update();
        backButton.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        game.GraphicsDevice.Clear(Color.DarkSlateGray);
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

        startRunButton.Draw(spriteBatch);
        inviteButton.Draw(spriteBatch);
        backButton.Draw(spriteBatch);

        spriteBatch.End();
    }
}
