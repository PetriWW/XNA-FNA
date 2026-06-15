using Microsoft.Xna.Framework;
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
    private static KeyboardState currentKeyboard;
    private static KeyboardState previousKeyboard;
    private static MouseState currentMouse;
    private static MouseState previousMouse;

    private static readonly Dictionary<string, Keys> KeyBindings = new()
    {
       { GameActions.MoveUp, Keys.W },
       { GameActions.MoveDown, Keys.S },
       { GameActions.MoveLeft, Keys.A },
       { GameActions.MoveRight, Keys.D },
       { GameActions.Pause, Keys.Escape },
       { GameActions.Interact, Keys.E }
    };

    // ARCHITECTURE FIX: Centralized hardware processing loop
    public static void Update()
    {
        previousKeyboard = currentKeyboard;
        currentKeyboard = Keyboard.GetState();

        previousMouse = currentMouse;
        currentMouse = Mouse.GetState();
    }

    public static bool IsActionActive(string action)
    {
       if (KeyBindings.TryGetValue(action, out Keys key))
       {
          return currentKeyboard.IsKeyDown(key);
       }
       return false;
    }

    // ARCHITECTURE FIX: Concrete input boundaries for modular state access
    public static bool IsActionJustPressed(string action)
    {
       if (KeyBindings.TryGetValue(action, out Keys key))
       {
          return currentKeyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);
       }
       return false;
    }

    public static Point GetMousePosition() => new Point(currentMouse.X, currentMouse.Y);

    public static bool IsUISelectPressed() => currentMouse.LeftButton == ButtonState.Pressed;
}
