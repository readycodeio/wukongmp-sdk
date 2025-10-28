using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.WukongUtils
{
    public static class GameSaveUtils
    {
        public static string GetModDirectory()
        {
            var modName = Constants.IsCoop ? "WukongMp.Coop" : "WukongMp.PvP";

            if (LaunchParameters.Instance.ModFolderOverride != null)
            {
                return FPaths.Combine(LaunchParameters.Instance.ModFolderOverride, modName);
            }

            return FPaths.Combine(FPaths.ProjectDir, "Binaries", "Win64", "CSharpLoader", "Mods", modName);
        }

        public static string GetSaveFileFullName(string slotName)
        {
            slotName += ".sav";
            return FPaths.Combine(GetModDirectory(), slotName);
        }
    }
}