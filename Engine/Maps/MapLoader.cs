using System;
using System.Text.Json;
using Microsoft.Xna.Framework;
using MyGame.Engine.Core;
using MyGame.Gameplay.Prefabs;

using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace MyGame.Engine.Maps;

public static class MapLoader
{
    public static Vector2? LoadLevel(string assetPath, string levelName)
    {
        string jsonContent = AssetManager.GetTextFile(assetPath);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        LdtkRoot mapData = JsonSerializer.Deserialize<LdtkRoot>(jsonContent, options)
                           ?? throw new Exception("Failed to parse LDtk JSON.");

        LdtkLevel? targetLevel = null;
        foreach (var level in mapData.Levels)
        {
            if (level.Identifier == levelName)
            {
                targetLevel = level;
                break;
            }
        }

        if (targetLevel == null) throw new Exception($"Level '{levelName}' not found in map data.");

        Console.WriteLine($"[MapLoader]: Parsing Level {targetLevel.Identifier} ({targetLevel.PxWidth}x{targetLevel.PxHeight})");

        Vector2? spawnLocation = null;

        foreach (var layer in targetLevel.LayerInstances)
        {
            if (layer.Type == "IntGrid")
            {
                ParseCollisions(layer);
            }
            else if (layer.Type == "Entities")
            {
                spawnLocation = ParseEntities(layer);
            }
        }

        return spawnLocation;
    }

    private static void ParseCollisions(LdtkLayerInstance layer)
    {
        int tileSize = layer.GridSize;
        int cellsWidth = layer.CellsWidth;

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

                // Create the static body block
                var wallBody = Game1.Instance.PhysicsWorld.CreateRectangle(
                    physWidth,
                    physHeight,
                    1f,
                    new AetherVector2(physX, physY)
                );

                // ARCHITECTURE FIX: Safe friction assignment using Aether v2.2.0 FixtureList constraints
                if (wallBody.FixtureList.Count > 0)
                {
                    wallBody.FixtureList[0].Friction = 0.3f;
                }
            }
        }
    }

    private static Vector2? ParseEntities(LdtkLayerInstance layer)
    {
        Vector2? spawnLocation = null;

        foreach (var entity in layer.EntityInstances)
        {
            if (entity.Identifier == "PlayerSpawn")
            {
                spawnLocation = new Vector2(entity.Px[0], entity.Px[1]);
            }
        }

        return spawnLocation;
    }
}
