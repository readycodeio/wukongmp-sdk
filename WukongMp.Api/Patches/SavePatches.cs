using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArchiveB1;
using b1;
using B1UI.GSSvc;
using B1UI.GSUI;
using BtlB1;
using CommB1;
using HarmonyLib;
using ReadyM.Relay.Common;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches
{
    internal static class SavePatchesData
    {
        public static bool RedirectSaveFiles = Constants.IsCoop;
        public static bool ShouldCacheSave;
    }

    /// Replace Steam save folder with ours.
    [HarmonyPatch(typeof(GSWindowsPlatformSaveGame), nameof(GSWindowsPlatformSaveGame.GetFileFullName))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class PatchWindowsSaveGame
    {
        public static bool Prefix(ref string __result, string SlotName, string UserId)
        {
            if (!SavePatchesData.RedirectSaveFiles)
                return true;

            if (!SlotName.StartsWith("ArchiveSaveFile"))
                return true;

            __result = GameSaveUtils.GetSaveFileFullName(SlotName);
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
            SavePatchesData.RedirectSaveFiles = true;
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

            if (!SavePatchesData.RedirectSaveFiles)
            {
                if (SavePatchesData.ShouldCacheSave)
                {
                    SavePatchesData.ShouldCacheSave = false;
                    var characterArchiveSlotName = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, ArchiveId);
                    var characterArchiveFullName = GSWindowsPlatformSaveGame.GetFileFullName(characterArchiveSlotName, __instance.ArchiveWorker.UserId);

                    SavePatchesData.RedirectSaveFiles = true;
                    var newCharacterArchiveSlotName = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, Constants.CharacterArchiveId);
                    var newCharacterArchiveFullName = GSWindowsPlatformSaveGame.GetFileFullName(newCharacterArchiveSlotName, __instance.ArchiveWorker.UserId);
                    File.Copy(characterArchiveFullName, newCharacterArchiveFullName, true);
                }
                else
                {
                    SavePatchesData.RedirectSaveFiles = true;
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
            else if (false) // TODO: Enable this after we refactor the client to be always on for co-op
            {
                // Read archive with our co-op save.
                var worldDownloadTask = WukongMpMod.Instance.DownloadWorldSaveAsync();
                var playerDownloadTask = WukongMpMod.Instance.DownloadPlayerSaveAsync();

                Task.WhenAll(worldDownloadTask, playerDownloadTask).Wait();

                if (worldDownloadTask.Result is null)
                {
                    Logging.LogError("Failed to download world save file from the cloud");
                    return;
                }

                if (playerDownloadTask.Result is null)
                {
                    Logging.LogError("Failed to download player save file from the cloud");
                    return;
                }

                var worldData = worldDownloadTask.Result.Content;
                var playerData = playerDownloadTask.Result.Content;

                // we need to write the data as file to read it
                var worldSaveName = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, Constants.CoopWorldArchiveId);
                var worldSavePath = GSWindowsPlatformSaveGame.GetFileFullName(worldSaveName, __instance.ArchiveWorker.UserId);
                File.WriteAllBytes(worldSavePath, worldData);

                var playerSaveName = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, Constants.CoopPlayerArchiveId);
                var playerSavePath = GSWindowsPlatformSaveGame.GetFileFullName(playerSaveName, __instance.ArchiveWorker.UserId);
                File.WriteAllBytes(playerSavePath, playerData);

                var readWorldResult = __instance.ReadArchiveData(Constants.CoopWorldArchiveId, out var worldArchiveData, out var archiveCanBeRepaired);
                if (readWorldResult != ReadArchiveResult.Success)
                {
                    Logging.LogError("ReadArchiveData Failed, Result: {Result}", readWorldResult);
                    return;
                }

                var readPlayerResult = __instance.ReadArchiveData(Constants.CoopPlayerArchiveId, out var playerArchiveData, out archiveCanBeRepaired);
                if (readPlayerResult != ReadArchiveResult.Success)
                {
                    Logging.LogError("ReadArchiveData Failed, Result: {Result}", readPlayerResult);
                    return;
                }

                OutArchiveData = playerArchiveData.GameArchiveData;

                // Keep only RoleData with player state

                // World data:
                OutArchiveData.LevelArchiveData = worldArchiveData.GameArchiveData.LevelArchiveData;
                OutArchiveData.PersistentECSData = worldArchiveData.GameArchiveData.PersistentECSData;
                OutArchiveData.StateMachineArchiveData = worldArchiveData.GameArchiveData.StateMachineArchiveData;
                OutArchiveData.TaskArchiveData = worldArchiveData.GameArchiveData.TaskArchiveData;
            }

            SavePatchesData.RedirectSaveFiles = false;

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
    [HarmonyPatch(typeof(BGW_ArchiveReadWriteWorker), nameof(BGW_ArchiveReadWriteWorker.AppendArchiveSaveRequest), typeof(int), typeof(GSArchiveFileContainer), typeof(List<ArchiveSaveRequestOne>))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchArchiveReadWriteWorkerAppendArchiveSaveRequest
    {
        public static bool Prefix(int ArchiveId, GSArchiveFileContainer ArchiveWriteContainer, List<ArchiveSaveRequestOne> saveArchiveRequests)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return false;

            var data = ArchiveWriteContainer.GameArchiveFile.GameArchivesDataBytes.ToByteArray();

            if (data == null)
                return false;

            Logging.LogInformation("Will upload save to the cloud, ArchiveId: {ArchiveId}, Size: {Size} Mb", ArchiveId, (data.Length / (1024.0 * 1024.0)).ToString("F2"));

            Task.Run(async () =>
            {
                if (WukongMpMod.Instance.IsMasterClient)
                {
                    var uploadedWorld = await WukongMpMod.Instance.UploadWorldSaveAsync(data);
                    LogSuccess(uploadedWorld, "world save");
                }

                var uploadedPlayer = await WukongMpMod.Instance.UploadPlayerSaveAsync(data);
                LogSuccess(uploadedPlayer, "player save");
            });

            return false;
        }

        private static void LogSuccess(bool success, string name)
        {
            if (success)
            {
                Logging.LogInformation("Blob uploaded successfully: {Name}", name);
            }
            else
            {
                Logging.LogError("Failed to upload blob: {Name}", name);
            }
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