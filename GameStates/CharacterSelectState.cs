using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    private const string TargetMapPath = "Maps/GameWorld.ldtk";

    public CharacterSelectState(Game1 game, StateManager stateManager) : base(game, stateManager) { }

    public override void LoadContent()
    {
        Texture2D uiTex = AssetManager.WhitePixel;

        startRunButton = new Button(uiTex, Rectangle.Empty);
        multiplayerButton = new Button(uiTex, Rectangle.Empty);
        backButton = new Button(uiTex, Rectangle.Empty) { Text = "Back to Menu", NormalColor = Color.DarkRed, HoverColor = Color.Red };

        NetworkRouter.OnLobbyMatchStart += HandleLobbyMatchStart;
        NetworkRouter.OnJoineeReady += HandleJoineeReady;

        startRunButton.OnClick += () =>
        {
            bool inLobby = SteamManager.CurrentLobby.HasValue;
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
                stateManager.ChangeState(new GameplayState(game, stateManager, game.EcsWorld, DefaultClassId, TargetMapPath));
            }
            else
            {
                bool isMatchInProgress = SteamManager.CurrentLobby?.GetData("GameState") == "InGame";
                if (isMatchInProgress)
                {
                    stateManager.ChangeState(new GameplayState(game, stateManager, game.EcsWorld, DefaultClassId, TargetMapPath));
                }
                else if (SteamManager.KnownHostId.HasValue)
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

    public override void UnloadContent()
    {
        NetworkRouter.OnLobbyMatchStart -= HandleLobbyMatchStart;
        NetworkRouter.OnJoineeReady -= HandleJoineeReady;
    }

    private void HandleLobbyMatchStart() => stateManager.ChangeState(new GameplayState(game, stateManager, game.EcsWorld, DefaultClassId, TargetMapPath));
    private void HandleJoineeReady() => isJoineeReady = true;

    public override void Update(GameTime gameTime)
    {
        var viewport = game.GraphicsDevice.Viewport;
        int centerX = (viewport.Width / 2) - 100;
        int startY = (viewport.Height / 2) - 80;

        startRunButton.Bounds = new Rectangle(centerX, startY, 200, 50);
        multiplayerButton.Bounds = new Rectangle(centerX, startY + 70, 200, 50);
        backButton.Bounds = new Rectangle(centerX, startY + 140, 200, 50);

        bool inLobby = SteamManager.CurrentLobby.HasValue;
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

        bool isClicked = InputManager.ConsumeUIClick();

        startRunButton.Update(mousePos, isClicked);
        multiplayerButton.Update(mousePos, isClicked);
        backButton.Update(mousePos, isClicked);
    }

    public override void Draw(SpriteBatch spriteBatch, float alpha = 1f)
    {
        game.GraphicsDevice.Clear(Color.DarkSlateGray);
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

        startRunButton.Draw(spriteBatch);
        multiplayerButton.Draw(spriteBatch);
        backButton.Draw(spriteBatch);

        spriteBatch.End();
    }
}
