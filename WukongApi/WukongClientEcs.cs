using b1;
using BtlShare;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.ECS.Components;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.ECS;
using WukongApi.Patches;

namespace WukongApi;

public sealed partial class WukongClient
{
    public readonly NetworkedEntityManager EntityManager;
    public ArchetypeId MonsterArchetype;

    private void DefineEcs()
    {
        MonsterArchetype = EntityManager.DefineArchetype(
            typeof(MarkerComponent),
            typeof(LocalTamerComponent),
            typeof(TamerComponent),
            typeof(AnimationComponent),
            typeof(HpComponent),
            typeof(MonsterAnimationComponent),
            typeof(NicknameComponent),
            typeof(NetworkIdComponent),
            typeof(TeamComponent),
            typeof(TranslationComponent)
        );

        EntityManager.OnEntityCreated += OnNetworkedEntityCreated;
        EntityManager.OnEntityDestroyed += OnNetworkedEntityDestroyed;
    }

    private void OnNetworkedEntityCreated(NetworkIdComponent obj)
    {
        Logging.LogDebug("Networked entity created: {Id}", obj);
    }

    private void OnNetworkedEntityDestroyed(NetworkIdComponent netId)
    {
        if (netId.Owner == RelayClient.LocalPlayer.PeerId)
        {
            // our own entity - send destroy event
            Logging.LogDebug("Networked entity destroyed: {Id} (owned)", netId);
            var writer = new NetDataWriter();
            writer.Put((byte)SystemEvent.DestroyEntity);
            writer.Put(netId);
            RelayClient.OpRaiseEventRaw(writer, DeliveryMethod.ReliableOrdered);
        }
        else
        {
            // remote entity, dissolve it locally
            var entity = EntityManager.GetEntityByNetworkId(netId);
            if (entity.HasValue)
            {
                Logging.LogDebug("Networked entity destroyed: {Id} (remote)", netId);
                var tamerComponent = EntityManager.GetComponent<LocalTamerComponent>(entity.Value);

                if (tamerComponent.Pawn != null)
                {
                    Logging.LogDebug("Dissolving pawn {Pawn}", tamerComponent.Pawn);
                    BUS_EventCollectionCS.Get(tamerComponent.Pawn).Evt_TriggerDeadDissolve.Invoke();
                }
                else
                {
                    Logging.LogError("Pawn is null for entity {Id}", netId);
                }

                EntityManager.QueueDestroyEntity(entity.Value);
            }
            else
            {
                Logging.LogError("Received destroy event for locally non-existent entity: {Id}", netId);
            }
        }
    }

    public void RunTickSystems()
    {
        EntityManager.DestroyQueuedEntities();

        SetCachedPlayerProperties(); // not a system

        SyncTamers();
        UpdateMarkerPositions();
        DestroyDeadMonsterMarkers();
        SyncMonsters();

        if (IsMasterClient)
        {
            SendMonsterArchetypeDelta();
        }
    }

    private void SyncTamers()
    {
        EntityManager.RunSystem((
            EntityId _,
            ref TamerComponent tamer,
            ref LocalTamerComponent localTamer) =>
        {
            if (!localTamer.IsSynced)
            {
                bool found = false;
                var allTamers = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
                foreach (var actor in allTamers)
                {
                    if (actor != null && BGU_DataUtil.GetActorGuid(actor) == tamer.Guid)
                    {
                        found = true;
                        localTamer.Tamer = actor;
                        localTamer.IsSynced = true;
                        Logging.LogDebug("Found matching tamer with guid: {Guid}", tamer.Guid);
                    }
                }

                if (!found)
                {
                    // spawn tamer
                    Logging.LogDebug("Matching tamer not found for guid: {Guid}", tamer.Guid);
                }
            }
        });
    }

