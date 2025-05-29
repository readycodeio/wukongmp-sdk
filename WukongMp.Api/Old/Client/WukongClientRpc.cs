using System;
using System.Linq;
using b1;
using BtlShare;
using LiteNetLib;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Old.DTO;
using WukongMp.Api.Old.State;

namespace WukongMp.Api.Old.Client;

public sealed partial class WukongClient
{
    public event Action<NetworkIdComponent, string, string, int, float, float, float>? OnUnitSpawn;
    public event Action<NetworkIdComponent, NetworkIdComponent, string, string, int>? OnSummonSpawn;
    public event Action<short, EquipmentState>? OnEquipmentChange;
    public event Action<string, bool, int>? OnReadinessChange;
    public event Action<PlayerState, int>? OnTeamChange;
    public event Action<PlayerState>? OnPlayerLeft;
    public event Action? OnBeforeJoinRoom;
    public event Action<short>? OnExitPhantomRush;
    public event Action? OnMatchmakingEnded;
    public event Action<short, int, float>? OnBuffAdded;
    public event Action<short, int, EBuffEffectTriggerType, int, bool>? OnBuffRemoved;
    public event Action<short, EBuffEffectTriggerType, bool>? OnBuffAllRemoved;
    public event Action<NetworkIdComponent, EBUStateTrigger, float, bool>? OnStateTriggerSet;
    public event Action<NetworkIdComponent, EBGUSimpleState, bool>? OnSimpleStateSet;
    public event Action<NetworkIdComponent, string>? OnFsmStateSet;
    public event Action<NetworkIdComponent, EState_MM>? OnMotionMatchingChanged;
    public event Action<short, string, int, int>? OnRequestSpawnUnits;
    public event Action<short, int, int, bool, EPlayerTransBeginType>? OnPlayerTransBegin;
    public event Action<short, int, int, bool, EPlayerTransEndType>? OnPlayerTransEnd;
    public event Action<FPlayMovieRequest>? OnPlayMovieRequest;
    public event Action<short, int>? OnWaitingForMovie;

    private void OnCustomEvent(CustomEventHeader header, NetPacketReader reader)
    {
        switch (header.EventCode)
        {
            case 150:
                // unit spawn
                var unitData = RelayClient.DeserializeObject<UnitSpawnData>(reader);
                OnUnitSpawn?.Invoke(unitData.Id, unitData.Guid, unitData.Name, unitData.TeamId, unitData.X, unitData.Y, unitData.Z);
                break;
            case 14:
                // exit phantom rush
                var phantomRushPlayerId = RelayClient.DeserializeObject<short>(reader);
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
                OnMotionMatchingChanged?.Invoke(new NetworkIdComponent((short)mmdata[0], (uint)mmdata[1]), (EState_MM)mmdata[2]);
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
                OnPlayMovieRequest?.Invoke(new FPlayMovieRequest
                {
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
                OnWaitingForMovie?.Invoke(header.Sender, sequenceId);
                break;
        }
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

    public void SendMotionMatchingState(NetworkIdComponent characterId, EState_MM MMState)
    {
        const byte eventCode = 22;
        int[] evData = [characterId.Owner, (int)characterId.Id, (int)MMState];
        RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
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