using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using b1;
using b1.BGU.BUAnim;
using b1.BGW;
using Microsoft.Extensions.Logging;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.WukongUtils;

public static class DebugUtils
{
    private static readonly List<AActor> TmpActors = [];

    public static void LogUe4SsPresence()
    {
        // check for presence of a known UE4SS files
        var win64Folder = FPaths.Combine(FPaths.ProjectDir, "Binaries", "Win64");
        var dwmApiPath = FPaths.Combine(win64Folder, "dwmapi.dll");
        var ue4ssFolder = FPaths.Combine(win64Folder, "ue4ss");

        if (File.Exists(dwmApiPath))
        {
            DI.Instance.Logger.LogInformation("dwmapi.dll file found at {Path}", dwmApiPath);
        }
        else
        {
            DI.Instance.Logger.LogInformation("dwmapi.dll file not found");
        }

        if (Directory.Exists(ue4ssFolder))
        {
            DI.Instance.Logger.LogInformation("ue4ss folder found at {Path}", ue4ssFolder);
            var modsPath = FPaths.Combine(ue4ssFolder, "Mods");

            if (Directory.Exists(modsPath))
            {
                var mods = Directory.EnumerateDirectories(modsPath);
                foreach (var mod in mods)
                {
                    var dirName = Path.GetFileName(mod);
                    DI.Instance.Logger.LogInformation("Found UE4SS mod folder: {ModFolder}", dirName);
                }
            }
        }
        else
        {
            DI.Instance.Logger.LogInformation("ue4ss folder not found");
        }
    }

    public static UClass? GetDebugCubeActorClass()
    {
        var world = GameUtils.GetWorld();
        if (world != null)
        {
            var debugCubeActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.DebugCubeActorPath, ELoadResourceType.SyncLoadAndCache);
            if (debugCubeActorClass == null)
            {
                Logging.LogError("Cannot find class of {Class} to spawn", Constants.DebugCubeActorPath);
                return null;
            }

            return debugCubeActorClass;
        }

