using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.States;
using MyGame.Engine.UI;
using MyGame.Engine.Core;
using MyGame.Engine.Networking;
using Steamworks;

namespace MyGame.GameStates;

public class LobbyState : GameState
{
    private readonly byte[] signalBuffer = new byte[1];
    private readonly int selectedClassId;

    private Button leaveLobbyButton = null!;
    private Button startMatchButton = null!;

    public LobbyState(Game1 game, StateManager stateManager, int chosenClassId) : base(game, stateManager)
    {
        selectedClassId = chosenClassId;
    }

    public override void LoadContent()
    {
        Texture2D uiTex = AssetManager.WhitePixel;

        leaveLobbyButton = new Button(uiTex, Rectangle.Empty)
            { Text = "Leave Lobby", NormalColor = Color.Crimson, HoverColor = Color.Red };

        leaveLobbyButton.OnClick += () =>
        {
            SteamManager.LeaveLobby();
            stateManager.ChangeState(new MainMenuState(game, stateManager));
        };

        startMatchButton = new Button(uiTex, Rectangle.Empty);
        startMatchButton.OnClick += HandleStartGamePressed;
    }

    public override void Update(GameTime gameTime)
    {
        if (!SteamManager.IsSteamActive || !SteamManager.CurrentLobby.HasValue)
        {
            stateManager.ChangeState(new MainMenuState(game, stateManager));
            return;
        }

        ListenForLobbySignals();

        var viewport = game.GraphicsDevice.Viewport;
        leaveLobbyButton.Bounds = new Rectangle((viewport.Width / 2) - 100, (viewport.Height / 2) - 60, 200, 50);
        startMatchButton.Bounds = new Rectangle((viewport.Width / 2) - 100, (viewport.Height / 2) + 10, 200, 50);

        var lobby = SteamManager.CurrentLobby.Value;
        bool isHost = lobby.Owner.Id == SteamClient.SteamId;

        if (isHost)
        {
            startMatchButton.Text = "Start Match";
            startMatchButton.NormalColor = Color.ForestGreen;
            startMatchButton.HoverColor = Color.Green;
            startMatchButton.Update();
        }
        else
        {
            startMatchButton.Text = "Waiting for Host...";
            startMatchButton.NormalColor = Color.DarkSlateGray;
            startMatchButton.HoverColor = Color.DarkSlateGray;
        }

        leaveLobbyButton.Update();
    }

    private void ListenForLobbySignals()
    {
        while (SteamNetworking.IsP2PPacketAvailable(1))
        {
            var packetData = SteamNetworking.ReadP2PPacket(1);
            if (packetData.HasValue && packetData.Value.Data.Length > 0)
            {
                if (packetData.Value.Data[0] == 99)
                {
                    stateManager.ChangeState(new GameplayState(game, stateManager, game.EcsWorld, selectedClassId));
                }
            }
        }
    }

    private void HandleStartGamePressed()
    {
        if (!SteamManager.IsSteamActive || !SteamManager.CurrentLobby.HasValue) return;
        var lobby = SteamManager.CurrentLobby.Value;

        if (lobby.Owner.Id != SteamClient.SteamId) return;

        lobby.SetJoinable(false);
        signalBuffer[0] = 99;

        foreach (var member in lobby.Members)
        {
            if (member.Id == SteamClient.SteamId) continue;
            SteamNetworking.SendP2PPacket(member.Id, signalBuffer, signalBuffer.Length, 1, P2PSend.Reliable);
        }

        stateManager.ChangeState(new GameplayState(game, stateManager, game.EcsWorld, selectedClassId));
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        game.GraphicsDevice.Clear(new Color(20, 24, 32, 255));
        if (!SteamManager.IsSteamActive || !SteamManager.CurrentLobby.HasValue) return;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

        leaveLobbyButton.Draw(spriteBatch);
        startMatchButton.Draw(spriteBatch);

        spriteBatch.End();
    }
}
