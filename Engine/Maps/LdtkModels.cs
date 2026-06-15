using System.Text.Json.Serialization;

namespace MyGame.Engine.Maps;

public class LdtkRoot
{
	[JsonPropertyName("levels")]
	public LdtkLevel[] Levels { get; set; } = null!;
}

public class LdtkLevel
{
	[JsonPropertyName("identifier")]
	public string Identifier { get; set; } = string.Empty;

	[JsonPropertyName("pxWid")]
	public int PxWidth { get; set; }

	[JsonPropertyName("pxHei")]
	public int PxHeight { get; set; }

	[JsonPropertyName("layerInstances")]
	public LdtkLayerInstance[] LayerInstances { get; set; } = null!;
}

public class LdtkLayerInstance
{
	[JsonPropertyName("__identifier")]
	public string Identifier { get; set; } = string.Empty;

	[JsonPropertyName("__type")]
	public string Type { get; set; } = string.Empty;

	[JsonPropertyName("__gridSize")]
	public int GridSize { get; set; }

	[JsonPropertyName("__cWid")]
	public int CellsWidth { get; set; }

	[JsonPropertyName("intGridCsv")]
	public int[] IntGridCsv { get; set; } = null!;

	[JsonPropertyName("entityInstances")]
	public LdtkEntityInstance[] EntityInstances { get; set; } = null!;
}

public class LdtkEntityInstance
{
	[JsonPropertyName("__identifier")]
	public string Identifier { get; set; } = string.Empty;

	[JsonPropertyName("px")]
	public int[] Px { get; set; } = null!;
}
