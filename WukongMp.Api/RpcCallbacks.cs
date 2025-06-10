using b1;
using b1.BGW;
using BtlShare;
using CSharpModBase;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong.Components;
using System;
using System.Threading.Tasks;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Old.Enums;
using WukongMp.Api.Old.State;
using WukongMp.Api.Patches;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public partial class WukongMpMod
{
    public void SendAddBuffHandler(int buffid, AActor caster, AActor rootcaster, float duration, EBuffSourceType buffsourcetype, bool brecursed, FBattleAttrSnapShot battleattrsnapshot)
        => SendAddBuff(new BuffAddData(buffid, duration));

    public void SendRemoveBuffHandler(int buffid, EBuffEffectTriggerType removetriggertype, int layer, bool withtriggerremmoveeffect)
        => SendRemoveBuff(new BuffRemoveData(buffid, removetriggertype, layer, withtriggerremmoveeffect));

    public void SendRemoveAllBuffsHandler(EBuffEffectTriggerType removetriggertype, bool withtriggerremmoveeffect)
        => SendRemoveAllBuffs(new BuffRemoveAllData(removetriggertype, withtriggerremmoveeffect));

    public void HandleBuffRemoveImmediately(int buffid, EBuffEffectTriggerType removetriggertype, bool withtriggerremmoveeffect)
        => SendRemoveBuff(new BuffRemoveData(buffid, removetriggertype, -1, withtriggerremmoveeffect));

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

    public void SendPvPEvent(PvPEvent ev, int data = 0)
    {
        if (!IsMasterClient)
        {
            Logging.LogError("Only room owner can send start countdown.");
            return;
        }

        Logging.LogInformation("Sending PvP event: {Event}", ev);

        SendPvpEvent([(int)ev, data]);
    }

    public void SendPlayMovieRequest(FPlayMovieRequest playMovieRequest)
    {
        SendPlayMovieRequest(new PlayMovieData(
            playMovieRequest.SequenceID,
            playMovieRequest.bDisablePlayerControl,
            playMovieRequest.bDisableMovementInput,
            playMovieRequest.bDisableLookAtInput,
            playMovieRequest.bHidePlayer,
            playMovieRequest.bHideHud,
            "",
            playMovieRequest.MatchType));
    }

    [RpcEvent(RelayMode.Others)]
    private static void OnExitPhantomRush(short playerId)
    {
        var playerState = Client.GetPlayerById(playerId);
        if (playerState == null)
        {
            Logging.LogError("Player not found: {Id}", playerId);
            return;
        }

        Logging.LogDebug("Received exit phantom rush for player {Nickname}", playerState.NickName);
        var events = BUS_EventCollectionCS.Get(playerState.Pawn);
        playerState.ReceivedPhantomRushExit = true;
        events?.Evt_RelievePhantomRush.Invoke();
    }

    [RpcEvent(RelayMode.All)]
    private static void OnEndMatchmaking()
    {
        PvPUtils.OnMatchmakingEnded();
    }

    [RpcEvent(RelayMode.Others)]
    private static void OnAddBuff(short __sender, BuffAddData data)
    {
        var playerState = Client.GetPlayerById(__sender);
        BuffUtils.AddBuff(playerState?.Pawn, data.BuffId, data.Duration);
    }

    [RpcEvent(RelayMode.Others)]
    private static void OnRemoveBuff(short __sender, BuffRemoveData data)
    {
        var state = Client.GetPlayerById(__sender);
        BuffUtils.RemoveBuff(state?.Pawn, data.BuffId, data.TriggerType, data.Layer, data.WithTriggerRemoveEffect);
    }

    [RpcEvent(RelayMode.Others)]
    private static void OnRemoveAllBuffs(short __sender, BuffRemoveAllData data)
    {
        var playerState = Client.GetPlayerById(__sender);
        BuffUtils.RemoveAllBuffs(playerState?.Pawn, data.TriggerType, data.WithTriggerRemoveEffect);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnUnitStateTrigger(StateTriggerData data)
    {
        var character = GetPawnByNetworkId(data.NetId);
        NpcLocomotionUtils.SetStateTrigger(character, data.Trigger, data.Time, data.NeedForceUpdate);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnUnitSimpleState(SimpleStateData data)
    {
        var character = GetPawnByNetworkId(data.NetId);
        NpcLocomotionUtils.SetSimpleState(character, data.SimpleState, data.IsRemove);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnTriggerFsmState(FsmStateData data)
    {
        var character = GetPawnByNetworkId(data.NetId);
        NpcLocomotionUtils.SetFsmState(character, data.FsmStateName);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnMotionMatchingState(MotionMatchingStateData data)
    {
        var character = GetPawnByNetworkId(data.NetId);
        NpcLocomotionUtils.SetMotionMatchingState(character, data.State);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnSpawnSummon(UnitSummonData data)
    {
        GameLoopPatch.QueueOnGameThread(() => { SummonPatch.ExecuteSummon(data.SummonerId, data.SummonId, data.Guid, data.Name, data.TeamId); }, nameof(OnSpawnSummon));
    }

    [RpcEvent(RelayMode.Master)]
    private static void OnSpawnUnits(short __sender, UnitSpawnRequestData data)
    {
        SpawningUtils.SpawnUnitsMaster(__sender, data.UnitName, data.Count, data.TeamId);
    }

    [RpcEvent(RelayMode.Others)]
    private static void OnPlayerTransBegin(short __sender, PlayerTransBeginData data)
    {
        TransformationUtils.TransformPlayer(__sender, data.UnitResId, data.UnitBornSkillId, data.EnableBlendViewTarget, data.TransBeginType);
    }

    [RpcEvent(RelayMode.Others)]
    private static void OnPlayerTransEnd(short __sender, PlayerTransEndData data)
    {
        TransformationUtils.TransformPlayerBack(__sender, data.UnitResId, data.UnitBornSkillId, data.EnableBlendViewTarget, data.TransEndType);
    }

    [RpcEvent(RelayMode.Others)]
    private static void OnPlayMovieRequest(PlayMovieData data)
    {
        CutsceneUtils.PlayCutscene(data);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnSetTarget(TargetData data)
    {
        var pawn = GetPawnByNetworkId(data.Character);
        if (pawn == null)
        {
            Logging.LogNull(nameof(data.Character));
            return;
        }

        if (data.ClearTarget)
        {
            TargetingApi.ClearTarget(pawn);
            return;
        }

        var target = GetPawnByNetworkId(data.Target);

        if (target == null)
        {
            Logging.LogNull(nameof(data.Target));
            return;
        }

        TargetingApi.SetTarget(pawn, target);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnCastImmobilize(NetworkIdComponent caster)
    {
        if (IsMasterClient)
        {
            var character = GetPawnByNetworkId(caster);
            if (character == null)
            {
                Logging.LogNull(nameof(caster));
                return;
            }
            ImmobilizeUtils.CastImmobilize(character);
        }
    }

    [RpcEvent(RelayMode.Others)]
    private void OnTriggerImmobilize(TriggerImmobilizeData data)
    {
        var caster = GetPawnByNetworkId(data.PlayerId);
        var target = GetPawnByNetworkId(data.Target);
        ImmobilizeUtils.TriggerImmobilize(caster, target, data.GreatSageTalentActiveBuff);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnRelieveImmobilize(NetworkIdComponent affected)
    {
        var character = GetPawnByNetworkId(affected);
        if (character == null)
        {
            Logging.LogNull(nameof(affected));
            return;
        }
        ImmobilizeUtils.RelieveImmobilize(character);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnBreakImmobilize(NetworkIdComponent entity)
    {
        // TODO
        Logging.LogWarning("BreakImmobilize not implemented");
    }

    [RpcEvent(RelayMode.All, EventCaching.AddToRoomCacheGlobal)]
    private static void OnChatMessage(ChatMessage message)
    {
        WukongChatter.OnGetMessage(message);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnPhantomRush(short __sender, ESkillDirection direction)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var playerState = Client.GetPlayerById(__sender);
            if (playerState?.Pawn == null)
            {
                Logging.LogError("Player not found: {PlayerId}", __sender);
                return;
            }

            Logging.LogDebug("Received phantom rush for player {Nickname} in direction {Direction}", playerState.NickName, direction);
            var events = BUS_EventCollectionCS.Get(playerState.Pawn);
            events?.Evt_TriggerPhantomRush.Invoke(direction);

            PlayerUtils.ResetCooldown(playerState.Pawn);
            PlayerUtils.ResetMana(playerState.Pawn);
        }, nameof(OnPhantomRush));
    }

    [RpcEvent(RelayMode.All)]
    public void OnBroadcastPlayerTransform(PlayerTransformData data)
    {
        // TODO: Use targeted RPC mode (select which peers to send to)
        if (data.PlayerId != RelayClient.PeerId)
            return;

        PlayerUtils.TeleportLocalPlayer(data.Location, data.Rotation, false);
    }

    [RpcEvent(RelayMode.All)]
    private void OnPvpEvent(int[] data)
    {
        // TODO: Not QueueOnGameThread, why?
        var ev = (PvPEvent)data[0];
        var winnerTeamId = data[1];

        Logging.LogDebug("Received PvP event: {Event}", ev);

        switch (ev)
        {
            case PvPEvent.RoundStart:
                Task.Run(PvPUtils.ShowPvPCountDown);
                WukongMP.Instance.StartRound();
                WukongMP.Instance.EnablePvP();
                Client.EnterPvP();
                break;
            case PvPEvent.RoundEnd:
                WukongMP.Instance.DisablePvP();
                WukongMP.Instance.EndRound();

                if (winnerTeamId == Constants.DrawTeamId)
                {
                    UIUtils.ShowTip(Texts.RoundDraw);
                }
                else
                {
                    UIUtils.ShowTip(string.Format(Texts.RoundEndedWinner, PvPUtils.GetLocalizedTeamName(winnerTeamId)));
                }

                if (winnerTeamId == Constants.DrawTeamId)
                    return;

                if (winnerTeamId == Client.LocalPlayerState.TeamId)
                {
                    AssetUtils.PlayBossDefeatedSound();
                }

                break;
            case PvPEvent.TournamentEnd:
            {
                if (winnerTeamId == Constants.DrawTeamId)
                {
                    UIUtils.ShowTip(Texts.TournamentDraw);
                }
                else
                {
                    UIUtils.ShowTip(string.Format(Texts.TournamentEndedWinner, PvPUtils.GetLocalizedTeamName(winnerTeamId)));
                }

                Task.Run(async () =>
                {
                    if (IsMasterClient)
                    {
                        foreach (var playerState in Client.SpectatingPlayers)
                        {
                            Client.SetRemotePlayerProperty(playerState.PeerId, nameof(PlayerState.IsSpectator), false);
                        }
                    }

                    await Task.Delay(2000);
                    PvPUtils.EndTournament();
                    Client.ExitPvP();
                    Client.LocalPlayerState.IsReadyForPvP = false;
                    Client.SetReadyState(false);
                });

                break;
            }
            case PvPEvent.ResetStats:
                WukongMP.Instance.ResetRoundState();

                if (!Client.LocalPlayerState.IsDead)
                {
                    Utils.TryRunOnGameThread(() =>
                    {
                        TamerUtils.DestroyAllTamers();
                        var events = BUS_EventCollectionCS.Get(Client.LocalPlayerState.Pawn!);

                        if (events == null)
                        {
                            Logging.LogError("events are null");
                            return;
                        }

                        events.Evt_TriggerTeleportResetPlayer!.Invoke();
                    });
                }

                if (IsMasterClient)
                {
                    // reset other players' Hp to HpMax if they are not dead
                    foreach (var (key, state) in Client.ConnectedPlayers)
                    {
                        if (!state.IsDead)
                        {
                            if (state.Pawn == null)
                            {
                                Logging.LogError("Pawn is null in {Patch}", nameof(OnPvpEvent));
                                return;
                            }

                            var attrContainer = (BUC_AttrContainer?)BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(state.Pawn);
                            if (attrContainer != null)
                            {
                                var hpMax = attrContainer.GetFloatValue(EBGUAttrFloat.HpMax);
                                attrContainer.SetFloatValue(EBGUAttrFloat.Hp, hpMax);
                                state.Hp = hpMax;
                                Client.SetRemotePlayerProperty(key, nameof(PlayerState.Hp), state.Hp);
                            }
                        }
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ev));
        }
    }

    [RpcEvent(RelayMode.Master)]
    private void OnSuicide(short __sender)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var player = Client.GetPlayerById(__sender)?.Pawn;
            if (player == null)
                return;

            var events = BUS_EventCollectionCS.Get(player);
            events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Hp, -2000f);
            if (IsMasterClient)
            {
                events?.Evt_UnitDead.Invoke(player, EDeadReason.Suicide);
            }
        }, nameof(OnSuicide));
    }

    [RpcEvent(RelayMode.All)]
    private static void OnRebirthPlayer(short peerId)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            Logging.LogDebug("RebirthPlayer for player {PlayerId} called", peerId);

            var player = Client.GetPlayerById(peerId);
            if (player == null)
                return;

            if (player.PeerId == Client.LocalPlayerState.PeerId)
            {
                FreeCameraManager.Instance.LeaveFreeCameraMode();
            }

            var events = BUS_EventCollectionCS.Get(player.Pawn);
            if (events != null)
            {
                events.Evt_OnLeaveFalling.Invoke(); // Reset falling timer.
                events.Evt_RebirthTeleportFinish.Invoke(ERebirthType.RebirthPoint); // Rest state and play anim montage.
                events.Evt_TriggerTeleportResetPlayer.Invoke(); // Reset player stats, will set IsDead flag to false.
            }
        }, nameof(OnRebirthPlayer));
    }

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
            var playerState = Client.GetPlayerById(__sender);
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
    public void OnMontageCallback(MontageCallbackData data)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var id = data.NetId;
            var pawn = GetPawnByNetworkId(id);
            if (pawn == null)
            {
                Logging.LogNull(nameof(data.NetId));
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
                Logging.LogNull(nameof(data.NetworkId));
                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);
            if (events == null)
            {
                Logging.LogError("Failed to get event collection for unit {Unit}", pawn.GetName());
                return;
            }

            Logging.LogDebug("OnUnitDead for unit {Unit}", pawn.GetName());
            events.Evt_UnitDead.Invoke(GameUtils.GetControlledPawn(), data.DeadReason, data.DmgId, data.StiffLevel, null, default, data.IsDotDmg, data.AbnormalType);
        }, nameof(OnUnitDead));
    }

    [RpcEvent(RelayMode.Others)]
    private void OnWaitingForSequence(short __sender, SequenceWaitingData data)
    {
        CutsceneUtils.SetWaitingForCutsceneStatus(__sender, data);
    }

    [RpcEvent(RelayMode.Others)]
    private void OnIronBodyStart(short __sender)
    {
        var player = Client.GetPlayerById(__sender);
        if (player == null)
        {
            Logging.LogError("Player not found: {Id}", __sender);
            return;
        }
        if (player.Pawn == null)
        {
            Logging.LogError("Player pawn is null for player {Id}", __sender);
            return;
        }
        IronBodyUtils.TriggerIronBody(player.Pawn);
    }

    [RpcEvent(RelayMode.Master)]
    void OnUnitSpawned(NetworkIdComponent netEntity)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            if (NetManager.TryGetEntityByNetworkId(netEntity, out var entity))
            {
                if (entity.HasValue)
                {
                    TamerUtils.AddSpawnedUnit(entity.Value);
                }
            }
        }, nameof(OnUnitSpawned));
    }

    [RpcEvent(RelayMode.Master)]
    void OnUnitDespawn(NetworkIdComponent netEntity)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            if (NetManager.TryGetEntityByNetworkId(netEntity, out var entity))
            {
                if (entity.HasValue)
                {
                    TamerUtils.SubtractSpawnedUnit(entity.Value);
                }
            }
        }, nameof(OnUnitDespawn));
    }
}