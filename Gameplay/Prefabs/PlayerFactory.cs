using System;
using Flecs.NET.Core;
using Steamworks;
using MyGame.Engine.Networking;
using MyGame.Gameplay.Components;

using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace MyGame.Gameplay.Prefabs;

public static class PlayerFactory
{
    public const float PixelsPerMeter = 64f;

    public static Entity CreateLocal(Flecs.NET.Core.World world, int classId)
    {
        ulong uniqueId = NetworkIdGenerator.GetNext();

        string entityLookupName = $"p_{uniqueId}";

        float startX = 400f / PixelsPerMeter;
        float startY = 300f / PixelsPerMeter;
        float radiusInMeters = 16f / PixelsPerMeter;

        var initialPosition = new AetherVector2(startX, startY);

        var aetherBody = Game1.Instance.PhysicsWorld.CreateCircle(radiusInMeters, 1f, initialPosition, nkast.Aether.Physics2D.Dynamics.BodyType.Dynamic);

        aetherBody.FixedRotation = true;
        aetherBody.LinearDamping = 8f;

        return world.Entity(entityLookupName)
            .Add<LocalPlayerTag>()
            .Add<MatchEntityTag>()
            .Set(new LocalInput { AxisX = 0, AxisY = 0 })
            .Set(new Position { X = 400, Y = 300 })
            .Set(new Velocity { X = 0, Y = 0 })
            .Set(new CharacterClass { Id = classId })
            .Set(new NetworkOwner { Value = SteamClient.SteamId })
            .Set(new NetworkId { Value = uniqueId })
            .Set(new PhysicsBody { Value = aetherBody });
    }

    public static Entity CreateRemote(Flecs.NET.Core.World world, string entityKey, PlayerTransformPacket packet, SteamId senderId)
    {
        return world.Entity(entityKey)
            .Add<RemotePlayerTag>()
            .Add<MatchEntityTag>()
            .Set(new Position { X = packet.X, Y = packet.Y })
            .Set(new TargetPosition { X = packet.X, Y = packet.Y })
            .Set(new Velocity { X = packet.Vx, Y = packet.Vy })
            .Set(new CharacterClass { Id = packet.CharacterClassId })
            .Set(new NetworkOwner { Value = senderId })
            .Set(new NetworkId { Value = packet.EntityNetworkSequenceId })
            .Set(new NetworkSequence { LatestSequence = packet.SequenceNumber });
    }
}
