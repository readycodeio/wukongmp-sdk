using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils
{
    public static class GameSaveUtils
    {
        public static string GetModDirectory()
        {
            if (CmdLineParams.Instance.ModFolderOverride != null)
            {
                return FPaths.Combine(CmdLineParams.Instance.ModFolderOverride, "WukongMPMod");
            }

            return FPaths.Combine(FPaths.ProjectDir, "Binaries", "Win64", "CSharpLoader", "Mods", "WukongMPMod");
        }

        public static string GetSaveFileFullName(string slotName)
        {
            slotName += ".sav";
            return FPaths.Combine(GetModDirectory(), slotName);
        }
    }
}
