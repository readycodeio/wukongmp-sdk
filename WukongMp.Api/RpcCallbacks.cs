using b1;
using b1.BGW;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Old.DTO;
using WukongMp.Api.Patches;

namespace WukongMp.Api;

public partial class WukongMpMod
{
    [RpcEvent(RelayMode.Others)]
    private static void OnDamageNum(DamageNumParam damageNum)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var uiEvt = BGW_UIEventCollection.Get(GameUtils.GetWorld());
            uiEvt.Evt_UI_ShowHPChangeNum(damageNum);
        }, nameof(OnDamageNum), BGW_TickGroupMask.TG_PreAnim);
    }

    [RpcEvent(RelayMode.All)]
    private static void OnTeleportFinish(short __sender)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var playerState = WukongMP.Instance.Client.GetPlayerById(__sender);
            if (playerState == null)
            {
                Logging.LogError("Player not found: {PlayerId}", __sender);
                return;
            }

            var events = BUS_EventCollectionCS.Get(playerState.Pawn);
            events?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportEnd, -1f);
            events?.Evt_TeleportFinish.Invoke();
        }, nameof(OnTeleportFinish));
    }

    [RpcEvent(RelayMode.Others)]
    private void WakeUpMonster(string guid)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                if (BGU_DataUtil.GetActorGuid(actor) != guid)
                    continue;

                var events = BGS_GSEventCollection.Get(actor);
                if (events != null)
                {
                    var hasGuid = false;

                    World.Query<TamerComponent>().ForEachEntity((ref tamer, _) =>
                    {
                        if (tamer.Guid == guid)
                        {
                            hasGuid = true;
                        }
                    });

                    if (actor.GetMonster() == null)
                    {
                        Logging.LogDebug("Spawning monster for tamer with guid: {Guid}.", guid);

                        if (!hasGuid)
                        {
                            Logging.LogError("Not syncing monster");
                        }

                        Logging.LogDebug("Invoking Evt_TamerBlockingSpawnImmediately.");
                        events.Evt_TamerBlockingSpawnImmediately.Invoke(guid);
                    }
                    else if (!hasGuid)
                    {
                        Logging.LogDebug("Monster already spawned but not synced: {Guid}.", guid);

                        Logging.LogError("Not syncing monster");
                    }
                }
                else
                {
                    Logging.LogDebug("Event is null");
                }

                return;
            }

            // TODO: Spawn if not found
        }, nameof(WakeUpMonster));
    }

    [RpcEvent(RelayMode.Others)]
    public void OnMontageCallback(MontageCallbackData data)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var id = data.NetId;
            var pawn = GetPawnByNetworkId(id);
            if (pawn == null)
            {
                LogNullCharacter(id);
                return;
            }

            if (string.IsNullOrEmpty(data.MontagePath))
            {
                Logging.LogDebug("Stopping montage playback for character {CharacterId}", id);
                pawn.StopAnimMontage(null);
                return;
            }

            var fullMontagePath = data.Compressed ? MontageHelpers.DecompressMontageName(data.MontagePath) : data.MontagePath;
            Logging.LogDebug("Received montage: {Montage}, position: {Position}, reset: {Reset}", fullMontagePath, data.Position, data.Reset);

            var animInstance = pawn.Mesh.GetAnimInstance();
            if (animInstance == null)
            {
                Logging.LogError("AnimInstance is null");
                return;
            }

            var currentMontage = animInstance.GetCurrentActiveMontage();
            Logging.LogDebug("Current montage: {Montage}", currentMontage?.PathName);

            // if the same montage is currently playing an no reset flag is given, do not play new montage
            if (currentMontage != null && currentMontage.PathName == fullMontagePath && !data.Reset)
            {
                Logging.LogDebug("Skipping montage playback: {Montage}, is reset: {Reset}", fullMontagePath, data.Reset);
                return;
            }

            var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(fullMontagePath, ELoadResourceType.SyncLoadAndCache);

            if (montage == null)
            {
                Logging.LogWarning("Montage not found: {Montage}", fullMontagePath);
                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);

            if (events == null)
            {
                Logging.LogError("events are null");
                return;
            }

            Logging.LogDebug("Applying montage callback for character {CharacterId} with montage {Montage} @ {Position}", id, fullMontagePath, data.Position);
            animInstance.Montage_Play(montage, 1f, EMontagePlayReturnType.MontageLength, data.Position);
            events.Evt_PlayMontageCallback.Invoke(EMontageBindReason.Default, montage, EMontageCallbackState.OnStarted);
        }, nameof(OnMontageCallback));
    }

    [RpcEvent(RelayMode.Others)]
    private void OnUnitDead(UnitDeadPacket data)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var pawn = GetPawnByNetworkId(data.NetworkId);
            if (pawn == null)
            {
                LogNullCharacter(data.NetworkId);
                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);
            if (events == null)
            {
                Logging.LogError("Failed to get event collection for unit {Unit}", pawn.GetName());
                return;
            }

            events.Evt_UnitDead.Invoke(null, data.DeadReason, data.DmgId, data.StiffLevel, null, default, data.IsDotDmg, data.AbnormalType);
        }, nameof(OnUnitDead));
    }

    public void SendMontageCallback(NetworkIdComponent netId, UAnimMontage montage, float position, bool reset)
    {
        Logging.LogDebug("Sending montage callback: {Montage} {Position}", montage.PathName, position);
        var shortened = MontageHelpers.CompressMontageName(montage.PathName, out var shortMontagePath);
        var data = shortened ? shortMontagePath : montage.PathName;
        var evData = new MontageCallbackData(netId, shortened, data, position, reset);
        SendMontageCallback(evData);
    }

    public void SendMontageCancel(NetworkIdComponent netId)
    {
        Logging.LogDebug("Sending montage cancel");
        var evData = new MontageCallbackData(netId, false, "", 0f, false);
        SendMontageCallback(evData);
    }

    private static void LogNullCharacter(NetworkIdComponent characterId)
    {
        if (characterId.Id != uint.MaxValue)
            Logging.LogWarning("Monster not found: {Id}", characterId); // monster not found
        else
            Logging.LogError("Player not found: {Id}", characterId); // player not found
    }
}