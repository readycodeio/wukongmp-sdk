using System;
using System.Collections.Generic;
using b1;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using LiteNetLib.Utils;
using ReadyM.Api;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong;
using ReadyM.Relay.Common.Wukong.Components;
using ReadyM.Relay.Common.Wukong.Jobs;
using UnrealEngine.Engine;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;

namespace WukongMp.Api;

public class WukongMpMod : ReadyMultiplayerMod
{
    private readonly ArchetypeId _monsterArchetype;
    private readonly SendEcsDeltaSystem _sendEcsDeltaSystem;

    public static WukongMpMod Instance { get; } = new();

    private WukongMpMod() : base(
        CmdLineParams.Instance.UserGuid,
        CmdLineParams.Instance.ServerIp!,
        CmdLineParams.Instance.ServerPort!.Value)
    {
        _monsterArchetype = World.RegisterArchetype(b =>
        {
            WukongCoreApi.SetUpMonsterArchetype(b);
            b.Add<MarkerComponent>()
                .Add<LocalTamerComponent>();
        });

        _sendEcsDeltaSystem = new SendEcsDeltaSystem(RelayClient)
        {
            Enabled = false // disabled by default until we become the master client
        };

        World.SystemRoot.Add(new SyncTamersSystem());
        World.SystemRoot.Add(new UpdateMarkersSystem());
        World.SystemRoot.Add(new DestroyDeadMonstersMarkersSystem());
        World.SystemRoot.Add(new SyncMonstersSystem());
        World.SystemRoot.Add(_sendEcsDeltaSystem);

        RelayClient.OnBeforeJoinedRoom += OnUpdatePeerId;
        RelayClient.OnEcsDelta += ApplyArchetypeDelta;
        RelayClient.OnRoomPropertiesChanged += OnRoomPropertiesChanged;
    }

    private void OnRoomPropertiesChanged(Dictionary<object, object?> diff)
    {
        if (diff.TryGetValue(RoomProperties.MasterClientId, out var id) && id is short newMasterId)
        {
            Logging.LogInformation("Master client changed to {NewMasterId}", newMasterId);

            CheckSendDeltaSystem();
        }
    }

    private void CheckSendDeltaSystem()
    {
        _sendEcsDeltaSystem.Enabled = IsMasterClient;
        Logging.LogDebug("SendEcsDeltaSystem enabled: {Enabled}", _sendEcsDeltaSystem.Enabled);
    }

    private void OnUpdatePeerId()
    {
        CheckSendDeltaSystem();
    }

    protected override void OnPingUpdated(int ping)
    {
        PingIndicatorWidget.Instance.SetPingValue(ping);
    }

    public void RunEcsWorldUpdate()
    {
        WukongMP.Instance.Client.SetCachedPlayerProperties();
        Tick(default);
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

    protected override void Log(LogLevel level, [StructuredMessageTemplate] string message, params object?[] args)
        => Logging.Log(level, message, args.AsSpan());

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
        query.ThrowOnStructuralChange = false; // okay because the query is readonly
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
        query.ThrowOnStructuralChange = false; // okay because the query is readonly
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