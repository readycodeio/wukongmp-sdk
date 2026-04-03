using System;
using BtlShare;
using Microsoft.Extensions.Logging;

namespace WukongMp.Api.Configuration;

/// <summary>
/// Configuration class for gameplay-related settings and queries.
/// </summary>
[Obsolete("Will be replaced with a configuration file in the future.")]
public sealed class GameplayConfiguration(ILogger logger)
{
    /// <summary>
    /// Can secondary lock targets (other than the character's body) be locked when using the camera lock-on feature?
    /// This is <c>true</c> in co-op and <c>false</c> in PvP.
    /// </summary>
    public bool IsSupportMultiLockEnabled { get; set; }

    /// <summary>
    /// Is immunity to strong damage enabled?
    /// This would prevent characters from being one-shot by powerful attacks, providing a more balanced gameplay experience.
    /// This is <c>true</c> in PvP and <c>false</c> in co-op.
    /// </summary>
    public bool IsStrongDamageImmueEnabled { get; set; }

    /// <summary>
    /// When enabled, allows for a custom camera arm length to be set, which can affect how close or far the camera is from the character.
    /// This is <c>true</c> in PvP and <c>false</c> in co-op.
    /// </summary>
    public bool EnableCustomCameraArmLength { get; set; }

    internal bool DeleteDestroyedTamersFromEcs { get; set; }

    internal bool DisableCutscenes { get; set; }

    [Obsolete("To be replaced by data sync direction after refactoring")]
    internal bool SyncTamerTeamFromGameToEcs { get; set; }

    [Obsolete("To be replaced by data sync direction after refactoring")]
    internal bool OverrideLocalPlayerTeamFromGlobalEntity { get; set; }

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