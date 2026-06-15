using System;
using Microsoft.Xna.Framework;
using ImGuiNET;

namespace MyGame.Engine.Debug;

public class DebugUIManager
{
	private ImGuiRenderer _imGuiRenderer = null!;

	// ARCHITECTURE FIX: Changed properties to public fields so they can be passed by 'ref' to ImGui
	public bool ShowNetworkStats = true;
	public bool ShowPhysicsDebug = true;

	public void Initialize(Game game)
	{
		_imGuiRenderer = new ImGuiRenderer(game);
		_imGuiRenderer.RebuildFontAtlas();
		Console.WriteLine("[Debug UI]: ImGui context initialized.");
	}

	public void Draw(GameTime gameTime)
	{
		_imGuiRenderer.BeforeLayout(gameTime);

		if (ShowNetworkStats) DrawNetworkWindow();
		if (ShowPhysicsDebug) DrawPhysicsWindow();

		_imGuiRenderer.AfterLayout();
	}

	private void DrawNetworkWindow()
	{
		ImGui.Begin("Network Monitor", ref ShowNetworkStats);
		ImGui.Text($"Lobby Status: {(Networking.SteamManager.CurrentLobby.HasValue ? "Connected" : "Offline")}");
		if (Networking.SteamManager.KnownHostId.HasValue)
		{
			ImGui.Text($"Host ID: {Networking.SteamManager.KnownHostId.Value}");
		}
		ImGui.End();
	}

	private void DrawPhysicsWindow()
	{
		ImGui.Begin("Physics Debug", ref ShowPhysicsDebug);
		ImGui.Text($"Bodies: {Game1.Instance.PhysicsWorld.BodyList.Count}");

		if (ImGui.Button("Spawn Lag Boxes"))
		{
			// Code to spawn 10 boxes here
		}
		ImGui.End();
	}
}
