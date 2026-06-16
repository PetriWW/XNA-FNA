using Flecs.NET.Core;
using Steamworks;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;
using nkast.Aether.Physics2D.Dynamics;

using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace MyGame.Gameplay.Prefabs;

public static class PhysicsLayers
{
    public const Category Environment = Category.Cat1;
    public const Category LocalPlayer = Category.Cat2;
    public const Category RemotePlayer = Category.Cat3;
    public const Category EnemyAndProjectiles = Category.Cat4;
}

public static class PlayerFactory
{
    public const float PixelsPerMeter = 32f;

    public static Entity CreateLocal(Flecs.NET.Core.World world, int classId, float spawnX, float spawnY)
    {
        ulong uniqueId = NetworkIdGenerator.GetNext();
        string entityLookupName = $"p_{uniqueId}";

        float radius = 5f / PixelsPerMeter;
        float innerHeight = 14f / PixelsPerMeter;

        var initialPosition = new AetherVector2(spawnX / PixelsPerMeter, spawnY / PixelsPerMeter);
        var aetherBody = Game1.Instance.PhysicsWorld.CreateCapsule(innerHeight, radius, 1f, initialPosition, 0f, BodyType.Dynamic);

        aetherBody.FixedRotation = true;
        aetherBody.LinearDamping = 0f;

        if (aetherBody.FixtureList.Count > 0)
        {
            aetherBody.FixtureList[0].Friction = 0f;
            aetherBody.FixtureList[0].CollisionCategories = PhysicsLayers.LocalPlayer;
            aetherBody.FixtureList[0].CollidesWith = PhysicsLayers.Environment | PhysicsLayers.EnemyAndProjectiles;
        }

        return world.Entity(entityLookupName)
            .Add<LocalPlayerTag>()
            .Add<MatchEntityTag>()
            .Set(new LocalInput { AxisX = 0, AxisY = 0, JumpJustPressed = false })
            .Set(new GroundState { IsGrounded = false, CoyoteTimer = 0f })
            .Set(new Position { X = spawnX, Y = spawnY })
            .Set(new PreviousPosition { X = spawnX, Y = spawnY })
            .Set(new Velocity { X = 0, Y = 0 })
            .Set(new PreviousVelocity { X = 0, Y = 0 })
            .Set(new CharacterClass { Id = classId })
            .Set(new NetworkOwner { Value = SteamClient.SteamId })
            .Set(new NetworkId { Value = uniqueId })
            .Set(new PhysicsBody { Value = aetherBody });
    }

    public static Entity CreateRemote(Flecs.NET.Core.World world, string entityKey, PlayerTransformPacket packet, SteamId senderId)
    {
        float radius = 5f / PixelsPerMeter;
        float innerHeight = 14f / PixelsPerMeter;
        var startPos = new AetherVector2(packet.X / PixelsPerMeter, packet.Y / PixelsPerMeter);

        var aetherBody = Game1.Instance.PhysicsWorld.CreateCapsule(innerHeight, radius, 1f, startPos, 0f, BodyType.Kinematic);
        aetherBody.FixedRotation = true;

        if (aetherBody.FixtureList.Count > 0)
        {
            aetherBody.FixtureList[0].Friction = 0f;
            aetherBody.FixtureList[0].CollisionCategories = PhysicsLayers.RemotePlayer;
            aetherBody.FixtureList[0].CollidesWith = PhysicsLayers.Environment;
        }

        return world.Entity(entityKey)
            .Add<RemotePlayerTag>()
            .Add<MatchEntityTag>()
            .Set(new Position { X = packet.X, Y = packet.Y })
            .Set(new PreviousPosition { X = packet.X, Y = packet.Y })
            .Set(new TargetPosition { X = packet.X, Y = packet.Y })
            .Set(new Velocity { X = packet.Vx, Y = packet.Vy })
            .Set(new CharacterClass { Id = packet.CharacterClassId })
            .Set(new NetworkOwner { Value = senderId })
            .Set(new NetworkId { Value = packet.EntityNetworkSequenceId })
            .Set(new NetworkSequence { LatestSequence = packet.SequenceNumber, TimeSinceLastPacket = 0f })
            .Set(new PhysicsBody { Value = aetherBody });
    }
}
