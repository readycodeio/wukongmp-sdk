using System;
using System.Linq;
using b1;
using BtlShare;
using LiteNetLib;
using ReadyM.Relay.Common.ECS.Components;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.DTO;
using WukongMp.Api.State;

namespace WukongMp.Api.Client;

public sealed partial class WukongClient
{
    public event Action<MontageCallbackData>? OnMontageCallback;
    public event Action<UnitDeadPacket>? OnUnitDead;
    public event Action<int, NetworkIdComponent, string, string, int, float, float, float>? OnUnitSpawn;
    public event Action<NetworkIdComponent, NetworkIdComponent, string, string, int>? OnSummonSpawn;
    public event Action<int>? OnTeleportFinish;
    public event Action<string>? OnMonsterWakeUp;
    public event Action<int, EquipmentState>? OnEquipmentChange;
    public event Action<string, bool, int>? OnReadinessChange;
    public event Action<PlayerState, int>? OnTeamChange;
    public event Action<PlayerState>? OnPlayerLeft;
    public event Action<int>? OnPlayerRebirth;
    public event Action<int>? OnKillPlayer;
    public event Action<FVector, FRotator>? OnSetPlayerTransform;
    public event Action? OnBeforeJoinRoom;
    public event Action<DamageNumParam>? OnDamageNum;
    public event Action<int, ESkillDirection>? OnPhantomRush;
    public event Action<int>? OnExitPhantomRush;
    public event Action<NetworkIdComponent, NetworkIdComponent, ImmobilizeActionType, bool>? OnHandleImmobilize;
    public event Action<NetworkIdComponent, NetworkIdComponent, bool>? OnTargetSet;
    public event Action? OnMatchmakingEnded;
    public event Action<int, int, float>? OnBuffAdded;
    public event Action<int, int, EBuffEffectTriggerType, int, bool>? OnBuffRemoved;
    public event Action<int, EBuffEffectTriggerType, bool>? OnBuffAllRemoved;
    public event Action<NetworkIdComponent, EBUStateTrigger, float, bool>? OnStateTriggerSet;
    public event Action<NetworkIdComponent, EBGUSimpleState, bool>? OnSimpleStateSet;
    public event Action<NetworkIdComponent, string>? OnFsmStateSet;
    public event Action<NetworkIdComponent, EState_MM>? OnMotionMatchingChanged;
    public event Action<int, string, int, int>? OnRequestSpawnUnits;
    public event Action<int, int, int, bool, EPlayerTransBeginType>? OnPlayerTransBegin;
    public event Action<int, int, int, bool, EPlayerTransEndType>? OnPlayerTransEnd;
    public event Action<FPlayMovieRequest>? OnPlayMoviewRequest;
    public event Action<int>? OnWaitingForMovie;

