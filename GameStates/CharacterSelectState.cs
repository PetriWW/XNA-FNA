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

namespace MyGame.GameStates;

public class CharacterSelectState : GameState
{
    private Button startRunButton = null!;
    private Button multiplayerButton = null!;
    private Button backButton = null!;

    private const int DefaultClassId = 0;

    private bool isJoineeReady = false;
    private int previousMemberCount = 0;

    public CharacterSelectState(Game1 game, StateManager stateManager) : base(game, stateManager) { }

    public override void LoadContent()
    {
        Texture2D uiTex = AssetManager.WhitePixel;

        startRunButton = new Button(uiTex, Rectangle.Empty);
        multiplayerButton = new Button(uiTex, Rectangle.Empty);
        backButton = new Button(uiTex, Rectangle.Empty)
            { Text = "Back to Menu", NormalColor = Color.DarkRed, HoverColor = Color.Red };

        startRunButton.OnClick += () =>
        {
            bool inLobby = SteamManager.CurrentLobby.HasValue;

            // ARCHITECTURE FIX: Safely unpack nullable Lobby Owner ID to fix CS8629 warnings
            bool isHost = !inLobby || (SteamManager.CurrentLobby?.Owner.Id == SteamClient.SteamId);

            if (isHost)
            {
                if (inLobby && SteamManager.CurrentLobby.HasValue)
                {
                    byte[] signalBuffer = new byte[] { PacketTypes.LobbyStart };
                    foreach (var member in SteamManager.CurrentLobby.Value.Members)
                    {
                        if (member.Id != SteamClient.SteamId)
                            SteamNetworking.SendP2PPacket(member.Id, signalBuffer, signalBuffer.Length, 2, P2PSend.Reliable);
                    }
                }
                stateManager.ChangeState(new GameplayState(game, stateManager, game.EcsWorld, DefaultClassId));
            }
            else
            {
                bool isMatchInProgress = SteamManager.CurrentLobby?.GetData("GameState") == "InGame";
                if (isMatchInProgress)
                {
                    stateManager.ChangeState(new GameplayState(game, stateManager, game.EcsWorld, DefaultClassId));
                }
                else if (SteamManager.KnownHostId.HasValue) // Safely unpack HostId
                {
                    isJoineeReady = !isJoineeReady;
                    byte[] readyBuffer = new byte[] { PacketTypes.PlayerReady };
                    SteamNetworking.SendP2PPacket(SteamManager.KnownHostId.Value, readyBuffer, readyBuffer.Length, 2, P2PSend.Reliable);
                }
            }
        };

        multiplayerButton.OnClick += async () =>
        {
            if (!SteamManager.CurrentLobby.HasValue)
            {
                multiplayerButton.IsEnabled = false;
                multiplayerButton.Text = "Creating Lobby...";
                await SteamManager.CreateLobby();
            }
            else
            {
                SteamManager.OpenInviteOverlay();
            }
        };

        backButton.OnClick += () =>
        {
            if (SteamManager.CurrentLobby.HasValue) SteamManager.LeaveLobby();
            stateManager.ChangeState(new MainMenuState(game, stateManager));
        };
    }

