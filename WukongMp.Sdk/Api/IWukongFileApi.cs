namespace WukongMp.Sdk.Api;

/// <summary>
/// Provides methods related to manipulating files for Wukong mods, such as save files.
/// </summary>
public interface IWukongFileApi
{
    /// <summary>
    /// Get the absolute path to the mod's directory.
    /// Useful for finding files packaged with the mod.
    /// </summary>
    /// <typeparam name="T">Pass the type of your mod's entry point. Used to find the mod's assembly and thus its directory.</typeparam>
    /// <returns>The absolute path to the mod's directory.</returns>
    string GetModDirectory<T>() where T : ModBase;
}