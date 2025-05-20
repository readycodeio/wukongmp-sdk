using b1;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.ECS.Components;
using ReadyM.Relay.Common.Multiplayer;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong.Components;
using ReadyM.Relay.Common.Wukong.Jobs;
using WukongMp.Api.Client;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Resources;

namespace WukongMp.Api;

public class WukongEcs
{
    public readonly World World;
    public readonly NetworkedEntityManager NetManager;
    private readonly WukongClient _client;
    private readonly ArchetypeId _monsterArchetype;
    private readonly ArchetypeId _playerArchetype;

    public static WukongEcs Instance { get; } = new(WukongMP.Instance.Client);

    private WukongEcs(WukongClient client)
    {
        _client = client;
        World = new World();
        NetManager = new NetworkedEntityManager(World, OnNetworkedEntityCreated, OnNetworkedEntityDestroyed);

        _monsterArchetype = World.DefineArchetype(
            // local
            typeof(MarkerComponent),
            typeof(LocalTamerComponent),
            // synced
            typeof(TamerComponent),
            typeof(AnimationComponent),
            typeof(HpComponent),
            typeof(MonsterAnimationComponent),
            typeof(NicknameComponent),
            typeof(NetworkIdComponent),
            typeof(TeamComponent),
            typeof(TranslationComponent)
        );

        _playerArchetype = World.DefineArchetype(
            // local
            typeof(MarkerComponent),
            typeof(LocalPlayerComponent),
            // synced
            typeof(PlayerComponent),
            typeof(AnimationComponent),
            typeof(HpComponent),
            typeof(NicknameComponent),
            typeof(NetworkIdComponent),
            typeof(TeamComponent),
            typeof(TranslationComponent)
        );

        World.DefineSystemGroup("OnUpdate", g => g
            .AddSystem<SyncTamersSystem>()
            .AddSystem<UpdateMarkersSystem>()
            .AddSystem<DestroyDeadMonstersMarkersSystem>()
            .AddSystem<SyncMonstersSystem>()
            .AddSystem(new SendEcsDeltaSystem(_client.RelayClient)));

        _client.RelayClient.OnEcsDelta += ApplyArchetypeDelta;
        _client.RelayClient.OnReceivedDestroyEntity += DestroyRemoteEntity;
    }

    private void OnNetworkedEntityCreated(NetworkIdComponent obj)
    {
        Logging.LogDebug("Networked entity created: {Id}", obj);
    }

    private void OnNetworkedEntityDestroyed(NetworkIdComponent netId)
    {
        if (netId.Owner == _client.RelayClient.LocalPlayer.PeerId)
        {
            // our own entity - send destroy event
            Logging.LogDebug("Networked entity destroyed: {Id} (owned)", netId);
            var writer = new NetDataWriter();
            writer.Put((byte)SystemEvent.DestroyEntity);
            writer.Put(netId);
            _client.RelayClient.OpRaiseEventRaw(writer, DeliveryMethod.ReliableOrdered);
        }
        else
        {
            // remote entity, dissolve it locally
            var entity = NetManager.GetEntityByNetworkId(netId);
            if (entity.HasValue)
            {
                Logging.LogDebug("Queueing remote entity for destruction: {Id}", netId);
                World.EntityManager.QueueDestroyEntity(entity.Value);
            }
            else
            {
                Logging.LogError("Received destroy event for locally non-existent entity: {Id}", netId);
            }
        }
    }

    public void RunEcsWorldUpdate()
    {
        WukongMP.Instance.Client.SetCachedPlayerProperties(); // not a system, TODO
        World.Update();
    }

    public EntityId CreateNetworkedMonster()
    {
        return NetManager.CreateNetworkedEntity(_monsterArchetype, (short)_client.RelayClient.LocalPlayer.PeerId).EntityId;
    }

    public EntityId CreateNetworkedMonster(NetworkIdComponent netId)
    {
        return NetManager.CreateNetworkedEntity(_monsterArchetype, netId);
    }

    public ref T GetEntityComponent<T>(EntityId entity) where T : struct
    {
        return ref World.EntityManager.GetComponent<T>(entity);
    }

    private void DestroyRemoteEntity(NetworkIdComponent netId)
    {
        var entity = NetManager.GetEntityByNetworkId(netId);

        if (entity.HasValue)
        {
            World.EntityManager.QueueDestroyEntity(entity.Value);
        }
        else
        {
            Logging.LogError("Received destroy event for locally non-existent entity: {Id}", netId);
        }
    }

    private void ApplyArchetypeDelta(NetDataReader reader)
    {
        World.RunJob(new ApplyDeltaJob(reader, _client.RelayClient, NetManager, _monsterArchetype));
    }

    public BGUCharacterCS? GetPawnByNetworkId(NetworkIdComponent netId)
    {
        if (netId.Owner == -1)
        {
            var player = WukongMP.Instance.Client.GetPlayerById((int)netId.Id);
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

    public void SetMonsterHpScaling(int scaling)
    {
        if (!WukongMP.Instance.Client.IsMasterClient)
        {
            GameUtils.ShowTip(string.Format(Texts.OnlyRoomOwnerCanUse, "/hp_scaling"));
        }

        Logging.LogDebug("Setting monster HP scaling to {Scaling}x", scaling);

        World.RunJob(new ScaleMonsterHpJob(scaling));
    }
}