        return null;
    }

    public static UClass? GetDebugSphereActorClass()
    {
        var world = GameUtils.GetWorld();
        if (world != null)
        {
            var debugActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.DebugSphereActorPath, ELoadResourceType.SyncLoadAndCache);
            if (debugActorClass == null)
            {
                Logging.LogError("Cannot find class of {Class} to spawn", Constants.DebugSphereActorPath);
                return null;
            }

            return debugActorClass;
        }

        return null;
    }

    public static AActor? SpawnActor(UClass unrealClass, FVector location, FRotator rotation)
    {
        var world = GameUtils.GetWorld();
        if (world != null)
        {
            var actor = world.SpawnActor(unrealClass, ref location, ref rotation);
            if (actor == null)
            {
                Logging.LogError("Cannot spawn actor {ActorName}", unrealClass.GetName());
                return null;
            }

            return actor;
        }

        return null;
    }

    public static List<AActor> GetInvisibleWallsAroundPlayer(float radius)
    {
        var world = GameUtils.GetWorld();
        var allActors = UGameplayStatics.GetAllActorsOfClass<AActor>(world);
        var playerLocation = GameUtils.GetControlledPawn()?.GetActorLocation() ?? FVector.ZeroVector;
        var wallActors = new List<AActor>();

        foreach (var actor in allActors)
        {
            if (actor == null)
                continue;
            var className = actor.GetClass()?.GetName();
            if (className == null)
                continue;
            var distance = FVector.Distance(actor.GetActorLocation(), playerLocation);
            if (distance < radius && className.Contains("BP_DynamicObstcle"))
            {
                wallActors.Add(actor);
            }
        }

        return wallActors;
    }

    public static void AddMarkerToActors(List<AActor> actors)
    {
        var world = GameUtils.GetWorld();
        var playerMarkerActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.PlayerMarkerPath, ELoadResourceType.SyncLoadAndCache);

        int i = 0;
        foreach (var actor in actors)
        {
            var guid = BGU_DataUtil.GetActorGuid(actor);
            Logging.LogDebug("{Id}: Processing actor with class: {ActorClass}, name: {ActorName}, guid {ActorGuid}", i++, actor.GetClass().GetName(), actor.GetName(), guid);
            var playerMarkerActor = BGU_UnrealWorldUtil.SpawnActor(world, playerMarkerActorClass);
            if (playerMarkerActor == null)
            {
                Logging.LogError("Cannot spawn player marker actor");
                return;
            }

            playerMarkerActor.CallFunctionByNameWithArguments($"SetText {guid} ()", true);
            playerMarkerActor.SetActorLocation(actor.GetActorLocation(), false, out _, true);
            TmpActors.Add(playerMarkerActor);
        }
    }

    public static void DestroyTmpMarkerActors()
    {
        foreach (var actor in TmpActors)
        {
            actor.DestroyActor();
        }

        TmpActors.Clear();
    }

    public static void ShowMarkersForInvisibleWalls(float radius)
    {
        AddMarkerToActors(GetInvisibleWallsAroundPlayer(radius));
    }

    public static void ResetPlayersAnimation()
    {
        foreach (var playerId in DI.Instance.State.AllPlayers)
        {
            if (playerId != DI.Instance.PlayerState.LocalPlayerId)
            {
                var characterEntity = DI.Instance.PlayerState.GetMainCharacterById(playerId);
                if (characterEntity == null)
                    return;

                var character = characterEntity.Value.GetLocalState().Pawn;
                if (character != null)
                    ResetActorAnimation(character);
            }
        }
    }

    public static void DumpPlayersAnimationDebugInfo()
    {
        foreach (var playerId in DI.Instance.State.AllPlayers)
        {
            var characterEntity = DI.Instance.PlayerState.GetMainCharacterById(playerId);
            if (characterEntity == null)
                return;

            var character = characterEntity.Value.GetLocalState().Pawn;
            if (character != null)
                DumpActorAnimationDebugInfo(character);
        }
    }

    public static void DumpTamerAnimationDebugInfo(string name)
    {
        var world = GameUtils.GetWorld();
        if (world == null)
            return;
        var actors = world.GetAllActorsOfClass<BUTamerActor>();
        foreach (var actor in actors)
        {
            var monster = actor.GetMonster();
            if (monster != null)
            {
                if (!monster.GetName().Contains(name))
                    continue;
                Logging.LogDebug("Found actor: {ActorName}", actor.GetName());
                DumpActorAnimationDebugInfo(monster);
            }
        }
    }

    public static void DumpActorAnimationDebugInfo(AActor pawn)
    {
        BUC_ABPHelperData animationHelperData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPHelperData>(pawn);
        BUC_ABPCommonSettingData commonData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPCommonSettingData>(pawn);
        BUC_ABPMotionMatchingData motionMatchingData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPMotionMatchingData>(pawn);
        BUC_ABPPlayerLocomotionData playerLocomotionData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPPlayerLocomotionData>(pawn);
        BUC_ABPCommonLocomotionData commonLocomotionData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPCommonLocomotionData>(pawn);
        BUC_ABPAdvancedMonsterLocomotionData advancedMonsterLocomotionData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPAdvancedMonsterLocomotionData>(pawn);
        BUC_ABPBasicData basicData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPBasicData>(pawn);
        BUC_ABPCharacterData characterData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPCharacterData>(pawn);
        BUC_ABPBGUCharacterData bguCharacterData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPBGUCharacterData>(pawn);
        BUC_ABPMonsterLocomotionData monsterLocomotionData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPMonsterLocomotionData>(pawn);
        BUC_ABPWheelMoveData wheelMoveData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPWheelMoveData>(pawn);
        BUC_ABPSplineMoveData splineMoveData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPSplineMoveData>(pawn);
        BUC_ABPSpeicalAdditiveData speicalAdditiveData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPSpeicalAdditiveData>(pawn);
        BUC_ABPSpecialMoveData specialMoveData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPSpecialMoveData>(pawn);
        BUC_ABPNPCAnimData aBPNPCAnimData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPNPCAnimData>(pawn);

        Logging.LogDebug("Animation debug info for: {Name}", pawn.GetName());
        Logging.LogDebug("FinalABPMoveMode: {MoveMode}", commonData.FinalABPMoveMode);
        Logging.LogDebug("HasValidMoveAnimConfig: {IsValid}", animationHelperData.HasValidMoveAnimConfig(EMoveSpeedLevel.Run, bLockMove: true));

        LogAllProperties(animationHelperData);
        LogAllProperties(commonData);
        LogAllProperties(motionMatchingData);
        LogAllProperties(motionMatchingData.CurrentAA);
        LogAllProperties(playerLocomotionData);
        LogAllProperties(commonLocomotionData);
        LogAllProperties(advancedMonsterLocomotionData);
        LogAllProperties(basicData);
        LogAllProperties(characterData);
        LogAllProperties(characterData.MovementComp);
        LogAllProperties(bguCharacterData);
        LogAllProperties(monsterLocomotionData);
        LogAllProperties(wheelMoveData);
        LogAllProperties(splineMoveData);
        LogAllProperties(speicalAdditiveData);
        LogAllProperties(specialMoveData);
        LogAllProperties(aBPNPCAnimData);

        var animInst = animationHelperData.AnimInst;
        if (!(animInst == null) && animInst is BUAnimHumanoidCS bUAnimHumanoidCS)
        {
            UAnimInstance moveAnimGraphInstance = bUAnimHumanoidCS.GetLinkedAnimGraphInstanceByTag(B1GlobalFNames.Move);
            if (!moveAnimGraphInstance.IsNullOrDestroyed())
            {
                LogAllProperties(moveAnimGraphInstance);
                var playerLocomotionAnimInst = moveAnimGraphInstance.GetLinkedAnimGraphInstanceByTag(B1GlobalFNames.PlayerLocomotion);
                if (!playerLocomotionAnimInst.IsNullOrDestroyed())
                {
                    LogAllProperties(playerLocomotionAnimInst);
                }

                var advancedMonsterLocomotionAnimInst = moveAnimGraphInstance.GetLinkedAnimGraphInstanceByTag(B1GlobalFNames.AdvancedMonsterLocomotion);
                if (!advancedMonsterLocomotionAnimInst.IsNullOrDestroyed())
                {
                    LogAllProperties(advancedMonsterLocomotionAnimInst);
                }

                var monsterLocomotionAnimInst = moveAnimGraphInstance.GetLinkedAnimGraphInstanceByTag(B1GlobalFNames.MonsterLocomotion);
                if (!monsterLocomotionAnimInst.IsNullOrDestroyed())
                {
                    LogAllProperties(monsterLocomotionAnimInst);
                }

                var motionMatchingAnimInst = moveAnimGraphInstance.GetLinkedAnimGraphInstanceByTag(B1GlobalFNames.MotionMatching);
                if (!motionMatchingAnimInst.IsNullOrDestroyed())
                {
                    LogAllProperties(motionMatchingAnimInst);
                }
            }
        }

        LogCurveValues(animationHelperData);
        LogStateMachineWeights(animationHelperData);
    }

    private static void LogAllProperties(object component)
    {
        foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(component))
        {
            if (descriptor == null)
                continue;

            if (descriptor.PropertyType.IsAssignableFrom(typeof(UBlendSpace)) || descriptor.PropertyType.IsAssignableFrom(typeof(UAnimSequence)))
                continue;

            var value = descriptor.GetValue(component);
            if (value == null)
                continue;

            Logging.LogDebug("{ObjectName} property name: {Name}, value: {Value}", component.GetType().Name, descriptor.Name, value.ToString());
        }
    }

    private static void LogStateMachineWeights(BUC_ABPHelperData animationHelperData)
    {
        foreach (var property in animationHelperData.StateMachineWeights)
        {
            foreach (var weight in property.Value)
            {
                Logging.LogDebug("StateMachineName: {StateMachineName}, stateName: {StateName}, value: {Value}", property.Key.ToString(), weight.ToString(), weight.Value);
            }
        }
    }

    private static void LogCurveValues(BUC_ABPHelperData animationHelperData)
    {
        foreach (var curve in animationHelperData.FloatCurveValues)
        {
            Logging.LogDebug("Curve name: {Name}, value {Value}", curve.Key.ToString(), curve.Value);
        }
    }

    public static void ResetActorAnimation(BGUCharacterCS player)
    {
        BUS_EventCollectionCS.Get(player)?.Evt_ResetABPSetting.Invoke();
    }

    public static void ResetActorStatus(BGUCharacterCS player)
    {
        BUS_EventCollectionCS.Get(player)?.Evt_ResetActorStatusPre.Invoke(EResetActorReason.Rebirth);
    }

    private static bool _superSpeed;
    private static float _originalFastSpeedRatio;
    private static float _originalNormalSpeedRatio;
    private static float _originalSlowSpeedRatio;

    public static void ToggleSuperFastSpeed()
    {
        BUC_SpeedCtrlData? speedCtrlData = BGU_DataUtil.GetUnPersistentReadOnlyData<IBUC_SpeedCtrlData, BUC_SpeedCtrlData>(GameUtils.GetControlledPawn()) as BUC_SpeedCtrlData;
        if (speedCtrlData == null)
            return;

        if (!_superSpeed)
        {
            _originalSlowSpeedRatio = speedCtrlData.GetMoveSpeedSlow();
            _originalNormalSpeedRatio = speedCtrlData.GetMoveSpeedNormal();
            _originalFastSpeedRatio = speedCtrlData.GetMoveSpeedFast();

            speedCtrlData.SetSpeedInfo(10000, 10000, 10000);
            _superSpeed = true;
        }
        else
        {
            speedCtrlData.SetSpeedInfo(_originalSlowSpeedRatio, _originalNormalSpeedRatio, _originalFastSpeedRatio);
            _superSpeed = false;
        }
    }
}