    public void OnCustomEvent(CustomEventHeader header, NetPacketReader reader)
    {
        switch (header.EventCode)
        {
            case 1:
                // unit spawn
                var unitData = RelayClient.DeserializeObject<UnitSpawnData>(reader);
                OnUnitSpawn?.Invoke(header.Sender, unitData.Id, unitData.Guid, unitData.Name, unitData.TeamId, unitData.X, unitData.Y, unitData.Z);
                break;
            case 2:
                // montage callback
                var montData = RelayClient.DeserializeObject<MontageCallbackData>(reader);
                OnMontageCallback?.Invoke(montData);
                break;
            case 3:
                // unit dead
                var unitDeadData = RelayClient.DeserializeObject<UnitDeadPacket>(reader);
                OnUnitDead?.Invoke(unitDeadData);
                break;
            case 4:
                // teleport finish
                OnTeleportFinish?.Invoke(header.Sender);
                break;
            case 5:
                // monster wake up
                var guid = RelayClient.DeserializeObject<string>(reader);
                OnMonsterWakeUp?.Invoke(guid);
                break;
            case 6:
                // damage num
                var damageNumParam = RelayClient.DeserializeObject<DamageNumParam>(reader);
                OnDamageNum?.Invoke(damageNumParam);
                break;
            case 7:
            {
                // player rebirth
                var playerId = RelayClient.DeserializeObject<int>(reader);
                OnPlayerRebirth?.Invoke(playerId);
                break;
            }
            case 8:
                // PvP event
                var ev = RelayClient.DeserializeObject<int[]>(reader);
                HandlePvPEvent((PvPEvent)ev[0], ev[1]);
                break;
            case 9:
                // kill player
                var id = RelayClient.DeserializeObject<int>(reader);
                OnKillPlayer?.Invoke(id);
                break;
            case 10:
                // player transform
                var playerData = RelayClient.DeserializeObject<PlayerTransformData>(reader);
                if (playerData.PlayerId == LocalPlayerState.PeerId)
                    OnSetPlayerTransform?.Invoke(playerData.Location, playerData.Rotation);
                break;
            case 11:
                // start phantom rush
                var direction = RelayClient.DeserializeObject<ESkillDirection>(reader);
                OnPhantomRush?.Invoke(header.Sender, direction);
                break;
            case 12:
                // immobilize
                var immobilizeData = RelayClient.DeserializeObject<ImmobilizeData>(reader);
                OnHandleImmobilize?.Invoke(immobilizeData.PlayerId, immobilizeData.OtherPlayerId, immobilizeData.ImmobilizeActionType, immobilizeData.GreatSageTalentActiveBuff);
                break;
            case 13:
                // target
                var targetData = RelayClient.DeserializeObject<int[]>(reader);
                OnTargetSet?.Invoke(new NetworkIdComponent((short)targetData[0], (uint)targetData[1]), new NetworkIdComponent((short)targetData[2], (uint)targetData[3]), targetData[4] != 0);
                break;
            case 14:
                // exit phantom rush
                var phantomRushPlayerId = RelayClient.DeserializeObject<int>(reader);
                OnExitPhantomRush?.Invoke(phantomRushPlayerId);
                break;
            case 15:
                // end matchmaking phase
                OnMatchmakingEnded?.Invoke();
                break;
            case 16:
                // buff add
                var buffData = RelayClient.DeserializeObject<byte[]>(reader);
                var buffId = BitConverter.ToInt32(buffData, 0);
                var buffDuration = BitConverter.ToSingle(buffData, 4);
                OnBuffAdded?.Invoke(header.Sender, buffId, buffDuration);
                break;
            case 17:
                // buff remove
                var data = RelayClient.DeserializeObject<int[]>(reader);
                OnBuffRemoved?.Invoke(header.Sender, data[0], (EBuffEffectTriggerType)data[1], data[2], data[3] != 0);
                break;
            case 18:
                // buff all remove
                var evData = RelayClient.DeserializeObject<byte[]>(reader);
                OnBuffAllRemoved?.Invoke(header.Sender, (EBuffEffectTriggerType)evData[0], evData[1] != 0);
                break;
            case 19:
                // state trigger
                var stateTriggerData = RelayClient.DeserializeObject<StateTriggerData>(reader);
                OnStateTriggerSet?.Invoke(stateTriggerData.NetId, stateTriggerData.Trigger, stateTriggerData.Time, stateTriggerData.NeedForceUpdate);
                break;
            case 20:
                // simple state
                var simpleStateData = RelayClient.DeserializeObject<SimpleStateData>(reader);
                OnSimpleStateSet?.Invoke(simpleStateData.NetId, simpleStateData.SimpleState, simpleStateData.IsRemove);
                break;
            case 21:
                // fsm state
                var fsmStateData = RelayClient.DeserializeObject<FsmStateData>(reader);
                OnFsmStateSet?.Invoke(fsmStateData.NetId, fsmStateData.FsmStateName);
                break;
            case 22:
                // motion matching
                var mmdata = RelayClient.DeserializeObject<int[]>(reader);
                OnMotionMatchingChanged?.Invoke(new NetworkIdComponent((short)mmdata[0], (uint)mmdata[1]), (EState_MM)mmdata[1]);
                break;
            case 23:
                // chat message received
                var chatMessage = RelayClient.DeserializeObject<ChatMessage>(reader);
                WukongChatter.OnGetMessage(chatMessage);
                break;
            case 24:
                // spawn summon
                var summonData = RelayClient.DeserializeObject<UnitSummonData>(reader);
                OnSummonSpawn?.Invoke(summonData.SummonerId, summonData.SummonId, summonData.Guid, summonData.Name, summonData.TeamId);
                break;
            case 25:
                // spawn request 
                var spawnRequestData = RelayClient.DeserializeObject<UnitSpawnRequestData>(reader);
                OnRequestSpawnUnits?.Invoke(header.Sender, spawnRequestData.UnitName, spawnRequestData.Count, spawnRequestData.TeamId);
                break;
            case 26:
                // begin transform request 
                var transBeginRequestData = RelayClient.DeserializeObject<PlayerTransBeginData>(reader);
                OnPlayerTransBegin?.Invoke(header.Sender, transBeginRequestData.UnitResId, transBeginRequestData.UnitBornSkillId, transBeginRequestData.EnableBlendViewTarget, transBeginRequestData.TransBeginType);
                break;
            case 27:
                // end transform request 
                var transEndRequestData = RelayClient.DeserializeObject<PlayerTransEndData>(reader);
                OnPlayerTransEnd?.Invoke(header.Sender, transEndRequestData.UnitResId, transEndRequestData.UnitBornSkillId, transEndRequestData.EnableBlendViewTarget, transEndRequestData.TransEndType);
                break;
            case 28:
                // end transform request 
                var playMovieData = RelayClient.DeserializeObject<PlayMovieData>(reader);
                OnPlayMoviewRequest?.Invoke(new FPlayMovieRequest {
                    SequenceID = playMovieData.SequenceID,
                    bDisablePlayerControl = playMovieData.DisablePlayerControl,
                    bDisableMovementInput = playMovieData.DisableMovementInput,
                    bDisableLookAtInput = playMovieData.DisableLookAtInput,
                    bHidePlayer = playMovieData.HidePlayer,
                    bHideHud = playMovieData.HideHud,
                    OverlapBoxGuid = playMovieData.OverlapBoxGuid,
                    MatchType = playMovieData.MatchType,
                });
                break;
             case 29:
                // waiting for movie
                var sequenceId = RelayClient.DeserializeObject<int>(reader);
                OnWaitingForMovie?.Invoke(sequenceId);
                break;
        }
    }

