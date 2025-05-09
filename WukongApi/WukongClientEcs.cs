using System.Numerics;
using b1;
using BtlShare;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong;
using UnrealEngine.Runtime;
using WukongApi.ECS;
using WukongApi.Patches;
using EntityManager = ReadyM.Relay.Common.ECS.EntityManager;

namespace WukongApi;

public sealed partial class WukongClient
{
    public readonly EntityManager entityManager;
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
            typeof(PeerIdComponent),
            typeof(TeamComponent),
            typeof(TranslationComponent)
        );
    }

    public void RunTickSystems()
    {
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
        entityManager.RunSystem((
            EntityId _,
            ref HpComponent hpComp,
            ref TamerComponent tamer,
            ref LocalDeathComponent localDeath,
            ref MarkerComponent marker) =>
        {
            if (hpComp.Hp <= 0 && !localDeath.killed)
            {
                localDeath.killed = true;

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
    }

    public EntityId RegisterMonster()
    {
        return entityManager.CreateEntity(monsterArchetype);
    }

    public ref T GetEntityComponent<T>(EntityId entity) where T : struct
    {
        return ref entityManager.GetComponent<T>(entity);
    }

    private void UpdateMarkerPositions()
    {
        entityManager.RunSystem((EntityId _, ref TamerComponent tamer, ref MarkerComponent marker, ref TranslationComponent trans) =>
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
            ref PeerIdComponent peerId,
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

            writer.Put(peerId.PeerId);

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
        while (reader.TryGetInt(out var peerId))
        {
            var entity = entityManager.GetEntityByPeerId(peerId);

            if (entity.HasValue)
            {
                Logging.LogDebug("Received delta for peer {Peer}", peerId);

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
            else
            {
                Logging.LogWarning("Entity not found in index for peer {Peer}", peerId);

                AnimationComponent.SkipDelta(RelayClient, reader);
                HpComponent.SkipDelta(RelayClient, reader);
                MonsterAnimationComponent.SkipDelta(RelayClient, reader);
                NicknameComponent.SkipDelta(RelayClient, reader);
                TeamComponent.SkipDelta(RelayClient, reader);
                TranslationComponent.SkipDelta(RelayClient, reader);
            }
        }
    }
}