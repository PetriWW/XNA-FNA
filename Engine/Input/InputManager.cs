using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace MyGame.Engine.Input;

public static class GameActions
{
	public const string MoveUp = "MoveUp";
	public const string MoveDown = "MoveDown";
	public const string MoveLeft = "MoveLeft";
	public const string MoveRight = "MoveRight";
	public const string Pause = "Pause";
	public const string Interact = "Interact";
}

public static class InputManager
{
	private static readonly Dictionary<string, Keys> KeyBindings = new()
	{
		{ GameActions.MoveUp, Keys.W },
		{ GameActions.MoveDown, Keys.S },
		{ GameActions.MoveLeft, Keys.A },
		{ GameActions.MoveRight, Keys.D },
		{ GameActions.Pause, Keys.Escape },
		{ GameActions.Interact, Keys.E }
	};

	public static bool IsActionActive(string action)
	{
		if (KeyBindings.TryGetValue(action, out Keys key))
		{
			return Keyboard.GetState().IsKeyDown(key);
		}
		return false;
	}
}
