using System;
using BtlShare;

namespace WukongMp.Sdk.Api;

public interface IWukongConfigurationApi
{
    bool IsSupportMultiLockEnabled { get; set; }
    bool IsStrongDamageImmueEnabled { get; set; }
    bool EnableCustomCameraArmLength { get; set; }
    bool DeleteDestroyedTamersFromEcs { get; set; }
    bool SyncTamerTeamFromGameToEcs { get; set; }
    bool DisableCutscenes { get; set; }
    bool OverrideLocalPlayerTeamFromGlobalEntity { get; set; }

    string GetLaunchParameter(string key, string defaultValue);
    void SetDisableTamerAttackQuery(Func<bool> shouldDisableTamerAttack);
    void SetIsSkillEnabledQuery(Func<int, bool> isSkillEnabled);
    void SetIsPlayerInBattleQuery(Func<bool> isPlayerInBattle);
    void SetIsInteractionAllowedQuery(Func<EInteractType, bool> isInteractAllowed);
    void SetIsTamerNotSynchronizedQuery(Func<string, bool> isTamerNotSynchronized);
    void SetIsAreaOverlapDisabledQuery(Func<string, bool> isAreaOverlapDisabled);

    void ClearDisableTamerAttackQuery();
    void ClearIsSkillEnabledQuery();
    void ClearIsPlayerInBattleQuery();
    void ClearIsInteractionAllowedQuery();
    void ClearIsTamerNotSynchronizedQuery();
    void ClearIsAreaOverlapDisabledQuery();
}