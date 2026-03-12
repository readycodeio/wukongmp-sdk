using System;
using BtlShare;
using Microsoft.Extensions.Logging;

namespace WukongMp.Api.Configuration;

internal class GameplayConfiguration(ILogger logger)
{
    public bool IsSupportMultiLockEnabled { get; set; } = false;
    public bool IsStrongDamageImmueEnabled { get; set; } = false;
    public bool EnableCustomCameraArmLength { get; set; } = false;
    public bool EnableSpawnedTamers { get; set; } = false;
    public bool DisableCutscenes { get; set; } = false;

    [Obsolete("To be replaced by data sync direction after refactoring")]
    public bool SyncTamerTeamFromGameToEcs { get; set; } = false;

    [Obsolete("To be replaced by data sync direction after refactoring")]
    public bool OverrideLocalPlayerTeamFromGlobalEntity { get; set; } = false;

    private Func<bool>? disableTamerAttackQuery;

    public void SetDisableTamerAttackQuery(Func<bool> query)
    {
        if (disableTamerAttackQuery is not null)
            logger.LogError("DisableTamerAttackQuery is already set. Overriding the existing query.");

        disableTamerAttackQuery = query;
    }

    public void ClearDisableTamerAttackQuery()
    {
        disableTamerAttackQuery = null;
    }

    public bool ShouldDisableTamerAttack() => disableTamerAttackQuery?.Invoke() ?? false;

    private Func<int, bool>? isSkillEnabledQuery;

    public void SetIsSkillEnabledQuery(Func<int, bool> query)
    {
        if (isSkillEnabledQuery is not null)
            logger.LogError("IsSkillEnabledQuery is already set. Overriding the existing query.");

        isSkillEnabledQuery = query;
    }

    public void ClearIsSkillEnabledQuery()
    {
        isSkillEnabledQuery = null;
    }

    public bool IsSkillEnabled(int skillId) => isSkillEnabledQuery?.Invoke(skillId) ?? true;

    // Custom IsPlayerInBattle
    public bool EnableCustomIsPlayerInBattle { get; set; } = false;
    private Func<bool>? isPlayerInBattleQuery;

    public void SetIsPlayerInBattleQuery(Func<bool> query)
    {
        if (isPlayerInBattleQuery is not null)
            logger.LogError("IsPlayerInBattleQuery is already set. Overriding the existing query.");

        isPlayerInBattleQuery = query;
    }

    public void ClearIsPlayerInBattleQuery()
    {
        isPlayerInBattleQuery = null;
    }

    public bool IsPlayerInBattle() => isPlayerInBattleQuery?.Invoke() ?? false;

    // IsInteractAllowed
    private Func<EInteractType, bool>? isInteractionAllowedQuery;

    public void SetIsInteractionAllowedQuery(Func<EInteractType, bool> query)
    {
        if (isInteractionAllowedQuery is not null)
            logger.LogError("IsInteractionAllowedQuery is already set. Overriding the existing query.");

        isInteractionAllowedQuery = query;
    }

    public void ClearIsInteractionAllowedQuery()
    {
        isPlayerInBattleQuery = null;
    }

    public bool IsInteractionAllowed(EInteractType interactType) => isInteractionAllowedQuery?.Invoke(interactType) ?? true;

    // IsTamerNotSynchronized
    private Func<string, bool>? isTamerNotSynchronizedQuery;

    public void SetIsTamerNotSynchronizedQuery(Func<string, bool> query)
    {
        if (isTamerNotSynchronizedQuery is not null)
            logger.LogError("IsTamerNotSynchronizedQuery is already set. Overriding the existing query.");

        isTamerNotSynchronizedQuery = query;
    }

    public void ClearIsTamerNotSynchronizedQuery()
    {
        isTamerNotSynchronizedQuery = null;
    }

    public bool IsTamerNotSynchronized(string guid) => isTamerNotSynchronizedQuery?.Invoke(guid) ?? true;

    // IsAreaOverlapDisabled
    private Func<string, bool>? isAreaOverlapDisabledQuery;

    public void SetIsAreaOverlapDisabledQuery(Func<string, bool> query)
    {
        if (isAreaOverlapDisabledQuery is not null)
            logger.LogError("IsAreaOverlapDisabledQuery is already set. Overriding the existing query.");

        isAreaOverlapDisabledQuery = query;
    }

    public void ClearIsAreaOverlapDisabledQuery()
    {
        isAreaOverlapDisabledQuery = null;
    }

    public bool IsAreaOverlapDisabled(string guid) => isAreaOverlapDisabledQuery?.Invoke(guid) ?? false;
}