    public override void Update(GameTime gameTime)
    {
        ListenForLobbySignals();

        var viewport = game.GraphicsDevice.Viewport;
        int centerX = (viewport.Width / 2) - 100;
        int startY = (viewport.Height / 2) - 80;

        startRunButton.Bounds = new Rectangle(centerX, startY, 200, 50);
        multiplayerButton.Bounds = new Rectangle(centerX, startY + 70, 200, 50);
        backButton.Bounds = new Rectangle(centerX, startY + 140, 200, 50);

        bool inLobby = SteamManager.CurrentLobby.HasValue;

        // ARCHITECTURE FIX: Safe nullable unpacks
        bool isHost = !inLobby || (SteamManager.CurrentLobby?.Owner.Id == SteamClient.SteamId);
        bool isMatchInProgress = inLobby && (SteamManager.CurrentLobby?.GetData("GameState") == "InGame");
        int currentMembers = inLobby ? (SteamManager.CurrentLobby?.MemberCount ?? 1) : 1;

        if (currentMembers < previousMemberCount) isJoineeReady = false;
        previousMemberCount = currentMembers;

        if (!inLobby)
        {
            if (multiplayerButton.Text != "Creating Lobby...")
            {
                multiplayerButton.Text = "Host Multiplayer";
                multiplayerButton.IsEnabled = true;
            }
            multiplayerButton.NormalColor = Color.DarkGoldenrod;
            multiplayerButton.HoverColor = Color.Goldenrod;

            startRunButton.Text = "Start Solo Run";
            startRunButton.NormalColor = Color.DarkGreen;
            startRunButton.HoverColor = Color.Green;
            startRunButton.IsEnabled = true;
        }
        else if (isHost)
        {
            multiplayerButton.Text = "Invite Friends";
            multiplayerButton.NormalColor = Color.DarkGreen;
            multiplayerButton.HoverColor = Color.Green;
            multiplayerButton.IsEnabled = true;

            if (currentMembers == 1)
            {
                startRunButton.Text = "Start Match (Solo)";
                startRunButton.NormalColor = Color.DarkGreen;
                startRunButton.HoverColor = Color.Green;
                startRunButton.IsEnabled = true;
            }
            else if (!isJoineeReady)
            {
                startRunButton.Text = "Waiting for Joinee...";
                startRunButton.NormalColor = Color.DarkSlateGray;
                startRunButton.HoverColor = Color.DarkSlateGray;
                startRunButton.IsEnabled = false;
            }
            else
            {
                startRunButton.Text = "Start Match";
                startRunButton.NormalColor = Color.DarkGreen;
                startRunButton.HoverColor = Color.Green;
                startRunButton.IsEnabled = true;
            }
        }
        else
        {
            multiplayerButton.Text = "Connected to Host";
            multiplayerButton.NormalColor = Color.DarkSlateGray;
            multiplayerButton.HoverColor = Color.DarkSlateGray;
            multiplayerButton.IsEnabled = false;

            if (isMatchInProgress)
            {
                startRunButton.Text = "Join Ongoing Match";
                startRunButton.NormalColor = Color.DarkGoldenrod;
                startRunButton.HoverColor = Color.Goldenrod;
                startRunButton.IsEnabled = true;
            }
            else if (!isJoineeReady)
            {
                startRunButton.Text = "Ready Up";
                startRunButton.NormalColor = Color.DarkSlateBlue;
                startRunButton.HoverColor = Color.SlateBlue;
                startRunButton.IsEnabled = true;
            }
            else
            {
                startRunButton.Text = "Waiting for Host...";
                startRunButton.NormalColor = Color.DarkSlateGray;
                startRunButton.HoverColor = Color.DarkSlateGray;
                startRunButton.IsEnabled = false;
            }
        }

        Point mousePos = InputManager.GetMousePosition();
        bool isPressed = InputManager.IsUISelectPressed();

        startRunButton.Update(mousePos, isPressed);
        multiplayerButton.Update(mousePos, isPressed);
        backButton.Update(mousePos, isPressed);
    }

    private void ListenForLobbySignals()
    {
        if (!SteamManager.IsSteamActive) return;

        while (SteamNetworking.IsP2PPacketAvailable(2))
        {
            var packetData = SteamNetworking.ReadP2PPacket(2);
            if (packetData.HasValue && packetData.Value.Data.Length > 0)
            {
                byte signal = packetData.Value.Data[0];
                if (signal == PacketTypes.LobbyStart)
                {
                    stateManager.ChangeState(new GameplayState(game, stateManager, game.EcsWorld, DefaultClassId));
                }
                else if (signal == PacketTypes.PlayerReady)
                {
                    isJoineeReady = true;
                }
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        game.GraphicsDevice.Clear(Color.DarkSlateGray);
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

        startRunButton.Draw(spriteBatch);
        multiplayerButton.Draw(spriteBatch);
        backButton.Draw(spriteBatch);

        spriteBatch.End();
    }
}
