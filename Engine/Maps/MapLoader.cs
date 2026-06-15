using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.Core;
using MyGame.Gameplay.Prefabs;

using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace MyGame.Engine.Maps;

public class LevelData
{
    public Vector2 SpawnPoint { get; set; } = new Vector2(400, 300);
    public List<TileRenderData> Tiles { get; set; } = new();
}

public struct TileRenderData
{
    public Texture2D Texture;
    public Rectangle Source;
    public Vector2 Position;
    public SpriteEffects Effects;
}

public static class MapLoader
{
    public static LevelData LoadLevel(string assetPath)
    {
        string jsonContent = AssetManager.GetTextFile(assetPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        LdtkLevel targetLevel = JsonSerializer.Deserialize<LdtkLevel>(jsonContent, options)
                           ?? throw new Exception($"[MapLoader Error]: Failed to parse standalone LDtk level at {assetPath}");

        Console.WriteLine($"[MapLoader]: Parsing Separate Level Asset: {targetLevel.Identifier} ({targetLevel.PxWidth}x{targetLevel.PxHeight})");

        LevelData levelData = new LevelData();

        for (int i = targetLevel.LayerInstances.Length - 1; i >= 0; i--)
        {
            var layer = targetLevel.LayerInstances[i];

            if (layer.Type == "IntGrid") ParseCollisions(layer);
            if (layer.Type == "Entities") ParseEntities(layer, levelData);

            if (!string.IsNullOrEmpty(layer.TilesetRelPath))
            {
                string textureFileName = Path.GetFileName(layer.TilesetRelPath);
                Texture2D layerTexture = AssetManager.GetTexture($"Textures/{textureFileName}");

                var tilesToProcess = (layer.AutoLayerTiles != null && layer.AutoLayerTiles.Length > 0)
                                      ? layer.AutoLayerTiles
                                      : (layer.GridTiles ?? Array.Empty<LdtkTileInstance>());

                foreach (var t in tilesToProcess)
                {
                    SpriteEffects effect = SpriteEffects.None;
                    if (t.F == 1 || t.F == 3) effect |= SpriteEffects.FlipHorizontally;
                    if (t.F == 2 || t.F == 3) effect |= SpriteEffects.FlipVertically;

                    levelData.Tiles.Add(new TileRenderData
                    {
                        Texture = layerTexture,
                        Source = new Rectangle(t.Src[0], t.Src[1], layer.GridSize, layer.GridSize),
                        Position = new Vector2(t.Px[0], t.Px[1]),
                        Effects = effect
                    });
                }
            }
        }

        return levelData;
    }

    private static void ParseCollisions(LdtkLayerInstance layer)
    {
        int tileSize = layer.GridSize;
        int cellsWidth = layer.CellsWidth;

        if (layer.IntGridCsv == null) return;

        for (int i = 0; i < layer.IntGridCsv.Length; i++)
        {
            int tileValue = layer.IntGridCsv[i];
            if (tileValue == 1)
            {
                int x = i % cellsWidth;
                int y = i / cellsWidth;

                float physX = (x * tileSize + (tileSize / 2f)) / PlayerFactory.PixelsPerMeter;
                float physY = (y * tileSize + (tileSize / 2f)) / PlayerFactory.PixelsPerMeter;
                float physWidth = tileSize / PlayerFactory.PixelsPerMeter;
                float physHeight = tileSize / PlayerFactory.PixelsPerMeter;

                var wallBody = Game1.Instance.PhysicsWorld.CreateRectangle(
                    physWidth, physHeight, 1f, new AetherVector2(physX, physY)
                );

                if (wallBody.FixtureList.Count > 0)
                {
                    wallBody.FixtureList[0].Friction = 0.3f;
                }
            }
        }
    }

    private static void ParseEntities(LdtkLayerInstance layer, LevelData outputData)
    {
        if (layer.EntityInstances == null) return;

        foreach (var entity in layer.EntityInstances)
        {
            // ARCHITECTURE FIX: Aligned identifier matching exactly with 'Player' from LDtk configuration templates
            if (entity.Identifier == "Player")
            {
                outputData.SpawnPoint = new Vector2(entity.Px[0], entity.Px[1]);
            }
        }
    }
}
