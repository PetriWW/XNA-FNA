using System.Collections.Generic;
using Flecs.NET.Core;
using MyGame.Gameplay.Components;

namespace MyGame.Gameplay.Systems;

public static class NetworkRegistry
{
	private static readonly Dictionary<ulong, Entity> _registry = new();

	public static void Register(World world)
	{
		// Auto-cleans the registry if an entity is destroyed or disconnected
		world.Observer<NetworkId>("NetworkRegistryCleanup")
			.Event(Ecs.OnRemove)
			.Each((Entity e, ref NetworkId netId) =>
			{
				_registry.Remove(netId.Value);
			});
	}

	public static void Add(ulong netId, Entity entity)
	{
		_registry[netId] = entity;
	}

	public static Entity? GetEntity(ulong netId)
	{
		if (_registry.TryGetValue(netId, out Entity e) && e.IsAlive()) return e;
		return null;
	}

	public static void ClearAll() => _registry.Clear();
}
