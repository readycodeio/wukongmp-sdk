using System.Reflection;
using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils
{
    public static class GameSaveUtils
    {
        public static string GetModDirectory()
        {
            var modName = Assembly.GetExecutingAssembly().GetName().Name;

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