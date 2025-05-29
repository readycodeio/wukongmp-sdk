using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ArchiveB1;
using b1;
using B1UI.GSSvc;
using B1UI.GSUI;
using BtlB1;
using CommB1;
using HarmonyLib;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;

namespace WukongMp.Api.Patches
{
    internal static class SavePatchesData
    {
        public static bool CustomSaveEnabled;
        public static bool ShouldCacheSave;
    }

    /// Replace Steam save folder with ours.
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

    /// When "Load game" (save selector list ) is selected in main menu.
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

    /// Load our custom save on new game.
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
                ArchiveId = Constants.NewCharacterArchiveId
            });
            return false;
        }
    }

    /// Read the world save and character save data, clear spells and set the birth point.
    [HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.LoadArchive))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class PatchGameArchive
    {
        public static void Postfix(BGW_GameArchiveMgr __instance, ReadArchiveResult __result, int ArchiveId, LoadArchiveSource Source, ref FUStBEDArchivesData? OutArchiveData)
        {
            if (__result != ReadArchiveResult.Success)
            {
                Logging.LogError("Original readArchiveData Failed, Result: {Result}", __result);
                return;
            }

            if (OutArchiveData == null)
            {
                Logging.LogError("Original OutArchiveData is null");
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
                    var characterReadArchiveResult = __instance.ReadArchiveData(Constants.CharacterArchiveId, out var characterGameArchiveData, out var characterArchiveCanBeRepaired);
                    if (characterReadArchiveResult == ReadArchiveResult.Success)
                    {
                        OutArchiveData = characterGameArchiveData.GameArchiveData;
                    }
                }
            }

            if (!Constants.IsCoop)
            {
                // Read archive with our world state.
                var readArchiveResult = __instance.ReadArchiveData(Constants.WorldArchiveId, out var gameArchiveData, out var archiveCanBeRepaired);
                if (readArchiveResult != 0)
                {
                    Logging.LogError("ReadArchiveData Failed, Result: {Result}", readArchiveResult);
                    return;
                }

                // Keep only RoleData with player state
                OutArchiveData.LevelArchiveData = gameArchiveData.GameArchiveData.LevelArchiveData;
                OutArchiveData.PersistentECSData = gameArchiveData.GameArchiveData.PersistentECSData;
                OutArchiveData.StateMachineArchiveData = gameArchiveData.GameArchiveData.StateMachineArchiveData;
                OutArchiveData.TaskArchiveData = gameArchiveData.GameArchiveData.TaskArchiveData;

                var levelConfig = LevelSpawnConfig.GetCurrentLevelSpawnData();
                OutArchiveData.PersistentECSData.BPCData.BPCPlayerRoleData.MapId = levelConfig.MapId;
                OutArchiveData.PersistentECSData.BPCData.BPCPlayerRoleData.MapAreaId = levelConfig.MapAreaId;
                OutArchiveData.PersistentECSData.BPCData.BPCRebirthPointData.CurrentBirthPoint.PointID = levelConfig.BirthPointID;
            }
            else
            {
                OutArchiveData.PersistentECSData.BPCData.BPCPlayerRoleData.MapId = 10;
                OutArchiveData.PersistentECSData.BPCData.BPCPlayerRoleData.MapAreaId = 1;
                OutArchiveData.PersistentECSData.BPCData.BPCRebirthPointData.CurrentBirthPoint.PointID = 1004;
            }

            SavePatchesData.CustomSaveEnabled = false;

            if (!Constants.IsCoop)
            {
                OutArchiveData.RoleData.RoleCs.Actor.Wear.SpellList.Clear();
                OutArchiveData.RoleData.RoleCs.Actor.Wear.SpellList.Add(new SpellItem { SpellId = 5101, Type = SpellType.QiShu }); // Immobilize
                OutArchiveData.RoleData.RoleCs.Actor.Wear.SpellList.Add(new SpellItem { SpellId = 5201, Type = SpellType.ShenFa }); // Phantom dash
                OutArchiveData.RoleData.RoleCs.Actor.Wear.WearSoulSkill = null;
                OutArchiveData.RoleData.RoleCs.Actor.Wear.WearAccessory = null;

                OutArchiveData.RoleData.RoleCs.Actor.Progress.SpellList.Remove(5102); // Ring of fire
                OutArchiveData.RoleData.RoleCs.Actor.Progress.SpellList.Remove(5103); // Spell binder
                OutArchiveData.RoleData.RoleCs.Actor.Progress.SpellList.Remove(5202); // Rock solid
            }
        }
    }

    /// Disable game saves while multiplayer is enabled
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

    [HarmonyPatch(typeof(BPS_RebirthPointSystem), "OnSetRebirthPointAsCurrentBirthPoint")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnSetRebirthPointAsCurrentBirthPoint
    {
        public static void Postfix(UActorCompBaseCS __instance, int RebirthPointID)
        {
            Logging.LogWarning("BirthPointID updated: {Id}", RebirthPointID);
            FUStRebirthPointDesc fUStRebirthPointDesc = GameDBRuntime.GetFUStRebirthPointDesc(RebirthPointID);
            if (fUStRebirthPointDesc != null && BGUFuncLibMap.IsValidLevelId(fUStRebirthPointDesc.MapID))
            {
                Logging.LogWarning("MapId: {Id}", fUStRebirthPointDesc.MapID);
                Logging.LogWarning("MapAreaId: {Id}", BGUFuncLibMap.GetAreaId(__instance.GetOwner()));
            }
        }
    }

    [HarmonyPatch(typeof(BPS_RebirthPointSystem), "OnSetCurrentBirthPoint")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnSetCurrentBirthPoint
    {
        public static void Postfix(UActorCompBaseCS __instance, int BirthPointID)
        {
            Logging.LogWarning("BirthPointID updated: {Id}", BirthPointID);
            FUStRebirthPointDesc fUStRebirthPointDesc = GameDBRuntime.GetFUStRebirthPointDesc(BirthPointID);
            if (fUStRebirthPointDesc != null && BGUFuncLibMap.IsValidLevelId(fUStRebirthPointDesc.MapID))
            {
                Logging.LogWarning("MapId: {Id}", fUStRebirthPointDesc.MapID);
                Logging.LogWarning("MapAreaId: {Id}", BGUFuncLibMap.GetAreaId(__instance.GetOwner()));
            }
        }
    }

    [HarmonyPatch(typeof(BPS_RebirthPointSystem), "OnForceSetRebirthPoint")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnForceSetRebirthPoint
    {
        public static void Postfix(UActorCompBaseCS __instance, int RebirthPointId)
        {
            Logging.LogWarning("BirthPointID updated: {Id}", RebirthPointId);
            FUStRebirthPointDesc fUStRebirthPointDesc = GameDBRuntime.GetFUStRebirthPointDesc(RebirthPointId);
            if (fUStRebirthPointDesc != null && BGUFuncLibMap.IsValidLevelId(fUStRebirthPointDesc.MapID))
            {
                Logging.LogWarning("MapId: {Id}", fUStRebirthPointDesc.MapID);
                Logging.LogWarning("MapAreaId: {Id}", BGUFuncLibMap.GetAreaId(__instance.GetOwner()));
            }
        }
    }
}