    public void SendMontageCallback(NetworkIdComponent netId, UAnimMontage montage, float position, bool reset)
    {
        Logging.LogDebug("Sending montage callback: {Montage} {Position}", montage.PathName, position);
        const byte eventCode = 2;

        var shortened = MontageHelpers.CompressMontageName(montage.PathName, out var shortMontagePath);
        var data = shortened ? shortMontagePath : montage.PathName;
        var evData = new MontageCallbackData(netId, shortened, data, position, reset);

        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendMontageCancel(NetworkIdComponent netId)
    {
        Logging.LogDebug("Sending montage cancel");
        const byte eventCode = 2;

        var evData = new MontageCallbackData(netId, false, "", 0f, false);

        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendUnitDead(NetworkIdComponent networkId, EDeadReason deadReason, int dmgId, int stiffLevel, bool isDotDmg, EAbnormalStateType abnormalType)
    {
        const byte eventCode = 3;
        var payload = new UnitDeadPacket(networkId, deadReason, dmgId, stiffLevel, isDotDmg, abnormalType);
        RelayClient.OpRaiseEvent(eventCode, payload, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendTeleportFinish()
    {
        const byte eventCode = 4;
        RelayClient.OpRaiseEvent(eventCode, null, RelayMode.All, DeliveryMethod.ReliableOrdered);
    }

    public void SendMonsterWakeUp(string guid)
    {
        const byte eventCode = 5;
        RelayClient.OpRaiseEvent(eventCode, guid, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendDamageNum(DamageNumParam damageNumParam)
    {
        const byte eventCode = 6;
        RelayClient.OpRaiseEvent(eventCode, damageNumParam, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void BroadcastPlayerRebirth(int playerId)
    {
        const byte eventCode = 7;
        RelayClient.OpRaiseEvent(eventCode, playerId, RelayMode.All, DeliveryMethod.ReliableOrdered);
    }

    public void SendPvPEvent(PvPEvent ev, int data = 0)
    {
        if (!IsMasterClient)
        {
            Logging.LogError("Only room owner can send start countdown.");
            return;
        }

        Logging.LogInformation("Sending PvP event: {Event}", ev);

        const byte eventCode = 8;
        var evData = new[] { (int)ev, data };
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.All, DeliveryMethod.ReliableOrdered);
    }

    public void KillCurrentPlayer()
    {
        const byte eventCode = 9;
        RelayClient.OpRaiseEvent(eventCode, PeerId, RelayMode.Master, DeliveryMethod.ReliableOrdered);
    }

    public void BroadcastPlayerTransform(int playerId, FVector location, FRotator rotation)
    {
        const byte eventCode = 10;
        var evData = new PlayerTransformData(playerId, location, rotation);
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.All, DeliveryMethod.ReliableOrdered);
    }

    public void SendPhantomRush(ESkillDirection phantomRushDir)
    {
        const byte eventCode = 11;
        RelayClient.OpRaiseEvent(eventCode, phantomRushDir, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void BroadcastImmobilize(NetworkIdComponent playerId, NetworkIdComponent otherPlayerId, ImmobilizeActionType immobilizeActionType, bool hasBuff)
    {
        const byte eventCode = 12;
        var evData = new ImmobilizeData(playerId, otherPlayerId, immobilizeActionType, hasBuff);
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendTarget(NetworkIdComponent characterId, NetworkIdComponent targetId, int clearTarget)
    {
        const byte eventCode = 13;
        int[] evData = [characterId.Owner, (int)characterId.Id, targetId.Owner, (int)targetId.Id, clearTarget];
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void ExitPhantomRush(int playerId)
    {
        const byte eventCode = 14;
        RelayClient.OpRaiseEvent(eventCode, playerId, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendEndMatchmaking()
    {
        const byte eventCode = 15;
        RelayClient.OpRaiseEvent(eventCode, null, RelayMode.All, DeliveryMethod.ReliableOrdered);
    }

    private void HandleBuffAdd(int buffid, AActor caster, AActor rootcaster, float duration, EBuffSourceType buffsourcetype, bool brecursed, FBattleAttrSnapShot battleattrsnapshot)
    {
        const byte eventCode = 16;
        byte[] evData = BitConverter.GetBytes(buffid).Concat(BitConverter.GetBytes(duration)).ToArray();
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    private void HandleBuffRemove(int buffid, EBuffEffectTriggerType removetriggertype, int layer, bool withtriggerremmoveeffect)
    {
        const byte eventCode = 17;
        int[] evData = [buffid, (int)removetriggertype, layer, withtriggerremmoveeffect ? 1 : 0];
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    private void HandleBuffAllRemove(EBuffEffectTriggerType removetriggertype, bool withtriggerremmoveeffect)
    {
        const byte eventCode = 18;
        byte[] evData = [(byte)removetriggertype, (byte)(withtriggerremmoveeffect ? 1 : 0)];
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendUnitStateTrigger(NetworkIdComponent netId, EBUStateTrigger trigger, float time, bool needForceUpdate)
    {
        const byte eventCode = 19;
        var evData = new StateTriggerData(netId, trigger, time, needForceUpdate);
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendUnitSimpleState(NetworkIdComponent netId, EBGUSimpleState simpleState, bool isRemove)
    {
        const byte eventCode = 20;
        var evData = new SimpleStateData(netId, simpleState, isRemove);
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendTriggerFsmState(NetworkIdComponent netId, FGameplayTag eventTag)
    {
        const byte eventCode = 21;
        var evData = new FsmStateData(netId, eventTag.TagName.ToString());
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendChatMessage(ChatMessage message)
    {
        const byte eventCode = 23;
        RelayClient.OpRaiseEvent(eventCode, message, EventCaching.AddToRoomCacheGlobal);
    }

    public void SpawnSummon(NetworkIdComponent summonerId, NetworkIdComponent id, string guid, string unitName, int teamId)
    {
        const byte eventCode = 24;
        var evData = new UnitSummonData(summonerId, id, guid, unitName, teamId);
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void RequestSpawnUnits(string enemyName, int count, int teamId)
    {
        const byte eventCode = 25;
        var evData = new UnitSpawnRequestData(enemyName, count, teamId);
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Master, DeliveryMethod.ReliableOrdered);
    }

    public void SendPlayerTransBegin(int unitResId, int unitSkillId, bool blendViewTarget, EPlayerTransBeginType type)
    {
        const byte eventCode = 26;
        var evData = new PlayerTransBeginData(unitResId, unitSkillId, blendViewTarget, type);
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendPlayerTransEnd(int unitResId, int unitSkillId, bool blendViewTarget, EPlayerTransEndType type)
    {
        const byte eventCode = 27;
        var evData = new PlayerTransEndData(unitResId, unitSkillId, blendViewTarget, type);
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }
    
    public void SendPlayMovieRequest(FPlayMovieRequest playMovieRequest)
    {
        const byte eventCode = 28;
        var evData = new PlayMovieData(
            playMovieRequest.SequenceID,
            playMovieRequest.bDisablePlayerControl,
            playMovieRequest.bDisableMovementInput,
            playMovieRequest.bDisableLookAtInput,
            playMovieRequest.bHidePlayer,
            playMovieRequest.bHideHud,
            "",
            playMovieRequest.MatchType);
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }

    public void SendWaitingForMovie(int sequenceId)
    {
        const byte eventCode = 29;
        RelayClient.OpRaiseEvent(eventCode, sequenceId, RelayMode.Others, DeliveryMethod.ReliableOrdered);
    }
}