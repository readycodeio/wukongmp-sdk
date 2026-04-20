using System;
using BtlShare;
using WukongMp.Api;
using WukongMp.Api.Configuration;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongConfigurationApi(GameplayConfiguration configuration, LaunchParameters launchParameters) : IWukongConfigurationApi
{
    public bool IsSupportMultiLockEnabled
    {
        get => configuration.IsSupportMultiLockEnabled;
        set => configuration.IsSupportMultiLockEnabled = value;
    }

    public bool IsStrongDamageImmueEnabled
    {
        get => configuration.IsStrongDamageImmueEnabled;
        set => configuration.IsStrongDamageImmueEnabled = value;
    }

    public bool EnableCustomCameraArmLength
    {
        get => configuration.EnableCustomCameraArmLength;
        set => configuration.EnableCustomCameraArmLength = value;
    }

    public bool DeleteDestroyedTamersFromEcs
    {
        get => configuration.DeleteDestroyedTamersFromEcs;
        set => configuration.DeleteDestroyedTamersFromEcs = value;
    }

    public bool SyncTamerTeamFromGameToEcs
    {
        get => configuration.SyncTamerTeamFromGameToEcs;
        set => configuration.SyncTamerTeamFromGameToEcs = value;
    }

    public bool OverrideLocalPlayerTeamFromGlobalEntity
    {
        get => configuration.OverrideLocalPlayerTeamFromGlobalEntity;
        set => configuration.OverrideLocalPlayerTeamFromGlobalEntity = value;
    }

    public bool DisableCutscenes
    {
        get => configuration.DisableCutscenes;
        set => configuration.DisableCutscenes = value;
    }

    public string GetLaunchParameter(string key, string defaultValue)
    {
        return launchParameters.GetParameterOrDefault(key, defaultValue);
    }

    public void SetDisableTamerAttackQuery(Func<bool> shouldDisableTamerAttack) => configuration.SetDisableTamerAttackQuery(shouldDisableTamerAttack);
    public void SetIsSkillEnabledQuery(Func<int, bool> isSkillEnabled) => configuration.SetIsSkillEnabledQuery(isSkillEnabled);

    public void SetIsPlayerInBattleQuery(Func<bool> isPlayerInBattle)
    {
        configuration.EnableCustomIsPlayerInBattle = true;
        configuration.SetIsPlayerInBattleQuery(isPlayerInBattle);
    }

    public void SetIsInteractionAllowedQuery(Func<EInteractType, bool> isInteractAllowed) => configuration.SetIsInteractionAllowedQuery(isInteractAllowed);
    public void SetIsTamerNotSynchronizedQuery(Func<string, bool> isTamerNotSynchronized) => configuration.SetIsTamerNotSynchronizedQuery(isTamerNotSynchronized);
    public void SetIsAreaOverlapDisabledQuery(Func<string, bool> isAreaOverlapDisabled) => configuration.SetIsAreaOverlapDisabledQuery(isAreaOverlapDisabled);

    public void ClearDisableTamerAttackQuery()
    {
        configuration.ClearDisableTamerAttackQuery();
    }

    public void ClearIsSkillEnabledQuery()
    {
        configuration.ClearIsSkillEnabledQuery();
    }

    public void ClearIsPlayerInBattleQuery()
    {
        configuration.EnableCustomIsPlayerInBattle = false;
        configuration.ClearIsPlayerInBattleQuery();
    }

    public void ClearIsInteractionAllowedQuery()
    {
        configuration.ClearIsInteractionAllowedQuery();
    }

    public void ClearIsTamerNotSynchronizedQuery()
    {
        configuration.ClearIsTamerNotSynchronizedQuery();
    }

    public void ClearIsAreaOverlapDisabledQuery()
    {
        configuration.ClearIsAreaOverlapDisabledQuery();
    }
}