using System;
using Flecs.NET.Core;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;

namespace MyGame.Gameplay.Systems;

public static class NetworkCleanupSystem
{
	public static void Register(World world)
	{
		world.System<NetworkOwner, NetworkId>("NetworkDisconnectSweepSystem")
			.With<RemotePlayerTag>()
			.Kind(Ecs.PreUpdate)
			.Interval(1.0f)
			.Each((Iter it, int row, ref NetworkOwner owner, ref NetworkId netId) =>
			{
				Entity e = it.Entity(row);

				if (!SteamManager.CurrentLobby.HasValue)
				{
					NetworkReceiverSystem.RemoveShadow(netId.Value);
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
					Console.WriteLine($"[Network Sync]: Player {owner.Value} left the lobby. Purging native proxy.");

					NetworkReceiverSystem.RemoveShadow(netId.Value);
					e.Destruct();
				}
			});
	}
}
