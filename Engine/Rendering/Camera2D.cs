using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame.Engine.Rendering;

public class Camera2D
{
	public Vector2 Position { get; set; }
	public float Zoom { get; set; } = 1f;
	public float Rotation { get; set; } = 0f;

	private readonly GraphicsDevice _graphicsDevice;

	public Camera2D(GraphicsDevice graphicsDevice)
	{
		_graphicsDevice = graphicsDevice;
		Position = Vector2.Zero;
	}

	public Matrix GetViewMatrix()
	{
		Viewport viewport = _graphicsDevice.Viewport;

		return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *
		       Matrix.CreateRotationZ(Rotation) *
		       Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
		       Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0));
	}
}
