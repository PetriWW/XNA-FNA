using System;
using Microsoft.Xna.Framework;
using ImGuiNET;
using Flecs.NET.Core;
using MyGame.Gameplay.Components;
using MyGame.Engine.Networking;

namespace MyGame.Engine.Debug;

public class DebugUIManager
{
    private ImGuiRenderer _imGuiRenderer = null!;

    public bool ShowProfiler = true;
    public bool ShowNetworkStats = true;
    public bool ShowPhysicsDebug = true;
    public bool ShowCombatDebug = true;

    private float _currentFps;
    private readonly float[] _frametimes = new float[60];
    private int _frameIndex = 0;

    public void Initialize(Game game)
    {
        _imGuiRenderer = new ImGuiRenderer(game);
        _imGuiRenderer.RebuildFontAtlas();
    }

    public void Draw(GameTime gameTime)
    {
        _imGuiRenderer.BeforeLayout(gameTime);

        if (ShowProfiler) DrawProfilerWindow(gameTime);
        if (ShowNetworkStats) DrawNetworkWindow();
        if (ShowPhysicsDebug) DrawPhysicsWindow();
        if (ShowCombatDebug) DrawCombatWindow();

        _imGuiRenderer.AfterLayout();
    }

    private void DrawProfilerWindow(GameTime gameTime)
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Engine Profiler", ref ShowProfiler))
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _frametimes[_frameIndex] = dt;
            _frameIndex = (_frameIndex + 1) % _frametimes.Length;

            float avgFrameTime = 0;
            foreach (var t in _frametimes) avgFrameTime += t;
            avgFrameTime /= _frametimes.Length;
            _currentFps = avgFrameTime > 0 ? 1f / avgFrameTime : 0;

            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), $"FPS: {Math.Round(_currentFps)}");
            ImGui.Text($"Frame Time: {Math.Round(avgFrameTime * 1000, 2)} ms");
            ImGui.PlotLines("##frametimes", ref _frametimes[0], _frametimes.Length, _frameIndex, "Frametime (ms)", 0f, 0.033f, new System.Numerics.Vector2(0, 40));

            long memoryBytes = GC.GetTotalMemory(false);
            ImGui.Text($"Managed Heap: {Math.Round(memoryBytes / 1048576.0, 2)} MB");
            ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f), $"GC Collections -> Gen0: {GC.CollectionCount(0)} | Gen1: {GC.CollectionCount(1)} | Gen2: {GC.CollectionCount(2)}");

            ImGui.Separator();

            World ecs = Game1.Instance.EcsWorld;
            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 1, 1), "ECS Architecture:");
            ImGui.Text($"Local Players: {ecs.Count<LocalPlayerTag>()}");
            ImGui.Text($"Remote Shadows: {ecs.Count<RemotePlayerTag>()}");
            ImGui.Text($"Active Projectiles: {ecs.Count<ProjectileTag>()}");
            ImGui.Text($"Entities with Health: {ecs.Count<Health>()}");
        }
        ImGui.End();
    }

    private void DrawNetworkWindow()
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 250), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Steamworks P2P Monitor", ref ShowNetworkStats))
        {
            ImGui.Text($"Lobby Status: {(SteamManager.CurrentLobby.HasValue ? "Connected" : "Offline")}");
            if (SteamManager.CurrentLobby.HasValue)
            {
                ImGui.Text($"Members: {SteamManager.CurrentLobby.Value.MemberCount}/4");
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.8f, 0, 1), $"Known Host ID: {SteamManager.KnownHostId?.Value}");
            }
        }
        ImGui.End();
    }

    private void DrawPhysicsWindow()
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 370), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Aether2D Debug", ref ShowPhysicsDebug))
        {
            ImGui.Text($"Active Rigidbodies: {Game1.Instance.PhysicsWorld.BodyList.Count}");
            ImGui.Text($"Gravity: {Game1.Instance.PhysicsWorld.Gravity.Y}");
        }
        ImGui.End();
    }

    private void DrawCombatWindow()
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 480), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Combat Sandbox", ref ShowCombatDebug))
        {
            ImGui.Text("Local Player Testing:");

            if (ImGui.Button("Fire Projectile (Facing Dir)"))
            {
                Game1.Instance.EcsWorld.QueryBuilder<Position, FacingDirection>().With<LocalPlayerTag>().Build().Each((ref Position pos, ref FacingDirection facing) =>
                {
                    Game1.Instance.EcsWorld.Entity().Set(new ProjectileSpawnRequest
                    {
                        StartX = pos.X + (15f * facing.Value),
                        StartY = pos.Y - 10f,
                        VelocityX = 500f * facing.Value,
                        VelocityY = 0f
                    });
                });
            }
        }
        ImGui.End();
    }
}
