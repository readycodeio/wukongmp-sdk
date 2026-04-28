namespace WukongMp.Sdk.Api;

/// <summary>
/// Provides methods and properties for enabling and managing cheats in the game.
/// </summary>
public interface IWukongCheatsApi
{
    /// <summary>
    /// Gets a value indicating whether cheats are allowed.
    /// </summary>
    bool CheatsAllowed { get; }

    /// <summary>
    /// Toggles infinite mana for the player.
    /// </summary>
    void ToggleInfiniteMana();

    /// <summary>
    /// Resets all cooldowns for the player.
    /// </summary>
    void ResetCooldowns();

    /// <summary>
    /// Resets the player's mana to its maximum value.
    /// </summary>
    void ResetMana();

    /// <summary>
    /// Sets the cooldown time for the player's spirit abilities.
    /// </summary>
    /// <param name="spiritCooldownTime">The cooldown time to set, in seconds.</param>
    void SetSpritCooldownTime(float spiritCooldownTime);

    /// <summary>
    /// Toggles infinite vessel usage for the player.
    /// </summary>
    void ToggleInfiniteVessel();

    /// <summary>
    /// Toggles infinite transformation for the player.
    /// </summary>
    void ToggleInfiniteTransform();

    /// <summary>
    /// Toggles no cooldowns for skills for the player.
    /// </summary>
    void ToggleNoSkillsCooldown();
}