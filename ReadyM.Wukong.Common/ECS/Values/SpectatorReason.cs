namespace ReadyM.Wukong.Common.ECS.Values;

/// <summary>
/// The reason why the character is in spectator mode.
/// </summary>
public enum SpectatorReason
{
    /// <summary>
    /// Free camera mode was set from code.
    /// </summary>
    Api,

    /// <summary>
    /// The character died and entered free camera mode.
    /// </summary>
    Death,
}