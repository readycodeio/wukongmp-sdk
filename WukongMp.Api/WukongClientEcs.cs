using b1;
using BtlShare;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.ECS.Components;
using ReadyM.Relay.Common.Multiplayer;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Resources;

namespace WukongMp.Api;

public sealed partial class WukongClient
{
    public readonly World EcsWorld;
    public readonly NetworkedEntityManager NetManager;
    private ArchetypeId _monsterArchetype;

    private void DefineEcs()
    {
        _monsterArchetype = EcsWorld.DefineArchetype(
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

        EcsWorld.DefineSystemGroup("OnUpdate", g => g
            .AddSystem<SyncTamersSystem>()
            .AddSystem<UpdateMarkersSystem>()
            .AddSystem<DestroyDeadMonstersMarkersSystem>()
            .AddSystem<SyncMonstersSystem>()
            .AddSystem(new SendEcsDeltaSystem(RelayClient)));
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
            var entity = NetManager.GetEntityByNetworkId(netId);
            if (entity.HasValue)
            {
                Logging.LogDebug("Queueing remote entity for destruction: {Id}", netId);
                EcsWorld.EntityManager.QueueDestroyEntity(entity.Value);
            }
            else
            {
                Logging.LogError("Received destroy event for locally non-existent entity: {Id}", netId);
            }
        }
    }

    public void RunEcsWorldUpdate()
    {
        SetCachedPlayerProperties(); // not a system
        EcsWorld.Update();
    }

    public EntityId CreateNetworkedMonster()
    {
        return NetManager.CreateNetworkedEntity(_monsterArchetype, (short)RelayClient.LocalPlayer.PeerId).EntityId;
    }

    public EntityId CreateNetworkedMonster(NetworkIdComponent netId)
    {
        return NetManager.CreateNetworkedEntity(_monsterArchetype, netId);
    }

    public ref T GetEntityComponent<T>(EntityId entity) where T : struct
    {
        return ref EcsWorld.EntityManager.GetComponent<T>(entity);
    }

    private void DestroyRemoteEntity(NetworkIdComponent netId)
    {
        var entity = NetManager.GetEntityByNetworkId(netId);

        if (entity.HasValue)
        {
            EcsWorld.EntityManager.QueueDestroyEntity(entity.Value);
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
            var entity = NetManager.GetEntityByNetworkId(netId);

            if (!entity.HasValue)
            {
                if (NetManager.IsNetworkEntityDestroyed(netId))
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

        var entity = NetManager.GetEntityByNetworkId(netId);
        if (entity.HasValue)
        {
            ref var tamer = ref GetEntityComponent<LocalTamerComponent>(entity.Value);
            return tamer.Pawn;
        }

        return null;
    }

    public void SendUnitDead(NetworkIdComponent networkId, EDeadReason deadReason, int dmgId, int stiffLevel, bool isDotDmg, EAbnormalStateType abnormalType)
    {
        const byte eventCode = 3;
        var payload = new UnitDeadPacket(networkId, deadReason, dmgId, stiffLevel, isDotDmg, abnormalType);
        RelayClient.OpRaiseEvent(eventCode, payload, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SetMonsterHpScaling(int scaling)
    {
        if (!IsMasterClient)
        {
            GameUtils.ShowTip(string.Format(Texts.OnlyRoomOwnerCanUse, "/hp_scaling"));
        }

        Logging.LogDebug("Setting monster HP scaling to {Scaling}x", scaling);

        EcsWorld.RunJob(new ScaleMonsterHpJob(scaling));
    }
}