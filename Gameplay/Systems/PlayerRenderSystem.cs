using Flecs.NET.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.Core;
using MyGame.Gameplay.Components;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MyGame.Gameplay.Systems;

public static class PlayerRenderSystem
{
	private static Query<Position, PreviousPosition, CharacterClass, FacingDirection> _playerQuery;

	// ARCHITECTURE FIX: Static Cache prevents Closure Allocations
	private static SpriteBatch _batchCache = null!;
	private static float _alphaCache;

	public static void Initialize(World world)
	{
		_playerQuery = world.QueryBuilder<Position, PreviousPosition, CharacterClass, FacingDirection>().Build();
	}

	public static void Draw(SpriteBatch spriteBatch, float alpha)
	{
		_batchCache = spriteBatch;
		_alphaCache = alpha;

		_playerQuery.Each((Entity e, ref Position pos, ref PreviousPosition prevPos, ref CharacterClass cClass, ref FacingDirection facing) =>
		{
			XnaColor renderColor = cClass.Id == 0 ? XnaColor.Orange : XnaColor.Cyan;
			if (e.Has<RemotePlayerTag>()) renderColor = XnaColor.LightSkyBlue;

			float renderX = MathHelper.Lerp(prevPos.X, pos.X, _alphaCache);
			float renderY = MathHelper.Lerp(prevPos.Y, pos.Y, _alphaCache);

			SpriteEffects fx = facing.Value < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			_batchCache.Draw(
				AssetManager.WhitePixel,
				new Rectangle((int)renderX - 5, (int)renderY - 12, 10, 24),
				null,
				renderColor,
				0f,
				Vector2.Zero,
				fx,
				0f
			);
		});
	}
}
