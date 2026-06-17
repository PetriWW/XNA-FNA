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
    private const float CoyoteTimeMax = 0.15f;

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

        world.System<LocalInput, FacingDirection>("InputGatheringSystem")
            .Kind(Ecs.PreUpdate)
            .With<LocalPlayerTag>()
            .Each((ref LocalInput input, ref FacingDirection facing) =>
            {
                if (!Game1.Instance.IsActive) return;

                float dx = 0;
                if (InputManager.IsActionActive(GameActions.MoveLeft))  dx -= 1;
                if (InputManager.IsActionActive(GameActions.MoveRight)) dx += 1;

                input.AxisX = dx;

                // Track visual orientation independently of velocity
                if (dx != 0) facing.Value = dx > 0 ? 1 : -1;

                if (InputManager.ConsumeAction(GameActions.Jump))
                {
                    input.JumpJustPressed = true;
                }
            });

        world.System<PhysicsBody, GroundState>("GroundDetectionSystem")
            .Kind(Ecs.PreUpdate)
            .With<LocalPlayerTag>()
            .Each((Iter it, int i, ref PhysicsBody pBody, ref GroundState ground) =>
            {
                if (pBody.Value == null) return;

                bool hasFloorContact = false;
                var ce = pBody.Value.ContactList;
                while (ce != null)
                {
                    // ARCHITECTURE FIX: Validates verticality. Prevents "wall-jumping" by rubbing flat vertical walls.
                    if (ce.Contact.IsTouching && Math.Abs(pBody.Value.LinearVelocity.Y) <= 0.1f)
                    {
                        hasFloorContact = true;
                    }
                    ce = ce.Next;
                }

                if (hasFloorContact)
                {
                    ground.IsGrounded = true;
                    ground.CoyoteTimer = CoyoteTimeMax;
                }
                else
                {
                    ground.IsGrounded = false;
                    ground.CoyoteTimer -= it.DeltaTime();
                }
            });

        world.System<LocalInput, PhysicsBody, GroundState>("ApplyPhysicsInputSystem")
            .Kind(Ecs.OnUpdate)
            .With<LocalPlayerTag>()
            .Each((ref LocalInput input, ref PhysicsBody pBody, ref GroundState ground) =>
            {
                if (pBody.Value == null) return;

                var vel = pBody.Value.LinearVelocity;
                vel.X = input.AxisX * MoveSpeed;

                if (input.JumpJustPressed)
                {
                    if (ground.CoyoteTimer > 0f)
                    {
                        vel.Y = -JumpImpulse;
                        ground.CoyoteTimer = 0f;
                    }
                    input.JumpJustPressed = false;
                }

                pBody.Value.LinearVelocity = vel;
            });

        world.System<PhysicsBody, Position, Velocity>("SyncPhysicsToECSSystem")
            .Kind(Ecs.PostUpdate)
            .With<LocalPlayerTag>()
            .Each((ref PhysicsBody pBody, ref Position pos, ref Velocity vel) =>
            {
               if (pBody.Value == null) return;

               pos.X = pBody.Value.Position.X * PlayerFactory.PixelsPerMeter;
               pos.Y = pBody.Value.Position.Y * PlayerFactory.PixelsPerMeter;
               vel.X = pBody.Value.LinearVelocity.X * PlayerFactory.PixelsPerMeter;
               vel.Y = pBody.Value.LinearVelocity.Y * PlayerFactory.PixelsPerMeter;
            });
    }
}
