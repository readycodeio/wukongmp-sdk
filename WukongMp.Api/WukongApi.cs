using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Protocol.Enums;
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
    public Store World;
    public CommandBufferSynced CommandBuffer;
    private ArchetypeId _monsterArchetype;

    public readonly NetworkedEntityManager NetManager;
    private readonly WukongClient _client;

    public static WukongApi Instance { get; } = new(WukongMP.Instance.Client);

    private WukongApi(WukongClient client)
    {
        _client = client;
        World = ReadyMApp.CreateEntityStore();
        _monsterArchetype = World.RegisterArchetype(b =>
            b.Add(new MarkerComponent())
                .Add(new LocalTamerComponent())
                .Add(new TamerComponent())
                .Add(new AnimationComponent())
                .Add(new HpComponent())
                .Add(new MonsterAnimationComponent())
                .Add(new NicknameComponent())
                .Add(new TeamComponent())
                .Add(new TranslationComponent()));

        var cb = World.GetCommandBuffer();
        cb.ReuseBuffer = true;
        CommandBuffer = cb.Synced;

        NetManager = new NetworkedEntityManager(World, _client.LocalPlayerState.PeerId);
        NetManager.onEntityDestroyed += OnNetworkedEntityDestroyed;

        World.SystemRoot.Add(new SyncTamersSystem());
        World.SystemRoot.Add(new UpdateMarkersSystem());
        World.SystemRoot.Add(new DestroyDeadMonstersMarkersSystem());
        World.SystemRoot.Add(new SyncMonstersSystem());
        World.SystemRoot.Add(new SendEcsDeltaSystem(_client.RelayClient));

        _client.RelayClient.OnEcsDelta += ApplyArchetypeDelta;
        _client.RelayClient.OnReceivedDestroyEntity += DestroyRemoteEntity;
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
        return NetManager.CreateNetworkedEntity(_monsterArchetype).Entity;
    }

    public Entity CreateNetworkedMonster(NetworkIdComponent netId)
    {
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
        query.ThrowOnStructuralChange = false;
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
        query.ThrowOnStructuralChange = false;
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