    private void SyncMonsters()
    {
        EntityManager.RunSystem((
            EntityId _,
            ref HpComponent hpComp,
            ref TeamComponent teamComp,
            ref TamerComponent tamer,
            ref LocalTamerComponent localTamer) =>
        {
            if (localTamer.IsMonsterSpawned || !tamer.IsSpawned)
            {
                return;
            }

            var monster = localTamer.Tamer?.GetMonster();
            if (monster == null)
            {
                var bgsEvents = BGS_EventCollectionCS.Get(localTamer.Tamer);
                if (bgsEvents == null)
                {
                    Logging.LogError("events are null");
                    return;
                }

                bgsEvents.Evt_TamerBlockingSpawnImmediately.Invoke(tamer.Guid);
            }

            monster = localTamer.Tamer?.GetMonster();
            if (monster == null)
            {
                Logging.LogError("monster is null");
                return;
            }

            // set monster hp
            var attrs = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(monster);
            hpComp.Hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);

            var events = BUS_EventCollectionCS.Get(localTamer.Tamer);
            if (events == null)
            {
                Logging.LogError("events are null");
                return;
            }

            IBUC_ABPMotionMatchingData mmData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPMotionMatchingData>(localTamer.Pawn);
            if (mmData != null)
            {
                events.Evt_ChangeMotionMatchingState.Invoke(mmData.DefaultMMState);
            }

            if (!IsMasterClient)
            {
                events.Evt_AIPerceptionSetting.Invoke(false);
                events.Evt_AIPauseBT.Invoke(true);
                Logging.LogDebug("Tamer actor disabled.");
            }

            ClientUtils.RegisterNewPlayerTeam(monster, teamComp.TeamId);

            localTamer.IsMonsterSpawned = true;
            Logging.LogDebug("Monster {Guid} synced", tamer.Guid);
        });
    }

    private void DestroyDeadMonsterMarkers()
    {
        EntityManager.RunSystem((
            EntityId entityId,
            ref HpComponent hpComp,
            ref LocalTamerComponent tamer,
            ref MarkerComponent marker) =>
        {
            if (tamer.IsMonsterSpawned && hpComp.Hp <= 0 && !marker.DestroyQueued)
            {
                Logging.LogDebug("Monster {Id} died", entityId);
                marker.DestroyQueued = true;

                var markerActor = marker.MarkerActor;
                if (markerActor != null)
                {
                    GameLoopPatch.QueueOnGameThread(() => { BGU_UnrealWorldUtil.DestroyActor(markerActor); }, "DestroyMarkerActor");
                }
            }
        });
    }

    public EntityId CreateNetworkedMonster()
    {
        return EntityManager.CreateNetworkedEntity(MonsterArchetype, (short)RelayClient.LocalPlayer.PeerId).EntityId;
    }

    public EntityId CreateNetworkedMonster(NetworkIdComponent netId)
    {
        return EntityManager.CreateNetworkedEntity(MonsterArchetype, netId);
    }

    public ref T GetEntityComponent<T>(EntityId entity) where T : struct
    {
        return ref EntityManager.GetComponent<T>(entity);
    }

    private void UpdateMarkerPositions()
    {
        EntityManager.RunSystem((EntityId _,
            ref LocalTamerComponent tamer,
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

        EntityManager.RunSystem((
            EntityId _,
            ref AnimationComponent animation,
            ref HpComponent health,
            ref MonsterAnimationComponent monsterAnimation,
            ref NicknameComponent nickname,
            ref NetworkIdComponent netId,
            ref TeamComponent team,
            ref TranslationComponent translation,
            ref TamerComponent tamer
        ) =>
        {
            bool retried = false;

            while (true)
            {
                var beforeApplyPosition = writer.Length;

                var anyDirty = animation.IsDirty ||
                               health.IsDirty ||
                               monsterAnimation.IsDirty ||
                               nickname.IsDirty ||
                               team.IsDirty ||
                               translation.IsDirty ||
                               tamer.IsDirty;

                if (!anyDirty)
                    return;

                writer.Put(netId);

                animation.WriteDelta(RelayClient, writer);
                health.WriteDelta(RelayClient, writer);
                monsterAnimation.WriteDelta(RelayClient, writer);
                nickname.WriteDelta(RelayClient, writer);
                team.WriteDelta(RelayClient, writer);
                translation.WriteDelta(RelayClient, writer);
                tamer.WriteDelta(RelayClient, writer);

                if (writer.Length > RelayClient.GetMaxPacketSize(DeliveryMethod.Unreliable))
                {
                    if (retried)
                    {
                        // if we retried and still failed, log an error
                        Logging.LogError("Packet too large, unable to send");
                        return;
                    }

                    // Rewind and send the partial packet
                    writer.SetPosition(beforeApplyPosition);
                    RelayClient.OpRaiseEventRaw(writer, DeliveryMethod.Unreliable);

                    // Start a new writer and retry
                    writer = new NetDataWriter();
                    writer.Put((byte)SystemEvent.EcsUpdate);
                    retried = true;

                    // Continue loop to retry
                    continue;
                }

                animation.ClearDirty();
                health.ClearDirty();
                monsterAnimation.ClearDirty();
                nickname.ClearDirty();
                team.ClearDirty();
                translation.ClearDirty();
                tamer.ClearDirty();

                break;
            }
        });

        if (writer.Length > 1)
        {
            RelayClient.OpRaiseEventRaw(writer, DeliveryMethod.Unreliable);
        }
    }

    private void DestroyRemoteEntity(NetworkIdComponent netId)
    {
        var entity = EntityManager.GetEntityByNetworkId(netId);

        if (entity.HasValue)
        {
            EntityManager.QueueDestroyEntity(entity.Value);
        }
        else
        {
            Logging.LogError("Received destroy event for locally non-existent entity: {Id}", netId);
        }
    }

    private void ApplyMonsterArchetypeDelta(NetDataReader reader)
    {
        while (reader.TryGetShort(out var owner))
        {
            var id = reader.GetUInt();

            var netId = new NetworkIdComponent(owner, id);
            var entity = EntityManager.GetEntityByNetworkId(netId);

            if (!entity.HasValue)
            {
                if (EntityManager.IsNetworkEntityDestroyed(netId))
                {
                    // already dead, skip
                    AnimationComponent.SkipDelta(RelayClient, reader);
                    HpComponent.SkipDelta(RelayClient, reader);
                    MonsterAnimationComponent.SkipDelta(RelayClient, reader);
                    NicknameComponent.SkipDelta(RelayClient, reader);
                    TeamComponent.SkipDelta(RelayClient, reader);
                    TranslationComponent.SkipDelta(RelayClient, reader);
                    TamerComponent.SkipDelta(RelayClient, reader);
                    continue;
                }

                // it must be new
                Logging.LogDebug("Creating new entity {Id}", netId);
                entity = CreateNetworkedMonster(netId);
            }

            ref var animation = ref GetEntityComponent<AnimationComponent>(entity.Value);
            ref var health = ref GetEntityComponent<HpComponent>(entity.Value);
            ref var monsterAnimation = ref GetEntityComponent<MonsterAnimationComponent>(entity.Value);
            ref var nickname = ref GetEntityComponent<NicknameComponent>(entity.Value);
            ref var team = ref GetEntityComponent<TeamComponent>(entity.Value);
            ref var translation = ref GetEntityComponent<TranslationComponent>(entity.Value);
            ref var tamer = ref GetEntityComponent<TamerComponent>(entity.Value);

            animation.ReadDelta(RelayClient, reader);
            health.ReadDelta(RelayClient, reader);
            monsterAnimation.ReadDelta(RelayClient, reader);
            nickname.ReadDelta(RelayClient, reader);
            team.ReadDelta(RelayClient, reader);
            translation.ReadDelta(RelayClient, reader);
            tamer.ReadDelta(RelayClient, reader);
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

        var entity = EntityManager.GetEntityByNetworkId(netId);
        if (entity.HasValue)
        {
            ref var tamer = ref GetEntityComponent<LocalTamerComponent>(entity.Value);
            return tamer.Pawn;
        }

        return null;
    }
}