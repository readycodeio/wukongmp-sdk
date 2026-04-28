using System;
using BtlShare;

namespace WukongMp.Sdk.Api;

/// <summary>
/// Provides ways to configure various aspects othe game's behavior.
/// Will be replaced in the future with more specific configuration APIs, but for now serves as a catch-all for miscellaneous configuration options that don't fit anywhere else.
/// Hence, we do not document the individual configuration options here, as they are all subject to change and may be removed in the future without a major version bump.
/// </summary>
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