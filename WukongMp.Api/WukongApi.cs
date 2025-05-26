using b1;
using Friflo.Engine.ECS;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong;
using ReadyM.Relay.Common.Wukong.Components;
using ReadyM.Relay.Common.Wukong.Jobs;
using UnrealEngine.Engine;
using WukongMp.Api.Client;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Resources;

namespace WukongMp.Api;

public class WukongApi
{
    public readonly Store World;
    public readonly CommandBufferSynced CommandBuffer;
    private readonly ArchetypeId _monsterArchetype;

    public readonly NetworkedEntityManager NetManager;
    private readonly WukongClient _client;
    private readonly SendEcsDeltaSystem _sendEcsDeltaSystem;

    public static WukongApi Instance { get; } = new(WukongMP.Instance.Client);

    private WukongApi(WukongClient client)
    {
        _client = client;
        World = ReadyMApp.CreateEntityStore();

        _monsterArchetype = World.RegisterArchetype(b =>
        {
            WukongCoreApi.SetUpMonsterArchetype(b);
            b.Add<MarkerComponent>()
                .Add<LocalTamerComponent>();
        });

        var cb = World.GetCommandBuffer();
        cb.ReuseBuffer = true;
        CommandBuffer = cb.Synced;

        NetManager = new NetworkedEntityManager(World, ReadyM.Relay.Common.Protocol.Constants.UnsetPeerId);
        NetManager.onEntityDestroyed += OnNetworkedEntityDestroyed;

        _sendEcsDeltaSystem = new SendEcsDeltaSystem(_client.RelayClient)
        {
            Enabled = false // disabled by default until we become the master client
        };

        World.SystemRoot.Add(new SyncTamersSystem());
        World.SystemRoot.Add(new UpdateMarkersSystem());
        World.SystemRoot.Add(new DestroyDeadMonstersMarkersSystem());
        World.SystemRoot.Add(new SyncMonstersSystem());
        World.SystemRoot.Add(_sendEcsDeltaSystem);

        _client.RelayClient.OnBeforeJoinedRoom += UpdatePeerId;
        _client.RelayClient.OnEcsDelta += ApplyArchetypeDelta;
        _client.RelayClient.OnReceivedDestroyEntity += DestroyRemoteEntity;
        _client.OnMasterClientChanged += OnMasterClientChanged;
    }

    private void OnMasterClientChanged(short obj)
    {
        _sendEcsDeltaSystem.Enabled = _client.IsMasterClient;
        Logging.LogDebug("SendEcsDeltaSystem enabled: {Enabled}", _sendEcsDeltaSystem.Enabled);
    }

    private void UpdatePeerId()
    {
        Logging.LogDebug("Updating NetManager peer id to {PeerId}", _client.LocalPlayerState.PeerId);
        NetManager.PeerId = _client.LocalPlayerState.PeerId;

        _sendEcsDeltaSystem.Enabled = _client.IsMasterClient;
        Logging.LogDebug("SendEcsDeltaSystem enabled: {Enabled}", _sendEcsDeltaSystem.Enabled);
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
            if (NetManager.TryGetEntityByNetworkId(netId, out var entity))
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

        lock (CommandBuffer)
        {
            CommandBuffer.Playback();
        }

        World.SystemRoot.Update(default);
    }

    public Entity CreateNetworkedMonster()
    {
        var ids = NetManager.CreateNetworkedEntity(_monsterArchetype);
        Logging.LogDebug("Creating local networked monster with {NetId}", ids.NetId);
        return ids.Entity;
    }

    public Entity CreateNetworkedMonster(NetworkIdComponent netId)
    {
        Logging.LogDebug("Creating remote networked monster with {NetId}", netId);
        return NetManager.CreateRemoteNetworkedEntity(_monsterArchetype, netId);
    }

    private void DestroyRemoteEntity(NetworkIdComponent netId)
    {
        if (NetManager.TryGetEntityByNetworkId(netId, out var entity))
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
        if (WukongMP.Instance.Client.IsMasterClient)
        {
            return; // ignore echo deltas, TODO: server should only send deltas to other players
        }

        Logging.LogDebug("Applying archetype delta");
        new ApplyDeltaJob(reader, NetManager, CreateNetworkedMonster).Execute(); // TODO: Command buffer
    }

    public BGUCharacterCS? GetPawnByNetworkId(NetworkIdComponent netId)
    {
        if (netId.Id == uint.MaxValue)
        {
            var player = WukongMP.Instance.Client.GetPlayerById(netId.Owner);
            if (player != null)
                return player.Pawn;
        }

        if (NetManager.TryGetEntityByNetworkId(netId, out var entity))
        {
            if (entity.Value.TryGetComponent<LocalTamerComponent>(out var tamer))
            {
                return tamer.Pawn;
            }
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

    public Entity? GetMonsterByActor(AActor? actor)
    {
        if (actor == null)
            return null;

        Entity? entityId = null;

        var query = World.Query<LocalTamerComponent>();
        query.ForEachEntity((ref tamer, entity) =>
        {
            if (tamer.Pawn == actor)
            {
                entityId = entity;
            }
        });

        return entityId;
    }

    public Entity? GetByTamerActor(BUTamerActor? owner)
    {
        if (owner == null)
            return null;

        Entity? entityId = null;

        var query = World.Query<LocalTamerComponent>();
        query.ForEachEntity((ref tamer, entity) =>
        {
            if (tamer.Tamer == owner)
            {
                entityId = entity;
            }
        });

        return entityId;
    }
}