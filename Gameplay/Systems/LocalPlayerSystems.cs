using System;
using Flecs.NET.Core;
using MyGame.Engine.Input;
using MyGame.Gameplay.Components;
using MyGame.Gameplay.Prefabs;

using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace MyGame.Gameplay.Systems;

public static class LocalPlayerSystems
{
    private const float MoveSpeed = 8f;
    private const float JumpImpulse = 10f;

    public static void Register(World world)
    {
        world.Observer<PhysicsBody>()
            .Event(Ecs.OnRemove)
            .Each((ref PhysicsBody pBody) =>
            {
                if (pBody.Value != null && pBody.Value.World != null)
                {
                    Game1.Instance.PhysicsWorld.Remove(pBody.Value);
                }
            });

        world.System<LocalInput>("InputGatheringSystem")
            .Kind(Ecs.PreUpdate)
            .With<LocalPlayerTag>()
            .Each((ref LocalInput input) =>
            {
                if (!Game1.Instance.IsActive) return;

                float dx = 0;
                if (InputManager.IsActionActive(GameActions.MoveLeft))  dx -= 1;
                if (InputManager.IsActionActive(GameActions.MoveRight)) dx += 1;

                input.AxisX = dx;
            });

        world.System<LocalInput, PhysicsBody>("ApplyPhysicsInputSystem")
            .Kind(Ecs.OnUpdate)
            .With<LocalPlayerTag>()
            .Each((ref LocalInput input, ref PhysicsBody pBody) =>
            {
                // ARCHITECTURE FIX: Active null body verification guard block
                if (pBody.Value == null) return;

                var vel = pBody.Value.LinearVelocity;
                vel.X = input.AxisX * MoveSpeed;

                if (InputManager.IsActionJustPressed(GameActions.Jump) && Math.Abs(vel.Y) < 0.05f)
                {
                    vel.Y = -JumpImpulse;
                }

                pBody.Value.LinearVelocity = vel;
            });

        world.System<PhysicsBody, Position, Velocity>("SyncPhysicsToECSSystem")
            .Kind(Ecs.PostUpdate)
            .With<LocalPlayerTag>()
            .Each((ref PhysicsBody pBody, ref Position pos, ref Velocity vel) =>
            {
               // ARCHITECTURE FIX: Protect frame step synchronizer from uninitialized native proxies
               if (pBody.Value == null) return;

               pos.X = pBody.Value.Position.X * PlayerFactory.PixelsPerMeter;
               pos.Y = pBody.Value.Position.Y * PlayerFactory.PixelsPerMeter;
               vel.X = pBody.Value.LinearVelocity.X * PlayerFactory.PixelsPerMeter;
               vel.Y = pBody.Value.LinearVelocity.Y * PlayerFactory.PixelsPerMeter;
            });
    }
}
