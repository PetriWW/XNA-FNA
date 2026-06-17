using Flecs.NET.Core;
using MyGame.Gameplay.Components;

namespace MyGame.Gameplay.Systems;

public static class TransformSystems
{
	public static void Register(World world)
	{
		// Globally locks in the PreviousPosition for ALL entities at the start of the frame.
		// This allows the Draw loop to perfectly interpolate smooth camera/player movement.
		world.System<Position, PreviousPosition>("StorePreviousPositionSystem")
			.Kind(Ecs.PreUpdate)
			.Each((ref Position pos, ref PreviousPosition prevPos) =>
			{
				prevPos.X = pos.X;
				prevPos.Y = pos.Y;
			});
	}
}
