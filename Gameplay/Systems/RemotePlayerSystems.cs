using System;
using Flecs.NET.Core;
using Microsoft.Xna.Framework;
using MyGame.Gameplay.Components;

namespace MyGame.Gameplay.Systems;

public static class RemotePlayerSystems
{
	public static void Register(World world)
	{
		world.System<Position, TargetPosition, Velocity>("InterpolateRemotePlayersSystem")
			.With<RemotePlayerTag>()
			.Without<LocalPlayerTag>()
			.Each((Iter it, int row, ref Position pos, ref TargetPosition target, ref Velocity vel) =>
			{
				float dt = it.DeltaTime();

				target.X += vel.X * dt;
				target.Y += vel.Y * dt;

				pos.X = MathHelper.Lerp(pos.X, target.X, 15f * dt);
				pos.Y = MathHelper.Lerp(pos.Y, target.Y, 15f * dt);
			});
	}
}
