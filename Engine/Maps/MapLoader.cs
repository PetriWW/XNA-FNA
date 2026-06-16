using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyGame.Engine.Core;

namespace MyGame.Engine.Maps;

public struct RectangleF { public float X, Y, Width, Height; }

public class LevelData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Vector2 SpawnPoint { get; set; } = new Vector2(100, 100);

    public TileRenderData[] Tiles { get; set; } = Array.Empty<TileRenderData>();
    public RectangleF[] Collisions { get; set; } = Array.Empty<RectangleF>();
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
    public static LevelData LoadSingleLevel(string assetPath)
    {
        string jsonContent = AssetManager.GetTextFile(assetPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        LdtkRoot targetRoot = JsonSerializer.Deserialize<LdtkRoot>(jsonContent, options) ?? throw new Exception($"[MapLoader Error]");

        LdtkLevel levelData = targetRoot.Levels[0];

        if (!string.IsNullOrEmpty(levelData.ExternalRelPath))
        {
            string dir = Path.GetDirectoryName(assetPath) ?? "";
            string externalPath = Path.Combine(dir, levelData.ExternalRelPath).Replace('\\', '/');
            levelData = JsonSerializer.Deserialize<LdtkLevel>(AssetManager.GetTextFile(externalPath), options)!;
        }

        LevelData roomData = new LevelData
        {
            Width = levelData.PxWidth,
            Height = levelData.PxHeight
        };

        var tempTiles = new List<TileRenderData>();
        var tempCollisions = new List<RectangleF>();

        for (int i = levelData.LayerInstances.Length - 1; i >= 0; i--)
        {
            var layer = levelData.LayerInstances[i];

            if (layer.Type == "IntGrid")
                ParseCollisions(layer, tempCollisions);

            if (layer.Type == "Entities")
                ParseEntities(layer, roomData);

            if (!string.IsNullOrEmpty(layer.TilesetRelPath))
            {
                Texture2D layerTexture = AssetManager.GetTexture($"Textures/{Path.GetFileName(layer.TilesetRelPath)}", true);
                var tiles = (layer.AutoLayerTiles != null && layer.AutoLayerTiles.Length > 0) ? layer.AutoLayerTiles : (layer.GridTiles ?? Array.Empty<LdtkTileInstance>());

                foreach (var t in tiles)
                {
                    SpriteEffects effect = SpriteEffects.None;
                    if (t.F == 1 || t.F == 3) effect |= SpriteEffects.FlipHorizontally;
                    if (t.F == 2 || t.F == 3) effect |= SpriteEffects.FlipVertically;

                    tempTiles.Add(new TileRenderData {
                        Texture = layerTexture,
                        Source = new Rectangle(t.Src[0], t.Src[1], layer.GridSize, layer.GridSize),
                        Position = new Vector2(t.Px[0], t.Px[1]),
                        Effects = effect
                    });
                }
            }
        }

        roomData.Tiles = tempTiles.ToArray();
        roomData.Collisions = tempCollisions.ToArray();

        return roomData;
    }

    private static void ParseCollisions(LdtkLayerInstance layer, List<RectangleF> tempCollisions)
    {
        int tileSize = layer.GridSize;
        int cols = layer.CellsWidth;
        int rows = layer.IntGridCsv.Length / cols;
        bool[,] solidMap = new bool[cols, rows];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (layer.IntGridCsv[y * cols + x] == 1) solidMap[x, y] = true;
            }
        }

        for (int y = 0; y < rows; y++)
        {
            int startX = -1;
            for (int x = 0; x < cols; x++)
            {
                if (solidMap[x, y])
                {
                    if (startX == -1) startX = x;
                }
                else if (startX != -1)
                {
                    tempCollisions.Add(new RectangleF {
                        X = startX * tileSize,
                        Y = y * tileSize,
                        Width = (x - startX) * tileSize,
                        Height = tileSize
                    });
                    startX = -1;
                }
            }
            if (startX != -1)
            {
                tempCollisions.Add(new RectangleF {
                    X = startX * tileSize,
                    Y = y * tileSize,
                    Width = (cols - startX) * tileSize,
                    Height = tileSize
                });
            }
        }
    }

    private static void ParseEntities(LdtkLayerInstance layer, LevelData roomData)
    {
	    if (layer.EntityInstances == null) return;
	    foreach (var entity in layer.EntityInstances)
	    {
		    if (entity.Identifier == "Player")
		    {
			    // LDtk px is the top-left of the entity.
			    // We use the exact pixel coordinates from LDtk.
			    float spawnX = (float)entity.Px[0];
			    float spawnY = (float)entity.Px[1];

			    roomData.SpawnPoint = new Vector2(spawnX, spawnY);
			    break;
		    }
	    }
    }
}
