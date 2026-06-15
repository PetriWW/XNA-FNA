using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.Core;
using FontStashSharp;

using NumericsVector2 = System.Numerics.Vector2;

namespace MyGame.Engine.UI;

public class Button
{
    private readonly Texture2D texture;
    private bool wasLeftButtonPressed;

    public event Action? OnClick;

    public string Text { get; set; } = string.Empty;
    public float FontSize { get; set; } = 20f;
    public Rectangle Bounds { get; set; }

    public Color NormalColor { get; set; } = Color.DarkSlateBlue;
    public Color HoverColor { get; set; } = Color.SlateBlue;
    public Color TextColor { get; set; } = Color.White;

    public bool IsEnabled { get; set; } = true;
    public bool IsHovered { get; private set; } = false;

    public Button(Texture2D texture, Rectangle bounds)
    {
        this.texture = texture;
        this.Bounds = bounds;
    }

    // ARCHITECTURE FIX: Inversion of Control. The containing state feeds button metrics safely.
    public void Update(Point mousePosition, bool isLeftButtonPressed)
    {
        if (!IsEnabled) return;

        IsHovered = Bounds.Contains(mousePosition);

        if (IsHovered && !isLeftButtonPressed && wasLeftButtonPressed)
        {
            OnClick?.Invoke();
        }

        wasLeftButtonPressed = isLeftButtonPressed;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Color btnTint = !IsEnabled ? Color.DimGray : (IsHovered ? HoverColor : NormalColor);
        spriteBatch.Draw(texture, Bounds, btnTint);

        if (!string.IsNullOrEmpty(Text) && AssetManager.IsFontLoaded)
        {
           SpriteFontBase font = AssetManager.GetFont(FontSize);
           NumericsVector2 textSize = font.MeasureString(Text);

           System.Numerics.Vector2 textPos = new System.Numerics.Vector2(
              Bounds.X + (Bounds.Width - textSize.X) * 0.5f,
              Bounds.Y + (Bounds.Height - textSize.Y) * 0.5f
           );

           Color finalTextColor = !IsEnabled ? Color.Gray : TextColor;
           FSColor fsColor = new FSColor(finalTextColor.R, finalTextColor.G, finalTextColor.B, (byte)255);

           font.DrawText(AssetManager.FontRenderer, Text, textPos, fsColor);
        }
    }
}
