using ArchiveB1;
using b1;
using CommB1;
using HarmonyLib;

namespace WukongCSharpMod.Patches
{
    static class SavePatchesData
    {
        public static bool CustomSaveEnabled = false;
    }

    [HarmonyPatch(typeof(GSWindowsPlatformSaveGame), nameof(GSWindowsPlatformSaveGame.GetFileFullName))]
    [HarmonyPatchCategory(Constants.MultiplayerPatches)]
    public class PatchWindowsSaveGame
    {
        public static bool Prefix(ref string __result, string SlotName, string UserId)
        {
            if (!SavePatchesData.CustomSaveEnabled)
                return true;

            __result = GameUtils.GetReadySaveFileFullName(SlotName);
            return false;
        }
    }

    [HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.LoadArchive))]
    [HarmonyPatchCategory(Constants.MultiplayerPatches)]
    public class PatchGameArchive
    {
        public static void Postfix(BGW_GameArchiveMgr __instance, ReadArchiveResult __result, int ArchiveId, LoadArchiveSource Source, ref FUStBEDArchivesData OutArchiveData)
        {
            if (__result != ReadArchiveResult.Success)
            {
                Helpers.LogError($"Original readArchiveData Failed, Result:{__result}");
                return;
            }

            // Read archive with our world state.
            SavePatchesData.CustomSaveEnabled = true;
            ReadArchiveResult readArchiveResult = __instance.ReadArchiveData(0, out ArchiveFileUnpacked GameArchiveData, out EArchiveRepairStatus ArchiveCanBeRepaired);
            if (readArchiveResult != 0)
            {
                Helpers.LogError($"ReadArchiveData Failed, Result:{readArchiveResult}");
                return;
            }
            SavePatchesData.CustomSaveEnabled = false;

            // Keep only RoleData with player state
            OutArchiveData.LevelArchiveData = GameArchiveData.GameArchiveData.LevelArchiveData;
            OutArchiveData.PersistentECSData = GameArchiveData.GameArchiveData.PersistentECSData;
            OutArchiveData.StateMachineArchiveData = GameArchiveData.GameArchiveData.StateMachineArchiveData;
            OutArchiveData.TaskArchiveData = GameArchiveData.GameArchiveData.TaskArchiveData;

            MyMod.Instance.SetMultiplayerEnabled();
        }
    }

    // Disable game saves while multiplayer is enabled
    [HarmonyPatch(typeof(BGW_ArchiveReadWriteWorker), "CheckSaveTask")]
    [HarmonyPatchCategory(Constants.MultiplayerPatches)]
    public class PatchArchiveReadWriter
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}
