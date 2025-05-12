using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using b1;
using BtlShare;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.ECS.Components;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong;
using UnrealEngine.Runtime;
using WukongApi.ECS;
using WukongApi.Patches;

namespace WukongApi;

public sealed partial class WukongClient
{
    public readonly NetworkedEntityManager entityManager;
    public ArchetypeId monsterArchetype;

    private void DefineEcs()
    {
        monsterArchetype = entityManager.DefineArchetype(
            typeof(LocalDeathComponent),
            typeof(MarkerComponent),
            typeof(TamerComponent),
            typeof(AnimationComponent),
            typeof(HpComponent),
            typeof(MonsterAnimationComponent),
            typeof(NicknameComponent),
            typeof(NetworkIdComponent),
            typeof(TeamComponent),
            typeof(TranslationComponent)
        );

        entityManager.EntityDestroyed += OnNetworkedEntityDestroyed;
    }

    private void OnNetworkedEntityDestroyed(NetworkIdComponent obj)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.DestroyEntity);
        writer.Put(obj);
        RelayClient.OpRaiseEventRaw(writer, DeliveryMethod.ReliableOrdered);
    }

    public void RunTickSystems()
    {
        entityManager.RemoveQueuedEntities();

        SetCachedPlayerProperties(); // not a system
        UpdateMarkerPositions();
        CheckMonsterDeath();

        if (IsMasterClient)
        {
            SendMonsterArchetypeDelta();
        }
    }

    private void CheckMonsterDeath()
    {
        List<EntityId> deadEntities = [];

        entityManager.RunSystem((
            EntityId entityId,
            ref HpComponent hpComp,
            ref TamerComponent tamer,
            ref LocalDeathComponent localDeath,
            ref MarkerComponent marker) =>
        {
            if (hpComp.Hp <= 0 && !localDeath.killed)
            {
                Logging.LogDebug("Monster {Id} died", entityId);
                localDeath.killed = true;
                deadEntities.Add(entityId);

                var pawn = tamer.Pawn;
                var markerActor = marker.MarkerActor;

                var events = BUS_EventCollectionCS.Get(pawn);
                GameLoopPatch.QueueOnGameThread(() =>
                {
                    events.Evt_UnitDead.Invoke(pawn, EDeadReason.SkillDamage);
                    BGU_UnrealWorldUtil.DestroyActor(markerActor);
                }, "Evt_UnitDead");
            }
        });

        foreach (var deadEntity in deadEntities)
        {
            entityManager.QueueDestroyEntity(deadEntity);
        }
    }

    public EntityId CreateNetworkedEntity()
    {
        return entityManager.CreateNetworkedEntity(monsterArchetype, (short)RelayClient.LocalPlayer.PeerId);
    }

    public EntityId CreateNetworkedEntity(NetworkIdComponent netId)
    {
        return entityManager.CreateNetworkedEntity(monsterArchetype, netId);
    }

    public ref T GetEntityComponent<T>(EntityId entity) where T : struct
    {
        return ref entityManager.GetComponent<T>(entity);
    }

    private void UpdateMarkerPositions()
    {
        entityManager.RunSystem((EntityId _,
            ref TamerComponent tamer,
            ref MarkerComponent marker,
            ref TranslationComponent trans) =>
        {
            if (marker.MarkerActor == null)
                return;

            if (tamer.Pawn == null)
            {
                Logging.LogError("Pawn is null");
                return;
            }

            var markerHeight = tamer.Pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1f;
            marker.MarkerActor.SetActorLocation(trans.Position.ToFVector() + new FVector(0, 0, markerHeight), false, out var _, true);
        });
    }

    private void SendMonsterArchetypeDelta()
    {
        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.EcsUpdate);

        entityManager.RunSystem((
            EntityId _,
            ref AnimationComponent animation,
            ref HpComponent health,
            ref MonsterAnimationComponent monsterAnimation,
            ref NicknameComponent nickname,
            ref NetworkIdComponent peerId,
            ref TeamComponent team,
            ref TranslationComponent translation
        ) =>
        {
            var anyDirty = animation.IsDirty ||
                           health.IsDirty ||
                           monsterAnimation.IsDirty ||
                           nickname.IsDirty ||
                           team.IsDirty ||
                           translation.IsDirty;

            if (!anyDirty)
                return;

            writer.Put(peerId.Owner);
            writer.Put(peerId.Id);

            animation.WriteDelta(RelayClient, writer);
            animation.ClearDirty();

            health.WriteDelta(RelayClient, writer);
            health.ClearDirty();

            monsterAnimation.WriteDelta(RelayClient, writer);
            monsterAnimation.ClearDirty();

            nickname.WriteDelta(RelayClient, writer);
            nickname.ClearDirty();

            team.WriteDelta(RelayClient, writer);
            team.ClearDirty();

            translation.WriteDelta(RelayClient, writer);
            translation.ClearDirty();
        });

        RelayClient.OpRaiseEventRaw(writer, DeliveryMethod.Unreliable);
    }

    private void ApplyMonsterArchetypeDelta(NetDataReader reader)
    {
        while (reader.TryGetShort(out var owner))
        {
            var id = reader.GetUInt();
            var netId = new NetworkIdComponent(owner, id);
            var entity = entityManager.GetEntityByNetworkId(netId);

            if (!entity.HasValue || !entityManager.IsEntityAlive(entity.Value))
            {
                if (!entity.HasValue)
                {
                    Logging.LogWarning("Entity not found in index for {Id}", netId);
                }
                else
                {
                    Logging.LogWarning("Entity not alive for {Id}", netId);
                }

                AnimationComponent.SkipDelta(RelayClient, reader);
                HpComponent.SkipDelta(RelayClient, reader);
                MonsterAnimationComponent.SkipDelta(RelayClient, reader);
                NicknameComponent.SkipDelta(RelayClient, reader);
                TeamComponent.SkipDelta(RelayClient, reader);
                TranslationComponent.SkipDelta(RelayClient, reader);
            }
            else
            {
                Logging.LogDebug("Received delta for {Id}", netId);

                ref var animation = ref GetEntityComponent<AnimationComponent>(entity.Value);
                ref var health = ref GetEntityComponent<HpComponent>(entity.Value);
                ref var monsterAnimation = ref GetEntityComponent<MonsterAnimationComponent>(entity.Value);
                ref var nickname = ref GetEntityComponent<NicknameComponent>(entity.Value);
                ref var team = ref GetEntityComponent<TeamComponent>(entity.Value);
                ref var translation = ref GetEntityComponent<TranslationComponent>(entity.Value);

                animation.ReadDelta(RelayClient, reader);
                health.ReadDelta(RelayClient, reader);
                monsterAnimation.ReadDelta(RelayClient, reader);
                nickname.ReadDelta(RelayClient, reader);
                team.ReadDelta(RelayClient, reader);
                translation.ReadDelta(RelayClient, reader);
            }
        }
    }

    public BGUCharacterCS? GetPawnByNetworkId(NetworkIdComponent netId)
    {
        if (netId.Owner == -1)
        {
            var player = GetPlayerById((int)netId.Id);
            if (player != null)
                return player.Pawn;
        }

        var entity = entityManager.GetEntityByNetworkId(netId);
        if (entity.HasValue)
        {
            ref var tamer = ref GetEntityComponent<TamerComponent>(entity.Value);
            return tamer.Pawn;
        }

        return null;
    }
}