using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using MyGame.Engine.UI;

namespace MyGame.Engine.Core;

public static class AssetManager
{
    private static GraphicsDevice _graphicsDevice = null!;
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static readonly FontSystem FontSystem = new();

    public static Texture2D WhitePixel { get; private set; } = null!;
    public static bool IsFontLoaded { get; private set; } = false;

    public static FNAFontRenderer FontRenderer { get; private set; } = null!;

    public static void Initialize(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        _graphicsDevice = graphicsDevice;
        WhitePixel = new Texture2D(_graphicsDevice, 1, 1);
        WhitePixel.SetData(new[] { Color.White });

        FontRenderer = new FNAFontRenderer(spriteBatch);
    }

    // ARCHITECTURE FIX: Bypasses .NET build caching limitations by normalizing paths
    // using AppContext.BaseDirectory. If assets are not found inside the active execution bin,
    // it seamlessly walks back out to pull the live files straight from your raw Content source workspace.
    private static string ResolveAssetPath(string assetPath)
    {
        string localizedPath = assetPath.Replace('/', Path.DirectorySeparatorChar);
        string baseDir = AppContext.BaseDirectory;

        // 1. Try the compiled execution directory (bin)
        string binPath = Path.GetFullPath(Path.Combine(baseDir, "Content", localizedPath));
        if (File.Exists(binPath)) return binPath;

        // 2. Fall back to raw source directories
        string sourcePath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Content", localizedPath));
        if (File.Exists(sourcePath)) return sourcePath;

        throw new FileNotFoundException($"[AssetManager]: Missing asset! Checked both runtime and source directories:\nBin: {binPath}\nSource: {sourcePath}");
    }

    public static Texture2D GetTexture(string assetPath)
    {
        if (Textures.TryGetValue(assetPath, out var existingTex)) return existingTex;

        string fullPath = ResolveAssetPath(assetPath);

        using var stream = File.OpenRead(fullPath);
        var newTex = Texture2D.FromStream(_graphicsDevice, stream);
        Textures[assetPath] = newTex;
        return newTex;
    }

    public static void LoadFont(string assetPath)
    {
        string fullPath = ResolveAssetPath(assetPath);

        byte[] ttfData = File.ReadAllBytes(fullPath);
        FontSystem.AddFont(ttfData);
        IsFontLoaded = true;
    }

    public static SpriteFontBase GetFont(float fontSize) => FontSystem.GetFont(fontSize);

    public static string GetTextFile(string assetPath)
    {
        string fullPath = ResolveAssetPath(assetPath);
        return File.ReadAllText(fullPath);
    }

    public static void UnloadAll()
    {
        foreach (var tex in Textures.Values)
        {
            if (!tex.IsDisposed) tex.Dispose();
        }
        Textures.Clear();

        FontSystem.Reset();

        if (WhitePixel != null && !WhitePixel.IsDisposed) WhitePixel.Dispose();
        IsFontLoaded = false;
    }
}
