using System;
using BtlShare;
using Microsoft.Extensions.Logging;

namespace WukongMp.Api.Configuration;

[Obsolete("TODO: Make a more centralized configuration system.")]
public sealed class GameplayConfiguration(ILogger logger)
{
    public bool IsSupportMultiLockEnabled { get; set; } = false;
    public bool IsStrongDamageImmueEnabled { get; set; } = false;
    public bool EnableCustomCameraArmLength { get; set; } = false;
    public bool DeleteDestroyedTamersFromEcs { get; set; } = false;
    public bool DisableCutscenes { get; set; } = false;

    [Obsolete("To be replaced by data sync direction after refactoring")]
    public bool SyncTamerTeamFromGameToEcs { get; set; } = false;

    [Obsolete("To be replaced by data sync direction after refactoring")]
    public bool OverrideLocalPlayerTeamFromGlobalEntity { get; set; } = false;

    private Func<bool>? disableTamerAttackQuery;

    internal void SetDisableTamerAttackQuery(Func<bool> query)
    {
        if (disableTamerAttackQuery is not null)
            logger.LogError("DisableTamerAttackQuery is already set. Overriding the existing query.");

        disableTamerAttackQuery = query;
    }

    internal void ClearDisableTamerAttackQuery()
    {
        disableTamerAttackQuery = null;
    }

    internal bool ShouldDisableTamerAttack() => disableTamerAttackQuery?.Invoke() ?? false;

    private Func<int, bool>? isSkillEnabledQuery;

    internal void SetIsSkillEnabledQuery(Func<int, bool> query)
    {
        if (isSkillEnabledQuery is not null)
            logger.LogError("IsSkillEnabledQuery is already set. Overriding the existing query.");

        isSkillEnabledQuery = query;
    }

    internal void ClearIsSkillEnabledQuery()
    {
        isSkillEnabledQuery = null;
    }

    internal bool IsSkillEnabled(int skillId) => isSkillEnabledQuery?.Invoke(skillId) ?? true;

    // Custom IsPlayerInBattle
    internal bool EnableCustomIsPlayerInBattle { get; set; } = false;
    private Func<bool>? isPlayerInBattleQuery;

    internal void SetIsPlayerInBattleQuery(Func<bool> query)
    {
        if (isPlayerInBattleQuery is not null)
            logger.LogError("IsPlayerInBattleQuery is already set. Overriding the existing query.");

        isPlayerInBattleQuery = query;
    }

    internal void ClearIsPlayerInBattleQuery()
    {
        isPlayerInBattleQuery = null;
    }

    internal bool IsPlayerInBattle() => isPlayerInBattleQuery?.Invoke() ?? false;

    // IsInteractAllowed
    private Func<EInteractType, bool>? isInteractionAllowedQuery;

    internal void SetIsInteractionAllowedQuery(Func<EInteractType, bool> query)
    {
        if (isInteractionAllowedQuery is not null)
            logger.LogError("IsInteractionAllowedQuery is already set. Overriding the existing query.");

        isInteractionAllowedQuery = query;
    }

    internal void ClearIsInteractionAllowedQuery()
    {
        isPlayerInBattleQuery = null;
    }

    internal bool IsInteractionAllowed(EInteractType interactType) => isInteractionAllowedQuery?.Invoke(interactType) ?? true;

    // IsTamerNotSynchronized
    private Func<string, bool>? isTamerNotSynchronizedQuery;

    internal void SetIsTamerNotSynchronizedQuery(Func<string, bool> query)
    {
        if (isTamerNotSynchronizedQuery is not null)
            logger.LogError("IsTamerNotSynchronizedQuery is already set. Overriding the existing query.");

        isTamerNotSynchronizedQuery = query;
    }

    internal void ClearIsTamerNotSynchronizedQuery()
    {
        isTamerNotSynchronizedQuery = null;
    }

    internal bool IsTamerNotSynchronized(string guid) => isTamerNotSynchronizedQuery?.Invoke(guid) ?? true;

    // IsAreaOverlapDisabled
    private Func<string, bool>? isAreaOverlapDisabledQuery;

    internal void SetIsAreaOverlapDisabledQuery(Func<string, bool> query)
    {
        if (isAreaOverlapDisabledQuery is not null)
            logger.LogError("IsAreaOverlapDisabledQuery is already set. Overriding the existing query.");

        isAreaOverlapDisabledQuery = query;
    }

    internal void ClearIsAreaOverlapDisabledQuery()
    {
        isAreaOverlapDisabledQuery = null;
    }

    internal bool IsAreaOverlapDisabled(string guid) => isAreaOverlapDisabledQuery?.Invoke(guid) ?? false;
}