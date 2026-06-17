using System;
using Flecs.NET.Core;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;
using MyGame.Engine.Core;

namespace MyGame.Gameplay.Systems;

public static class NetworkCleanupSystem
{
	public static void Register(World world)
	{
		world.System<NetworkOwner, NetworkId>("NetworkDisconnectSweepSystem")
			.With<RemotePlayerTag>()
			.Kind(Ecs.PreUpdate)
			.Interval(1.0f) // Runs once per second, very lightweight
			.Each((Iter it, int row, ref NetworkOwner owner, ref NetworkId netId) =>
			{
				Entity e = it.Entity(row);

				if (!SteamManager.CurrentLobby.HasValue)
				{
					// NetworkRegistry automatically unregisters this via its OnRemove observer
					e.Destruct();
					return;
				}

				bool isStillInLobby = false;
				foreach (var member in SteamManager.CurrentLobby.Value.Members)
				{
					if (member.Id == owner.Value)
					{
						isStillInLobby = true;
						break;
					}
				}

				if (!isStillInLobby)
				{
					EngineLogger.Log($"Player {owner.Value} left the lobby. Purging native proxy.", "NETWORK");
					// NetworkRegistry automatically unregisters this via its OnRemove observer
					e.Destruct();
				}
			});
	}
}
