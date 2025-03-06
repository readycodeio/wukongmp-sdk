using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ArchiveB1;
using b1;
using B1UI.GSSvc;
using B1UI.GSUI;
using CommB1;
using HarmonyLib;
using UnrealEngine.Runtime;

namespace WukongApi.Patches
{
    static class SavePatchesData
    {
        public static bool CustomSaveEnabled = false;
        public static bool ShouldCacheSave = false;
    }

    [HarmonyPatch(typeof(GSWindowsPlatformSaveGame), nameof(GSWindowsPlatformSaveGame.GetFileFullName))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class PatchWindowsSaveGame
    {
        public static bool Prefix(ref string __result, string SlotName, string UserId)
        {
            if (!SavePatchesData.CustomSaveEnabled)
                return true;

            if (!SlotName.StartsWith("ArchiveSaveFile"))
                return true;

            __result = GameUtils.GetSaveFileFullName(SlotName);
            return false;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class PatchUIArchives
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("B1UI.GSUI.UIArchives:LoadArchive");
        }

        public static void Prefix()
        {
            SavePatchesData.ShouldCacheSave = true;
        }
    }

    [HarmonyPatch(typeof(GSB1UIUtil), nameof(GSB1UIUtil.StartNewGame))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class PatchStartNewGame
    {
        public static bool Prefix(UObject WorldContext)
        {
            SavePatchesData.CustomSaveEnabled = true;
            GSGMSvc.ClearAllAutoRunTag();
            if (BGW_GameLifeTimeMgr.Get(WorldContext).IsInFSMState(SGI_Global.MainMenu))
            {
                BGW_EventCollection.Get(WorldContext).Evt_ResetGameInstanceData(EGameInstanceResetType.StartNewGame);
            }

            BGW_EventCollection.Get(WorldContext).Evt_BGW_TriggerGlobalFSMEvent(EGI_Global.LoadArchive, new FSMInputData_GI_Global_SubG_GI_Loading_TravelLevel
            {
                ArchiveId = 9
            });
            return false;
        }
    }

    [HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.LoadArchive))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class PatchGameArchive
    {
        public static void Postfix(BGW_GameArchiveMgr __instance, ReadArchiveResult __result, int ArchiveId, LoadArchiveSource Source, ref FUStBEDArchivesData OutArchiveData)
        {
            if (__result != ReadArchiveResult.Success)
            {
                Logging.LogError($"Original readArchiveData Failed, Result:{__result}");
                return;
            }

            if (!SavePatchesData.CustomSaveEnabled)
            {
                if (SavePatchesData.ShouldCacheSave)
                {
                    SavePatchesData.ShouldCacheSave = false;
                    var characterArchiveSlotName = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, ArchiveId);
                    var characterArchiveFullName = GSWindowsPlatformSaveGame.GetFileFullName(characterArchiveSlotName, __instance.ArchiveWorker.UserId);

                    SavePatchesData.CustomSaveEnabled = true;
                    var newCharacterArchiveSlotName = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, Constants.CharacterArchiveId);
                    var newCharacterArchiveFullName = GSWindowsPlatformSaveGame.GetFileFullName(newCharacterArchiveSlotName, __instance.ArchiveWorker.UserId);
                    File.Copy(characterArchiveFullName, newCharacterArchiveFullName, true);
                }
                else
                {
                    SavePatchesData.CustomSaveEnabled = true;
                    var characterReadArchiveResult = __instance.ReadArchiveData(Constants.CharacterArchiveId, out var CharacterGameArchiveData, out var CharacterArchiveCanBeRepaired);
                    OutArchiveData = CharacterGameArchiveData.GameArchiveData;
                }
            }

            // Read archive with our world state.
            var readArchiveResult = __instance.ReadArchiveData(Constants.LevelArchiveId, out var GameArchiveData, out var ArchiveCanBeRepaired);
            if (readArchiveResult != 0)
            {
                Logging.LogError($"ReadArchiveData Failed, Result:{readArchiveResult}");
                return;
            }

            SavePatchesData.CustomSaveEnabled = false;

            // Keep only RoleData with player state
            OutArchiveData.LevelArchiveData = GameArchiveData.GameArchiveData.LevelArchiveData;
            OutArchiveData.PersistentECSData = GameArchiveData.GameArchiveData.PersistentECSData;
            OutArchiveData.StateMachineArchiveData = GameArchiveData.GameArchiveData.StateMachineArchiveData;
            OutArchiveData.TaskArchiveData = GameArchiveData.GameArchiveData.TaskArchiveData;

            OutArchiveData.RoleData.RoleCs.Actor.Wear.SpellList.Clear();
            OutArchiveData.RoleData.RoleCs.Actor.Wear.SpellList.Add(new SpellItem { SpellId=5101, Type=BtlB1.SpellType.QiShu }); // Immobilize
            OutArchiveData.RoleData.RoleCs.Actor.Wear.SpellList.Add(new SpellItem { SpellId=5201, Type=BtlB1.SpellType.ShenFa }); // Phantom dash
            OutArchiveData.RoleData.RoleCs.Actor.Wear.WearSoulSkill = null;
            OutArchiveData.RoleData.RoleCs.Actor.Wear.WearAccessory = null;
            OutArchiveData.RoleData.RoleCs.Actor.Wear.ShortcutsList.Clear();

            OutArchiveData.RoleData.RoleCs.Actor.Progress.SpellList.Remove(5102); // Ring of fire
            OutArchiveData.RoleData.RoleCs.Actor.Progress.SpellList.Remove(5103); // Spell binder
            OutArchiveData.RoleData.RoleCs.Actor.Progress.SpellList.Remove(5202); // Rock solid
        }
    }

    //// Disable game saves while multiplayer is enabled
    [HarmonyPatch(typeof(BGW_ArchiveReadWriteWorker), "CheckSaveTask")]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class PatchArchiveReadWriter
    {
        public static bool Prefix(Dictionary<string, ArchiveAsyncRequest> ___PendingRequests)
        {
            if (WukongMP.Instance.DisableArchiveSave)
            {
                return false;
            }

            if (___PendingRequests.Count == 0)
            {
                WukongMP.Instance.DisableArchiveSave = true;
                return false;
            }

            return true;
        }
    }

    //// Disable adding save game requests
    [HarmonyPatch(typeof(BGW_ArchiveReadWriteWorker), nameof(BGW_ArchiveReadWriteWorker.AppendArchiveSaveRequest), new[] { typeof(int), typeof(GSArchiveFileContainer), typeof(List<ArchiveSaveRequestOne>) })]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class PatchArchiveReadWriterAppendArchive1
    {
        public static bool Prefix(int ArchiveId, GSArchiveFileContainer ArchiveWriteContainer, List<ArchiveSaveRequestOne> saveArchiveRequests)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.MarkSaveSetting))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class PatchArchiveReadWriterAppendArchive2
    {
        public static bool Prefix(UISettingArchiveData UISettingArchiveData)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.IsArchiveNewGameplusReady))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchIsArchiveNewGameplusReady
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }


    [HarmonyPatch(typeof(GSB1UIUtil), nameof(GSB1UIUtil.CheckArchiveFull))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchCheckArchiveFull
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}