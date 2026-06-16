using System;
using Microsoft.Xna.Framework;
using ImGuiNET;
using Flecs.NET.Core;
using MyGame.Gameplay.Components;

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

            long memoryBytes = GC.GetTotalMemory(false);
            ImGui.Text($"Managed Memory: {Math.Round(memoryBytes / 1048576.0, 2)} MB");

            ImGui.Separator();

            World ecs = Game1.Instance.EcsWorld;
            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 1, 1), "ECS Entities:");

            ImGui.Text($"Local Players: {ecs.Count<LocalPlayerTag>()}");
            ImGui.Text($"Remote Shadows: {ecs.Count<RemotePlayerTag>()}");
            ImGui.Text($"Projectiles: {ecs.Count<ProjectileTag>()}");
        }
        ImGui.End();
    }

    private void DrawNetworkWindow()
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 220), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Network Monitor", ref ShowNetworkStats))
        {
            ImGui.Text($"Lobby Status: {(Networking.SteamManager.CurrentLobby.HasValue ? "Connected" : "Offline")}");
            if (Networking.SteamManager.KnownHostId.HasValue)
            {
                ImGui.Text($"Host ID: {Networking.SteamManager.KnownHostId.Value}");
            }
        }
        ImGui.End();
    }

    private void DrawPhysicsWindow()
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 320), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Physics Debug", ref ShowPhysicsDebug))
        {
            ImGui.Text($"Active Rigidbodies: {Game1.Instance.PhysicsWorld.BodyList.Count}");
        }
        ImGui.End();
    }

    private void DrawCombatWindow()
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 420), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Combat & Spawning", ref ShowCombatDebug))
        {
            ImGui.Text("Local Player Actions:");

            if (ImGui.Button("Shoot Projectile (Right)"))
            {
                Game1.Instance.EcsWorld.QueryBuilder<Position>().With<LocalPlayerTag>().Build().Each((Entity e, ref Position pos) =>
                {
                    Game1.Instance.EcsWorld.Entity().Set(new ProjectileSpawnRequest
                    {
                        StartX = pos.X,
                        StartY = pos.Y - 10f,
                        VelocityX = 500f,
                        VelocityY = 0f
                    });
                });
            }

            if (ImGui.Button("Shoot Projectile (Left)"))
            {
                Game1.Instance.EcsWorld.QueryBuilder<Position>().With<LocalPlayerTag>().Build().Each((Entity e, ref Position pos) =>
                {
                    Game1.Instance.EcsWorld.Entity().Set(new ProjectileSpawnRequest
                    {
                        StartX = pos.X,
                        StartY = pos.Y - 10f,
                        VelocityX = -500f,
                        VelocityY = 0f
                    });
                });
            }
        }
        ImGui.End();
    }
}
