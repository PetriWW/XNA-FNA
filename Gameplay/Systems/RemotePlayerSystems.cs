using System;
using Flecs.NET.Core;
using Microsoft.Xna.Framework;
using MyGame.Gameplay.Components;

namespace MyGame.Gameplay.Systems;

public static class RemotePlayerSystems
{
	public static void Register(World world)
	{
		world.System<Position, TargetPosition, Velocity, NetworkSequence>("InterpolateRemotePlayersSystem")
			.With<RemotePlayerTag>()
			.Without<LocalPlayerTag>()
			.Each((Iter it, int row, ref Position pos, ref TargetPosition target, ref Velocity vel, ref NetworkSequence seq) =>
			{
				float dt = it.DeltaTime();

				seq.TimeSinceLastPacket += dt;
				if (seq.TimeSinceLastPacket > 0.25f)
				{
					vel.X = 0;
					vel.Y = 0;
				}

				target.X += vel.X * dt;
				target.Y += vel.Y * dt;

				pos.X = MathHelper.Lerp(pos.X, target.X, 15f * dt);
				pos.Y = MathHelper.Lerp(pos.Y, target.Y, 15f * dt);
			});
	}
}
