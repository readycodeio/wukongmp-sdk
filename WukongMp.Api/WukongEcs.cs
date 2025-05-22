using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using LiteNetLib;
using LiteNetLib.Utils;
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
    public readonly EntityStore World;
    public CommandBuffer CommandBuffer { get; private set; }
    public readonly NetworkedEntityManager NetManager;

    private readonly SystemRoot _systemRoot;
    private readonly WukongClient _client;

    public static WukongEcs Instance { get; } = new(WukongMP.Instance.Client);

    private WukongEcs(WukongClient client)
    {
        _client = client;
        World = new EntityStore();
        CommandBuffer = World.GetCommandBuffer();
        _systemRoot = new SystemRoot();
        _systemRoot.AddStore(World);

        NetManager = new NetworkedEntityManager(World, OnNetworkedEntityCreated, OnNetworkedEntityDestroyed);

        _systemRoot.Add(new SyncTamersSystem());
        _systemRoot.Add(new UpdateMarkersSystem());
        _systemRoot.Add(new DestroyDeadMonstersMarkersSystem());
        _systemRoot.Add(new SyncMonstersSystem());
        _systemRoot.Add(new SendEcsDeltaSystem(_client.RelayClient));

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
                CommandBuffer.DeleteEntity(entity.Value.Id);
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
        CommandBuffer.Playback();
        CommandBuffer = World.GetCommandBuffer();
        _systemRoot.Update(new UpdateTick()); // TODO: Delta time
    }

    public Entity CreateNetworkedMonster()
    {
        var entity = NetManager.CreateNetworkedEntity((short)_client.RelayClient.LocalPlayer.PeerId).Entity;

        entity.AddComponent<MarkerComponent>();
        entity.AddComponent<LocalTamerComponent>();
        entity.AddComponent<TamerComponent>();
        entity.AddComponent<AnimationComponent>();
        entity.AddComponent<HpComponent>();
        entity.AddComponent<MonsterAnimationComponent>();
        entity.AddComponent<NicknameComponent>();
        entity.AddComponent<TeamComponent>();
        entity.AddComponent<TranslationComponent>();

        return entity;
    }

    public Entity CreateNetworkedMonster(NetworkIdComponent netId)
    {
        var entity = NetManager.CreateNetworkedEntity(netId);

        entity.AddComponent<MarkerComponent>();
        entity.AddComponent<LocalTamerComponent>();
        entity.AddComponent<TamerComponent>();
        entity.AddComponent<AnimationComponent>();
        entity.AddComponent<HpComponent>();
        entity.AddComponent<MonsterAnimationComponent>();
        entity.AddComponent<NicknameComponent>();
        entity.AddComponent<TeamComponent>();
        entity.AddComponent<TranslationComponent>();

        return entity;
    }

    private void DestroyRemoteEntity(NetworkIdComponent netId)
    {
        var entity = NetManager.GetEntityByNetworkId(netId);

        if (entity.HasValue)
        {
            CommandBuffer.DeleteEntity(entity.Value.Id);
        }
        else
        {
            Logging.LogError("Received destroy event for locally non-existent entity: {Id}", netId);
        }
    }

    private void ApplyArchetypeDelta(NetDataReader reader)
    {
        new ApplyDeltaJob(reader, _client.RelayClient, NetManager).Execute();
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
            ref var tamer = ref entity.Value.GetComponent<LocalTamerComponent>();
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

        World.Query<HpComponent, LocalTamerComponent>().Each(new ScaleMonsterHpJob(scaling));
    }
}