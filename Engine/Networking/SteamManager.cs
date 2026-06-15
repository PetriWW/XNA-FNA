using System;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using MyGame.Engine.States;
using MyGame.GameStates;

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

            Console.WriteLine($"[Steam]: Connected securely via Facepunch wrapper setup as {SteamClient.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Steam Offline Error]: {ex.Message}");
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
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Steam Error]: Failed to create lobby - {ex.Message}");
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

                // ARCHITECTURE FIX: Separation of Concerns.
                // SteamManager no longer dictates the game rules. Everyone goes to the UI Lobby.
                StateManager.Instance.ChangeState(new CharacterSelectState(Game1.Instance, StateManager.Instance));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Steam Error]: Failed to join lobby - {ex.Message}");
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
                Console.WriteLine("[Steam]: Host disconnected from server. Forcing drop to Main Menu.");
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
        }
    }
}
