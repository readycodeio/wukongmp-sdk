using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using System;
using System.Collections.Generic;
using System.Numerics;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;
using WukongMp.PvP.Configuration;

namespace WukongMp.Api.ECS.Systems;

internal class PvpAntiStallSystem(WukongAreaState areaState, WukongRpcCallbacks rpc)
    : QuerySystem<LocalMainCharacterComponent, MetadataComponent, TeamComponent>
{
    private struct PlayerEngagementData
    {
        public FVector LastPosition;
        public FVector ForwardDirection;
        public int TeamId;
        public bool IsAttacking;
        public float CurrentHp;
        public float PrevHp;
    }

    private enum AntiStallState
    {
        Monitoring,
        Warning,
        Active
    }

    private AntiStallState _state = AntiStallState.Monitoring;

    private const ulong TickInterval = 10; // Check every 10 ticks
    private ulong _tickCounter;
    private float _elapsedTime;
    private bool _isReset = false;

    private float _warningTimer;
    private float _activeTimer;

    private float _roomEngagementScore;
    private readonly Dictionary<NetworkId, float> _playerEngagementMultipliers = [];
    private readonly Dictionary<NetworkId, PlayerEngagementData> _playerEngagementData = [];
    private readonly Random _rng = new();

    private int _decayRounds = 0;

    protected override void OnUpdate()
    {
        // TODO: Run if enabled in config - areaState.CurrentArea.Value.Room.AntiStallEnabled

        if (!areaState.CurrentArea.HasValue || !areaState.OwnsPvpState)
            return;

        if (areaState.PvpState is not { InPvP: true })
        {
            ResetState();
            return;
        }
        _isReset = false;

        if (_tickCounter++ % TickInterval != 0)
        {
            _elapsedTime += Tick.deltaTime;
            return;
        }

        Query.ForEachEntity((ref localMainCharacter, ref metadata, ref team, entity) =>
        {
            var playerId = metadata.NetId;
            if (!_playerEngagementData.TryGetValue(playerId, out PlayerEngagementData data))
            {
                data=new PlayerEngagementData();
                _playerEngagementData[playerId] = data;
            }
            var pawn = localMainCharacter.Pawn;
            if (pawn != null)
            {
                data.LastPosition = pawn.GetActorLocation();
                data.ForwardDirection = pawn.GetActorForwardVector();
                data.TeamId = team.TeamId;
                data.IsAttacking = BGUFunctionLibraryCS.BGUHasUnitState(pawn, EBGUUnitState.Attacking);
                data.PrevHp = data.CurrentHp;
                data.CurrentHp = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(pawn).GetFloatValue(EBGUAttrFloat.Hp);
            }
            _playerEngagementData[playerId] = data;
        });

        UpdatePlayerMultipliers();
        UpdateEngagementScore();
        UpdateState();

        if (_state == AntiStallState.Warning)
        {
            _warningTimer += _elapsedTime;
            if (_warningTimer >= AntiStallConfig.WarningDuration)
            {
                SetActiveState();
            }
        }
        if (_state == AntiStallState.Active)
        {
            _activeTimer += _elapsedTime;
            if (_activeTimer >= AntiStallConfig.ActiveDuration)
            {
                _decayRounds++;
                SetMonitoringState();
            }
        }

        _elapsedTime = 0f;
    }

    private void UpdateEngagementScore()
    {
        foreach (var kvp in _playerEngagementData)
        {
            var data = kvp.Value;
            if (data.IsAttacking)
            {
                _roomEngagementScore += _elapsedTime * AntiStallConfig.AttackRoomEngagementScore;
            }
            if (!data.CurrentHp.Equals(data.PrevHp, Constants.FloatComparisonTolerance))
            {
                _roomEngagementScore += AntiStallConfig.DamageRoomEngagementScore;
            }
        }
        _roomEngagementScore = FMath.Min(_roomEngagementScore, AntiStallConfig.MaxRoomEngagementScore);
        _roomEngagementScore -= _elapsedTime * AntiStallConfig.RoomEngagementDecayScore;
        _roomEngagementScore = FMath.Max(_roomEngagementScore, 0f);
    }

    private void UpdatePlayerMultipliers()
    {
        var _playerFacingDictionary = CalculatePlayerFacing();
        foreach (var playerId in _playerEngagementData.Keys)
        {
            float current = _playerEngagementMultipliers.TryGetValue(playerId, out var val) ? val : 1.0f;

            if (_playerFacingDictionary.TryGetValue(playerId, out bool isFacing) && isFacing)
            {
                current = MathF.Max(current - AntiStallConfig.PlayerEngagementMultiplierIncrease * _elapsedTime, AntiStallConfig.PlayerEngagementMultiplierMin);
            }
            else
            {
                current = MathF.Min(current + AntiStallConfig.PlayerEngagementMultiplierDecay * _elapsedTime, AntiStallConfig.PlayerEngagementMultiplierMax);
            }

            _playerEngagementMultipliers[playerId] = current;
        }
    }

    private Dictionary<NetworkId, bool> CalculatePlayerFacing()
    {
        var _playerFacingDictionary = new Dictionary<NetworkId, bool>();
        var playerIds = new List<NetworkId>(_playerEngagementData.Keys);
        for (int i = 0; i < playerIds.Count; i++)
        {
            var idA = playerIds[i];
            var dataA = _playerEngagementData[idA];
            if (_playerFacingDictionary.TryGetValue(idA, out bool isFacingEnemyA) && isFacingEnemyA)
                continue;

            for (int j = i + 1; j < playerIds.Count; j++)
            {
                var idB = playerIds[j];
                var dataB = _playerEngagementData[idB];
                if (dataA.TeamId == dataB.TeamId)
                    continue;

                var dirAtoB = Vector3.Normalize(dataB.LastPosition.ToVector3() - dataA.LastPosition.ToVector3());
                var dirBtoA = -dirAtoB;
                float facingA = Vector3.Dot(dataA.ForwardDirection.ToVector3(), dirAtoB);
                float facingB = Vector3.Dot(dataB.ForwardDirection.ToVector3(), dirBtoA);
                if (facingA > AntiStallConfig.PlayersFacingThreshold)
                {
                    _playerFacingDictionary[idA] = true;
                }
                if (facingB > AntiStallConfig.PlayersFacingThreshold)
                {
                    _playerFacingDictionary[idB] = true;
                }
            }
            if (!_playerFacingDictionary.ContainsKey(idA))
                _playerFacingDictionary[idA] = false;
        }
        return _playerFacingDictionary;
    }

    private void UpdateState()
    {
        if (_roomEngagementScore > AntiStallConfig.RoomEngagementThreshold && _state == AntiStallState.Warning)
        {
            SetMonitoringState();
        }
        if (_roomEngagementScore < AntiStallConfig.RoomEngagementThreshold && _state == AntiStallState.Monitoring)
        {
            SetWarningState();
        }
    }

    private void SetMonitoringState()
    {
        _state = AntiStallState.Monitoring;
        rpc.SendHideAntiStall();
    }

    private void SetWarningState()
    {
        _state = AntiStallState.Warning;
        _warningTimer = 0f;
        rpc.SendShowAntiStallWarning(AntiStallConfig.WarningDuration);
    }

    private void SetActiveState()
    {
        _state = AntiStallState.Active;
        _activeTimer = 0f;
        rpc.SendShowAntiStallAction();
        var baseDecayRate = AntiStallConfig.BaseAttributeDecayRate + AntiStallConfig.AttributeDecayMultiplier * _decayRounds;
        foreach (var kvp in _playerEngagementMultipliers)
        {
            var playerId = kvp.Key;
            var multiplier = kvp.Value;
            var randomCoefficient = GetRandomCoefficient();
            var scaledDecay = baseDecayRate * multiplier * AntiStallConfig.ActiveDuration * randomCoefficient;
            Logging.LogDebug("Applying anti-stall decay to player {0}: baseDecayRate={1}, multiplier={2}, random={3}, scaledDecay={4}", playerId, baseDecayRate, multiplier, randomCoefficient, scaledDecay);  
            rpc.SendStallDamage(playerId, scaledDecay);
        }
    }

    private float GetRandomCoefficient()
    {
        return AntiStallConfig.RandomCoefficientMin + (float)_rng.NextDouble() * (AntiStallConfig.RandomCoefficientMax - AntiStallConfig.RandomCoefficientMin);
    }

    private void ResetState()
    {
        if (_isReset)
            return;

        _isReset = true;
        _state = AntiStallState.Monitoring;
        _decayRounds = 0;
        _roomEngagementScore = AntiStallConfig.MaxRoomEngagementScore;
        _playerEngagementMultipliers.Clear();
        _playerEngagementData.Clear();
        rpc.SendHideAntiStall();
    }
}
