using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MyGame.Engine.States;
using MyGame.Engine.UI; // Engine agnostic tools imported here
using MyGame.Engine.Core;
using MyGame.Engine.Networking;
using Steamworks;

namespace MyGame.GameStates.UI;

public class PauseMenuOverlay
{
    public bool IsPaused { get; private set; } = false;

    private readonly Game1 game;
    private readonly StateManager stateManager;
    private readonly byte[] signalBuffer = new byte[1];

    private readonly Button continueButton;
    private readonly Button exitButton;

    private KeyboardState previousKeyboardState;

    public PauseMenuOverlay(Game1 game, StateManager stateManager)
    {
        this.game = game;
        this.stateManager = stateManager;

        Texture2D uiTex = AssetManager.WhitePixel;

        continueButton = new Button(uiTex, Rectangle.Empty)
            { Text = "Continue", NormalColor = Color.DarkSlateGray, HoverColor = Color.SlateGray };
        exitButton = new Button(uiTex, Rectangle.Empty)
            { Text = "Exit to Menu", NormalColor = Color.DarkRed, HoverColor = Color.Red };

        continueButton.OnClick += () => { TransmitPauseState(false); };
        exitButton.OnClick += () =>
        {
            SteamManager.LeaveLobby();
            stateManager.ChangeState(new MainMenuState(game, stateManager));
        };
    }

    public void Unload() { }

    public void Update()
    {
        var currentKeyboardState = Keyboard.GetState();
        ListenForNetworkSignals();

        if (currentKeyboardState.IsKeyDown(Keys.Escape) && previousKeyboardState.IsKeyUp(Keys.Escape))
        {
            TransmitPauseState(!IsPaused);
        }

        if (IsPaused)
        {
            var viewport = game.GraphicsDevice.Viewport;
            int centerX = (viewport.Width / 2) - 100;
            int startY = (viewport.Height / 2) - 60;

            continueButton.Bounds = new Rectangle(centerX, startY, 200, 50);
            exitButton.Bounds = new Rectangle(centerX, startY + 80, 200, 50);

            continueButton.Update();
            exitButton.Update();
        }

        previousKeyboardState = currentKeyboardState;
    }

    private void ListenForNetworkSignals()
    {
        while (SteamNetworking.IsP2PPacketAvailable(1))
        {
            var packetData = SteamNetworking.ReadP2PPacket(1);
            if (packetData.HasValue && packetData.Value.Data.Length > 0)
            {
                byte signal = packetData.Value.Data[0];
                if (signal == 98) IsPaused = true;
                else if (signal == 97) IsPaused = false;
            }
        }
    }

    private void TransmitPauseState(bool enforcePause)
    {
        IsPaused = enforcePause;
        signalBuffer[0] = IsPaused ? (byte)98 : (byte)97;

        if (SteamManager.CurrentLobby.HasValue)
        {
            foreach (var member in SteamManager.CurrentLobby.Value.Members)
            {
                if (member.Id != SteamClient.SteamId)
                {
                    SteamNetworking.SendP2PPacket(member.Id, signalBuffer, 1, 1, P2PSend.Reliable);
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (IsPaused)
        {
            spriteBatch.Draw(AssetManager.WhitePixel, game.GraphicsDevice.Viewport.Bounds, Color.Black * 0.7f);
            continueButton.Draw(spriteBatch);
            exitButton.Draw(spriteBatch);
        }
    }
}
