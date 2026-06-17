using Flecs.NET.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.Core;
using MyGame.Gameplay.Components;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MyGame.Gameplay.Systems;

public static class ProjectileRenderSystem
{
	private static Query<Position, PreviousPosition> _projectileQuery;

	public static void Initialize(World world)
	{
		_projectileQuery = world.QueryBuilder<Position, PreviousPosition>().With<ProjectileTag>().Build();
	}

	public static void Draw(SpriteBatch spriteBatch, float alpha)
	{
		_projectileQuery.Each((ref Position pos, ref PreviousPosition prevPos) =>
		{
			float renderX = MathHelper.Lerp(prevPos.X, pos.X, alpha);
			float renderY = MathHelper.Lerp(prevPos.Y, pos.Y, alpha);

			spriteBatch.Draw(
				AssetManager.WhitePixel,
				new Rectangle((int)renderX - 4, (int)renderY - 4, 8, 8),
				XnaColor.Yellow
			);
		});
	}
}
