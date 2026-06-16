using System;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using MyGame.Engine.States;
using MyGame.GameStates;
using MyGame.Engine.Core;

namespace MyGame.Engine.Networking;

public static class SteamManager
{
    public static bool IsSteamActive { get; private set; } = false;
    public static Lobby? CurrentLobby { get; private set; }
    public static SteamId? KnownHostId { get; private set; }

    public static void Initialize()
    {
        try
        {
            SteamClient.Init(480, true);
            IsSteamActive = true;

            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
            SteamNetworking.OnP2PSessionRequest += OnP2PSessionRequest;

            EngineLogger.Log($"Steam Client initialized successfully. Logged in as: {SteamClient.Name}", "STEAM");
        }
        catch (Exception ex)
        {
            EngineLogger.Log($"Steam subsystem offline or unavailable: {ex.Message}", "ERROR");
            IsSteamActive = false;
        }
    }

    private static void OnP2PSessionRequest(SteamId steamId)
    {
        if (CurrentLobby.HasValue)
        {
            foreach (var member in CurrentLobby.Value.Members)
            {
                if (member.Id == steamId)
                {
                    SteamNetworking.AllowP2PPacketRelay(true);
                    SteamNetworking.AcceptP2PSessionWithUser(steamId);
                    EngineLogger.Log($"Accepted standard P2P session with {steamId}", "NETWORK");
                    return;
                }
            }
        }
    }

    public static async Task CreateLobby()
    {
        if (!IsSteamActive) return;

        try
        {
            var lobbyTask = await SteamMatchmaking.CreateLobbyAsync(4);
            if (lobbyTask.HasValue)
            {
                CurrentLobby = lobbyTask.Value;
                CurrentLobby.Value.SetFriendsOnly();
                CurrentLobby.Value.SetJoinable(true);
                KnownHostId = SteamClient.SteamId;

                CurrentLobby.Value.SetData("GameState", "Lobby");
                EngineLogger.Log($"P2P Lobby successfully established. ID: {CurrentLobby.Value.Id}", "NETWORK");
            }
        }
        catch (Exception ex)
        {
            EngineLogger.LogError("Failed to instantiate Steamworks lobby", ex);
        }
    }

    public static void OpenInviteOverlay()
    {
        if (IsSteamActive && CurrentLobby.HasValue)
        {
            SteamFriends.OpenGameInviteOverlay(CurrentLobby.Value.Id);
        }
    }

    public static void LeaveLobby()
    {
        if (CurrentLobby.HasValue)
        {
            foreach (var member in CurrentLobby.Value.Members)
            {
                if (member.Id != SteamClient.SteamId)
                {
                    SteamNetworking.CloseP2PSessionWithUser(member.Id);
                }
            }
            CurrentLobby.Value.Leave();
            CurrentLobby = null;
            KnownHostId = null;
            EngineLogger.Log("Left lobby and cleaned up network session contexts.", "NETWORK");
        }
    }

    private static async void OnGameLobbyJoinRequested(Lobby lobby, SteamId friendId)
    {
        try
        {
            RoomEnter result = await lobby.Join();
            if (result == RoomEnter.Success)
            {
                CurrentLobby = lobby;
                KnownHostId = lobby.Owner.Id;
                EngineLogger.Log($"Lobby Join Request Approved. Routing client to selection screen.", "NETWORK");

                StateManager.Instance.ChangeState(new CharacterSelectState(Game1.Instance, StateManager.Instance));
            }
            else
            {
                EngineLogger.Log($"Matchmaking lobby rejection signal received: {result}", "WARNING");
            }
        }
        catch (Exception ex)
        {
            EngineLogger.LogError("Exception tracking lobby insertion request", ex);
        }
    }

    public static void Update()
    {
        if (!IsSteamActive) return;
        SteamClient.RunCallbacks();

        if (CurrentLobby.HasValue && KnownHostId.HasValue)
        {
            if (CurrentLobby.Value.Owner.Id != KnownHostId.Value)
            {
                EngineLogger.Log("Host disconnected. Forcing drop to Main Menu.", "NETWORK");
                LeaveLobby();
            }
        }
    }

    public static void Shutdown()
    {
        if (IsSteamActive)
        {
            LeaveLobby();
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
            SteamNetworking.OnP2PSessionRequest -= OnP2PSessionRequest;
            SteamClient.Shutdown();
            EngineLogger.Log("Steamworks client runtime completely finalized.", "STEAM");
        }
    }
}
