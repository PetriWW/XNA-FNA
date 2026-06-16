using System;
using Flecs.NET.Core;
using Microsoft.Xna.Framework;
using MyGame.Gameplay.Components;
using MyGame.Gameplay.Prefabs;

namespace MyGame.Gameplay.Systems;

public static class RemotePlayerSystems
{
	private const float TeleportSnapThreshold = 3f * PlayerFactory.PixelsPerMeter;

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

				float distX = target.X - pos.X;
				float distY = target.Y - pos.Y;
				float distanceSq = (distX * distX) + (distY * distY);

				if (distanceSq > TeleportSnapThreshold * TeleportSnapThreshold)
				{
					pos.X = target.X;
					pos.Y = target.Y;
				}
				else
				{
					pos.X = MathHelper.Lerp(pos.X, target.X, 15f * dt);
					pos.Y = MathHelper.Lerp(pos.Y, target.Y, 15f * dt);
				}
			});
	}
}
