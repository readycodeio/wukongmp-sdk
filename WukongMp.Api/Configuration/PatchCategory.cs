namespace WukongMp.Api.Configuration;

/// <summary>
/// Category for Harmony patches.
/// Each patch must be assigned to a category.
/// </summary>
public static class PatchCategory
{
    /// <summary>
    /// Patches in this category are enabled at all times.
    /// </summary>
    public const string Global = "Global";

    /// <summary>
    /// Patches in this category are only enabled when the player is connected to the server.
    /// NOTE: In the current version of the SDK, this category is the same as <see cref="Global"/>.
    /// In the future, we will add support for enabling/disabling these patches based on the connection state of the player.
    /// </summary>
    public const string Connected = "Connected";

    /// <summary>
    /// Patches in this category are not enabled.
    /// </summary>
    public const string Disabled = "Disabled";
}