using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MyGame.Engine.States;
using MyGame.Engine.UI;
using MyGame.Engine.Core;
using MyGame.Engine.Networking;
using MyGame.Engine.Input;
using Steamworks;
using FontStashSharp;

namespace MyGame.GameStates.UI;

public class PauseMenuOverlay
{
    public bool IsPaused { get; private set; } = false;
    private int previousMemberCount = 0;

    private readonly Game1 game;
    private readonly StateManager stateManager;
    private readonly bool isHost;
    private readonly byte[] signalBuffer = new byte[1];

    private readonly Button continueButton;
    private readonly Button inviteButton;
    private readonly Button exitButton;

    private string networkStatusText = "Unknown Status";

    public PauseMenuOverlay(Game1 game, StateManager stateManager, bool isHost)
    {
        this.game = game;
        this.stateManager = stateManager;
        this.isHost = isHost;

        Texture2D uiTex = AssetManager.WhitePixel;

        continueButton = new Button(uiTex, Rectangle.Empty)
            { Text = "Continue", NormalColor = Color.DarkSlateGray, HoverColor = Color.SlateGray };

        inviteButton = new Button(uiTex, Rectangle.Empty)
            { Text = "Invite Friends", NormalColor = Color.DarkGreen, HoverColor = Color.Green };

        exitButton = new Button(uiTex, Rectangle.Empty)
            { Text = "Disconnect & Exit", NormalColor = Color.DarkRed, HoverColor = Color.Red };

        continueButton.OnClick += () => { TransmitPauseState(false); };

        inviteButton.OnClick += async () =>
        {
            if (this.isHost)
            {
                // ARCHITECTURE FIX: Lock button to prevent spam while waiting for Steam task to resolve
                inviteButton.IsEnabled = false;

                if (!SteamManager.CurrentLobby.HasValue)
                {
                    await SteamManager.CreateLobby();
                    SteamManager.CurrentLobby?.SetData("GameState", "InGame");
                }

                // ARCHITECTURE FIX: Only trigger the native Steam overlay if the lobby creation successfully finished
                if (SteamManager.CurrentLobby.HasValue)
                {
                    SteamManager.OpenInviteOverlay();
                }

                inviteButton.IsEnabled = true;
            }
        };

        exitButton.OnClick += () =>
        {
            SteamManager.LeaveLobby();
            stateManager.ChangeState(new MainMenuState(game, stateManager));
        };
    }

    public void Unload() { }

    public void Update()
    {
        ListenForNetworkSignals();
        UpdateNetworkStatusText();

        if (SteamManager.CurrentLobby.HasValue)
        {
            int currentMembers = SteamManager.CurrentLobby.Value.MemberCount;

            if (IsPaused && currentMembers < previousMemberCount) TransmitPauseState(false);
            if (IsPaused && currentMembers > previousMemberCount && isHost) TransmitPauseState(true);

            previousMemberCount = currentMembers;
        }

        if (InputManager.IsActionJustPressed(GameActions.Pause))
        {
            TransmitPauseState(!IsPaused);
        }

        if (IsPaused)
        {
            var viewport = game.GraphicsDevice.Viewport;
            int centerX = (viewport.Width / 2) - 125;
            int startY = (viewport.Height / 2) - 80;

            continueButton.Bounds = new Rectangle(centerX, startY, 250, 45);

            Point mousePos = InputManager.GetMousePosition();
            bool isPressed = InputManager.IsUISelectPressed();

            if (isHost)
            {
                inviteButton.Bounds = new Rectangle(centerX, startY + 60, 250, 45);
                exitButton.Bounds = new Rectangle(centerX, startY + 120, 250, 45);
                inviteButton.Update(mousePos, isPressed);
            }
            else
            {
                exitButton.Bounds = new Rectangle(centerX, startY + 60, 250, 45);
            }

            continueButton.Update(mousePos, isPressed);
            exitButton.Update(mousePos, isPressed);
        }
    }

    private void UpdateNetworkStatusText()
    {
        if (!SteamManager.IsSteamActive)
        {
            networkStatusText = "Network Offline (Local Solo)";
            return;
        }

        if (!SteamManager.CurrentLobby.HasValue)
        {
            networkStatusText = isHost ? "Playing Solo (Lobby Closed)" : "Connection to Host Lost!";
            return;
        }

        if (isHost)
            networkStatusText = $"Hosting Match: {SteamManager.CurrentLobby.Value.MemberCount} Players";
        else
            networkStatusText = "Connected to Host";
    }

    private void ListenForNetworkSignals()
    {
        while (SteamNetworking.IsP2PPacketAvailable(2))
        {
           var packetData = SteamNetworking.ReadP2PPacket(2);
           if (packetData.HasValue && packetData.Value.Data.Length > 0)
           {
              byte signal = packetData.Value.Data[0];
              if (signal == PacketTypes.PauseGame) IsPaused = true;
              else if (signal == PacketTypes.ResumeGame) IsPaused = false;
           }
        }
    }

    private void TransmitPauseState(bool enforcePause)
    {
        IsPaused = enforcePause;
        signalBuffer[0] = IsPaused ? PacketTypes.PauseGame : PacketTypes.ResumeGame;

        if (SteamManager.CurrentLobby.HasValue)
        {
           foreach (var member in SteamManager.CurrentLobby.Value.Members)
           {
              if (member.Id != SteamClient.SteamId)
              {
                 SteamNetworking.SendP2PPacket(member.Id, signalBuffer, signalBuffer.Length, 2, P2PSend.Reliable);
              }
           }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (IsPaused)
        {
            var viewport = game.GraphicsDevice.Viewport;
            spriteBatch.Draw(AssetManager.WhitePixel, viewport.Bounds, Color.Black * 0.85f);

            continueButton.Draw(spriteBatch);
            if (isHost) inviteButton.Draw(spriteBatch);
            exitButton.Draw(spriteBatch);

            if (AssetManager.IsFontLoaded)
            {
                SpriteFontBase font = AssetManager.GetFont(24f);
                var textSize = font.MeasureString(networkStatusText);
                System.Numerics.Vector2 textPos = new System.Numerics.Vector2(
                    (viewport.Width - textSize.X) / 2f,
                    (viewport.Height / 2f) - 150
                );

                FSColor fsColor = new FSColor(Color.LightSkyBlue.R, Color.LightSkyBlue.G, Color.LightSkyBlue.B, (byte)255);
                font.DrawText(AssetManager.FontRenderer, networkStatusText, textPos, fsColor);
            }
        }
    }
}
