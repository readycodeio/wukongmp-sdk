using System;
using b1;
using BtlShare;
using WukongMp.Api.Configuration;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.PvP.Configuration
{
    internal class PvpGameplayConfiguration : IDisposable
    {
        private readonly GameplayConfiguration _configuration;
        private readonly WukongAreaState _areaState;

        public PvpGameplayConfiguration(GameplayConfiguration configuration, WukongAreaState areaState)
        {
            _configuration = configuration;
            _areaState = areaState;

            ConfigurePvpGameplay();
        }

        public void ConfigurePvpGameplay()
        {
            _configuration.IsSupportMultiLockEnabled = false;
            _configuration.IsStrongDamageImmueEnabled = true;
            _configuration.EnableCustomCameraArmLength = true;
            _configuration.EnableSpawnedTamers = true;
            _configuration.DisableCutscenes = true;
            _configuration.SyncTamerTeamFromGameToEcs = false;
            _configuration.OverrideLocalPlayerTeamFromGlobalEntity = true;

            _configuration.SetDisableTamerAttackQuery(ShouldDisableTamerAttack);
            _configuration.SetIsSkillEnabledQuery(IsSkillEnabled);

            _configuration.EnableCustomIsPlayerInBattle = true;
            _configuration.SetIsPlayerInBattleQuery(() => _areaState.PvpState?.InPvP ?? false);
            _configuration.SetIsInteractionAllowedQuery(IsInteractAllowed);
            _configuration.SetIsTamerNotSynchronizedQuery(IsTamerNotSynchronized);
            _configuration.SetIsAreaOverlapDisabledQuery(IsAreaOverlapDisabled);
        }

        public void Dispose()
        {
            _configuration.ClearDisableTamerAttackQuery();
            _configuration.ClearIsSkillEnabledQuery();
        }

        private bool ShouldDisableTamerAttack()
        {
            return _areaState.PvpState is { InPvP: false };
        }

        private bool IsSkillEnabled(int skillId)
        {
            var areaEntity = _areaState.CurrentArea;
            if (areaEntity == null)
                return true;

            var room = areaEntity.Value.GetRoom();

            switch (skillId)
            {
                // Note: Phantom Rush is not a skill in code
                case Constants.ImmobilizeSkillId when !room.ImmobilizeAllowed:
                case Constants.GourdSkillId when !room.GourdAllowed:
                case Constants.ConsumableBuffSkillId when !room.ConsumablesAllowed:
                case Constants.IncenseTrailTalismanSkillId:
                case Constants.RuyiScrollSkillId:
                    return false;
                default:
                    // more skills here
                    return true;
            }
        }

        private bool IsInteractAllowed(EInteractType interactType)
        {
            return interactType != EInteractType.StandardObj && interactType != EInteractType.TaskNpc;
        }

        private bool IsTamerNotSynchronized(string guid)
        {
            var currentLevelId = BGUFuncLibMap.GetCurLevelId(GameUtils.GetWorld());
            var levelTamers = LevelTamersConfig.GetLevelTamers(currentLevelId);
            return levelTamers.Contains(guid);
        }

        private bool IsAreaOverlapDisabled(string guid)
        {
            var currentLevelId = BGUFuncLibMap.GetCurLevelId(GameUtils.GetWorld());
            var disabledAreas = LevelDisabledAreasConfig.GetDisabledAreas(currentLevelId);
            return disabledAreas.Contains(guid);
        }
    }
}
