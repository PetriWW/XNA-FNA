using MyGame.Engine.Maps;

namespace MyGame.Gameplay.Components;

public struct MapLoadRequest
{
    public string MapPath;
    public int LocalClassId;
}

public struct MapInstance
{
    public LevelData Data;
}
