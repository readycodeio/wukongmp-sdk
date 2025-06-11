using System;
using System.Buffers;
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
using ReadyM.Relay.Common.Wukong.Jobs;
using UnrealEngine.Engine;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Old;
using WukongMp.Api.Patches;

namespace WukongMp.Api;

/// <summary>
/// The common denominator of all multiplayer Wukong mods based on ReadyM's API.
/// </summary>
public partial class WukongMpModBase : ReadyMultiplayerMod
{
    private readonly ArchetypeId _monsterArchetype;
    private readonly SendEcsDeltaSystem _sendEcsDeltaSystem;

    [Obsolete]
    public static WukongClient Client => WukongMP.Instance.Client;

    protected IBlobClient Blobs => RelayClient;

    protected WukongMpModBase() : base(
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

    protected override void Log(LogLevel level, [StructuredMessageTemplate] string message, params object?[] args)
        => Logging.Log(level, message, args.AsSpan());

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

    public BGUCharacterCS? GetPawnByNetworkId(NetworkIdComponent netId)
    {
        if (netId.Id == uint.MaxValue)
        {
            var player = Client.GetPlayerById(netId.Owner);
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

    private void ApplyArchetypeDelta(NetDataReader reader)
    {
        if (IsMasterClient)
        {
            return; // ignore echo deltas, TODO: server should only send deltas to other players
        }

        Logging.LogDebug("Received archetype delta");

        var bytesToCopy = reader.UserDataSize - 1; // first byte is the event code
        var offset = reader.UserDataOffset + 1; // skip the first byte which is the event code

        var buffer = ArrayPool<byte>.Shared.Rent(bytesToCopy);
        // offset 1 to skip the first byte which is the event code
        Array.Copy(reader.RawData, offset, buffer, 0, bytesToCopy);

        var readerCopy = new NetDataReader(buffer, 0, bytesToCopy);

        GameLoopPatch.QueueOnGameThread(() =>
        {
            Logging.LogDebug("Applying archetype delta");
            new ApplyDeltaJob(readerCopy, NetManager, CreateNetworkedMonster).Execute(); // TODO: Command buffer
            ArrayPool<byte>.Shared.Return(buffer);
        }, nameof(ApplyDeltaJob));
    }
}