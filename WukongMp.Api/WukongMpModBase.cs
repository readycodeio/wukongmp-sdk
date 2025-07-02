using System;
using System.Collections.Generic;
using b1;
using CSharpModBase;
using Friflo.Engine.ECS;
using HarmonyLib;
using JetBrains.Annotations;
using ReadyM.Api;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Old;
using WukongMp.Api.Patches;

namespace WukongMp.Api;

/// <summary>
/// The common denominator of all multiplayer Wukong mods based on ReadyM's API.
/// </summary>
public abstract class WukongMpModBase : ReadyMultiplayerMod
{
    protected readonly Harmony Harmony = new("ReadyM.WukongMp");
    private ArchetypeId _monsterArchetype;
    private ArchetypeId _roomConfigArchetype;

    [Obsolete]
    public static WukongClient Client => WukongMP.Instance.Client;

    public bool IsMasterClient
    {
        get
        {
            var masterId = (PlayerId)RelayClient.RoomState.GetValueOrDefault(RoomProperties.MasterClientId, PlayerId.Invalid);
            return masterId != PlayerId.Invalid && RelayClient.LocalPlayer.PlayerId == masterId;
        }
    }

    protected IBlobClient Blobs => RelayClient;

    protected WukongMpModBase() : base(
        CmdLineParams.Instance.UserGuid,
        CmdLineParams.Instance.ServerIp!,
        CmdLineParams.Instance.ServerPort!.Value)
    {
        RelayClient.OnRoomPropertiesChanged += OnRoomPropertiesChanged;
    }

    protected override void ConfigureMod(IModConfig config)
    {
        _monsterArchetype = config.RegisterArchetype(b =>
        {
            WukongCoreApi.RegisterMonsterArchetype(b);
            b.Add<LocalTamerComponent>();
            b.Add<MarkerComponent>();
        });
        _roomConfigArchetype = config.RegisterArchetype(WukongCoreApi.RegisterRoomConfigArchetype);

        config.AddSystem<SyncTamersSystem>();
        config.AddSystem<UpdateMarkersSystem>();
        config.AddSystem<DestroyDeadMonstersMarkersSystem>();
        config.AddSystem<SyncMonstersSystem>();
    }

    protected override void ConfigureNetworking(INetworkedComponentConfig config)
    {
        WukongCoreApi.MarkNetworkedComponents(config);
    }

    protected override void Patch()
    {
        base.Patch();

        Harmony.PatchCategory(Constants.GlobalPatches);
        Logging.LogInformation("Patched Harmony category: {Category}", Constants.GlobalPatches);

        Harmony.PatchCategory(Constants.ConnectedPatches);
        Logging.LogInformation("Patched Harmony category: {Category}", Constants.ConnectedPatches);
    }

    protected override void Unpatch()
    {
        Harmony.UnpatchCategory(Constants.ConnectedPatches);
        Logging.LogInformation("Unpatched Harmony category: {Category}", Constants.ConnectedPatches);

        Harmony.UnpatchCategory(Constants.GlobalPatches);
        Logging.LogInformation("Unpatched Harmony category: {Category}", Constants.GlobalPatches);

        base.Unpatch();
    }

    public override void EnterRoom()
    {
        base.EnterRoom();

        Utils.TryRunOnGameThread(() => { });
    }

    public override void ExitRoom()
    {
        Utils.TryRunOnGameThread(() => { });

        base.ExitRoom();
    }

    protected override void Log(LogLevel level, [StructuredMessageTemplate] string message, params object?[] args)
        => Logging.Log(level, message, args.AsSpan());

    public Entity CreateNetworkedMonster(LocalTamerComponent localTamer, TamerComponent tamer, TeamComponent team)
    {
        var (entity, netId) = CreateNetworkedEntity(_monsterArchetype, b =>
        {
            b.Add(localTamer);
            b.Add(tamer);
            b.Add(team);
        });
        Logging.LogDebug("Creating local networked monster with {NetId}", netId);
        return entity;
    }

    public BGUCharacterCS? GetPawnByNetworkId(NetworkIdComponent netId)
    {
        if (netId.Id == uint.MaxValue)
        {
            var player = Client.GetPlayerById(netId.Creator);
            if (player != null)
                return player.Pawn;
        }

        if (TryGetEntityByNetworkId(netId, out var entity))
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

    public Entity? GetMonsterByGuid(string guid)
    {
        Entity? entityId = null;

        var query = World.Query<TamerComponent>();
        query.ThrowOnStructuralChange = false; // okay because the query is readonly
        query.ForEachEntity((ref tamer, entity) =>
        {
            if (tamer.Guid == guid)
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
        if (diff.TryGetValue(RoomProperties.MasterClientId, out var id) && id is PlayerId newMasterId)
        {
            Logging.LogInformation("Master client changed to {NewMasterId}", newMasterId);
        }
    }

    protected override void RunOnGameThread(Action action)
    {
        GameLoopPatch.QueueOnGameThread(action);
    }
}