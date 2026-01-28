using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using System.Collections.Generic;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;
using WukongMp.PvP.Configuration;

namespace WukongMp.Api.ECS.Systems;

internal class PvpAntiStallSystem(WukongAreaState areaState, WukongRpcCallbacks rpc)
    : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent, PvPComponent, TeamComponent>
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
    private readonly Dictionary<PlayerId, float> _playerEngagementMultipliers = [];
    private readonly Dictionary<PlayerId, PlayerEngagementData> _playerEngagementData = [];

    private int _decayRounds = 0;

    protected override void OnUpdate()
    {
        // if enabled in config

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

        Query.ForEachEntity((ref localMainCharacter, ref mainCharacter, ref pvp, ref team, entity) =>
        {
            var playerId = mainCharacter.PlayerId;
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
                IBUC_BeAttackData beAttackData = BGU_DataUtil.GetReadOnlyData<IBUC_BeAttackData, BUC_BeAttackData>(pawn);
                if (beAttackData != null && beAttackData.GetAttacker() == null)
                    data.PrevHp = data.CurrentHp; // Do not count damage from environment
            }
            _playerEngagementData[playerId] = data;
        });

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
            var decayRate = (AntiStallConfig.BaseDecayRate + AntiStallConfig.DecayMultiplier * _decayRounds) * Tick.deltaTime;
            rpc.SendStallDamage(decayRate);
            // TODO: Per player rpc to apply corrective action with multiplier based on their engagement score
            if (_activeTimer >= AntiStallConfig.ActiveDuration)
            {
                _decayRounds++;
                SetWarningState();
            }
        }

        _elapsedTime = 0f;
    }

    private void UpdateEngagementScore()
    {
        foreach (var kvp in _playerEngagementData)
        {
            var playerId = kvp.Key;
            var data = kvp.Value;
            // TODO: Add check if players from opposing teams are close to each other or facing each other

            if (data.IsAttacking)
            {
                _roomEngagementScore += _elapsedTime * AntiStallConfig.AttackEngagementScore;
            }
            if (data.CurrentHp < data.PrevHp)
            {
                _roomEngagementScore += _elapsedTime * AntiStallConfig.DamageEngagementScore;
            }
        }
        _roomEngagementScore = FMath.Min(_roomEngagementScore, AntiStallConfig.MaxEngagementScore);
        _roomEngagementScore -= _elapsedTime * AntiStallConfig.EngagementDecayScore;
        _roomEngagementScore = FMath.Max(_roomEngagementScore, 0f);
    }

    private void UpdateState()
    {
        if (_roomEngagementScore > AntiStallConfig.EngagementThreshold && _state != AntiStallState.Monitoring)
        {
            SetMonitoringState();
        }
        if (_roomEngagementScore < AntiStallConfig.EngagementThreshold && _state == AntiStallState.Monitoring)
        {
            SetWarningState();
        }
    }

    private void SetMonitoringState()
    {
        _state = AntiStallState.Monitoring;
        rpc.SendHideAntiStallWarning();
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
        //widgetManager.ShowAntiStallActiveWarning();
    }

    private void ResetState()
    {
        if (_isReset)
            return;

        _isReset = true;
        _state = AntiStallState.Monitoring;
        _decayRounds = 0;
        _roomEngagementScore = AntiStallConfig.MaxEngagementScore;
        _playerEngagementMultipliers.Clear();
        _playerEngagementData.Clear();
        rpc.SendHideAntiStallWarning();
    }
}
