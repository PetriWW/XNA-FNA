using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MyGame.Engine.Core;
using FontStashSharp;

using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using NumericsVector2 = System.Numerics.Vector2;

namespace MyGame.Engine.UI;

public class Button
{
    private readonly Texture2D texture;
    private MouseState currentMouse;
    private MouseState previousMouse;

    public event Action? OnClick;

    public string Text { get; set; } = string.Empty;
    public float FontSize { get; set; } = 20f;
    public Rectangle Bounds { get; set; }

    public Color NormalColor { get; set; } = Color.DarkSlateBlue;
    public Color HoverColor { get; set; } = Color.SlateBlue;
    public Color TextColor { get; set; } = Color.White;

    public Button(Texture2D texture, Rectangle bounds)
    {
        this.texture = texture;
        this.Bounds = bounds;
    }

    public void Update()
    {
        previousMouse = currentMouse;
        currentMouse = Mouse.GetState();

        if (Bounds.Contains(currentMouse.X, currentMouse.Y))
        {
            if (currentMouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed)
            {
                OnClick?.Invoke();
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Color btnTint = Bounds.Contains(currentMouse.X, currentMouse.Y) ? HoverColor : NormalColor;

        spriteBatch.Draw(texture, Bounds, btnTint);

        if (!string.IsNullOrEmpty(Text) && AssetManager.IsFontLoaded)
        {
           SpriteFontBase font = AssetManager.GetFont(FontSize);
           NumericsVector2 textSize = font.MeasureString(Text);

           System.Numerics.Vector2 textPos = new System.Numerics.Vector2(
              Bounds.X + (Bounds.Width - textSize.X) * 0.5f,
              Bounds.Y + (Bounds.Height - textSize.Y) * 0.5f
           );

           FSColor fsColor = new FSColor(TextColor.R, TextColor.G, TextColor.B, TextColor.A);

           font.DrawText(AssetManager.FontRenderer, Text, textPos, fsColor);
        }
    }
}
