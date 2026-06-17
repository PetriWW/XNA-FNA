using Flecs.NET.Core;
using Steamworks;
using MyGame.Gameplay.Components;
using MyGame.Engine.Networking;
using nkast.Aether.Physics2D.Dynamics.Contacts;

namespace MyGame.Gameplay.Systems;

public static class LocalHitDetectionSystem
{
    public static void Register(World world)
    {
        world.System<PhysicsBody, Damage, NetworkOwner>("LocalHitDetectionSystem")
            .Kind(Ecs.PostUpdate)
            .With<ProjectileTag>()
            .Each((Iter it, int row, ref PhysicsBody pBody, ref Damage dmg, ref NetworkOwner owner) =>
            {
                if (pBody.Value == null) return;

                Entity projectileEntity = it.Entity(row);
                bool isMyProjectile = owner.Value == SteamClient.SteamId;

                ContactEdge ce = pBody.Value.ContactList;
                while (ce != null)
                {
                    if (ce.Contact.IsTouching)
                    {
                        var otherBody = ce.Contact.FixtureA.Body == pBody.Value ? ce.Contact.FixtureB.Body : ce.Contact.FixtureA.Body;

                        if (otherBody.Tag == null)
                        {
                            projectileEntity.Destruct();
                            return;
                        }

                        if (otherBody.Tag is ulong victimNetId)
                        {
                            // ARCHITECTURE FIX: Instant O(1) lookup. Zero Allocations.
                            Entity? target = NetworkRegistry.GetEntity(victimNetId);
                            if (target == null) return;

                            bool hitLocalPlayer = target.Value.Has<LocalPlayerTag>();
                            bool hitRemotePlayer = target.Value.Has<RemotePlayerTag>();

                            // RULE 1: STRICT DEFENDER AUTHORITY
                            if (hitLocalPlayer)
                            {
                                it.World().Entity().Set(new OutboundDistributedEvent
                                {
                                    TargetNetworkId = victimNetId,
                                    EventType = GameEventType.Damage,
                                    IntPayload = dmg.Amount
                                });
                                projectileEntity.Destruct();
                                return;
                            }

                            // RULE 2: GHOSTING (Ignore hits on Co-Op partner's shadow)
                            if (hitRemotePlayer)
                            {
                                projectileEntity.Destruct();
                                return;
                            }

                            // RULE 3: STRICT ATTACKER AUTHORITY
                            if (isMyProjectile && !hitLocalPlayer && !hitRemotePlayer)
                            {
                                it.World().Entity().Set(new OutboundDistributedEvent
                                {
                                    TargetNetworkId = victimNetId,
                                    EventType = GameEventType.Damage,
                                    IntPayload = dmg.Amount
                                });
                                projectileEntity.Destruct();
                                return;
                            }
                        }
                    }
                    ce = ce.Next;
                }
            });
    }
}
