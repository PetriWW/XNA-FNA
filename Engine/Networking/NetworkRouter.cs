using System;
using Steamworks;

namespace MyGame.Engine.Networking;

public static class NetworkRouter
{
	public static event Action? OnLobbyMatchStart;
	public static event Action? OnJoineeReady;
	public static event Action<bool>? OnPauseStateChanged;

	public static void RouteControlPackets()
	{
		if (!SteamManager.IsSteamActive) return;

		while (SteamNetworking.IsP2PPacketAvailable(2))
		{
			var packetData = SteamNetworking.ReadP2PPacket(2);
			if (packetData.HasValue && packetData.Value.Data.Length > 0)
			{
				byte signal = packetData.Value.Data[0];

				switch (signal)
				{
					case PacketTypes.LobbyStart:
						OnLobbyMatchStart?.Invoke();
						break;
					case PacketTypes.PlayerReady:
						OnJoineeReady?.Invoke();
						break;
					case PacketTypes.PauseGame:
						OnPauseStateChanged?.Invoke(true);
						break;
					case PacketTypes.ResumeGame:
						OnPauseStateChanged?.Invoke(false);
						break;
				}
			}
		}
	}
}
