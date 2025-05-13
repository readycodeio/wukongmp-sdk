using System.Collections.Generic;
using b1;
using b1.BGW;
using B1UI.GSUI;
using BtlB1;
using CSharpModBase;
using GSE.GSUI;
using HarmonyLib;
using UnrealEngine.AssetRegistry;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.Resources;

namespace WukongApi
{
    public static class GameUtils
    {
        private static UWorld? _world;

        public static UWorld? GetWorld()
        {
            if (_world == null)
            {
                var obj = GCHelper.FindRef(FGlobals.GWorld)?.Managed;
                _world = (obj is UWorld ? obj : null) as UWorld;
            }

            return _world;
        }

        public static BGUPlayerCharacterCS? GetControlledPawn()
        {
            var pawn = UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld())?.GetControlledPawn() as BGUPlayerCharacterCS;
            return pawn.IsNullOrDestroyed() ? null : pawn;
        }

        public static IEnumerable<BGUCharacterCS> GetMonsters()
        {
            var world = GetWorld();
            if (world == null)
                yield break;

            var actors = world.GetAllActorsOfClass<BUTamerActor>();
            foreach (var actor in actors)
            {
                Logging.LogDebug("Found actor: {ActorName}", actor.GetName());

                var monster = actor.GetMonster();
                if (monster != null)
                {
                    Logging.LogDebug("Actor is a monster");
                    yield return monster;
                }
            }
        }

        public static void DestroyAllTamers()
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                if (actor != null && actor.GetMonster() != null)
                {
                    BGU_UnrealWorldUtil.DestroyActor(actor.GetMonster());
                }
                BGU_UnrealWorldUtil.DestroyActor(actor);
            }
        }

        public static BGP_PlayerControllerB1 GetPlayerController()
        {
            return (BGP_PlayerControllerB1)UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld());
        }

        public static BUS_GSEventCollection GetBUS_GSEventCollection()
        {
            return BUS_EventCollectionCS.Get(GetControlledPawn());
        }

        public static BGUPlayerCharacterCS? GetThis()
        {
            var controlledPawn = GetControlledPawn();
            return (controlledPawn is BGUPlayerCharacterCS ? controlledPawn : null) as BGUPlayerCharacterCS;
        }

        private static string GetSaveDirectory()
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
            return FPaths.Combine(GetSaveDirectory(), slotName);
        }

        public static bool IsGameInstanceValid() => BGWGameInstanceCS.Get(null) != null;

        public static bool IsWorldValid() => GetWorld() != null;

        public static void ShowTip(string tip)
        {
            Utils.TryRunOnGameThread(() =>
            {
                GenAGPage.ShowPage(39, nameof(ShowTip));
                var dSSimTipsData = new DSSimTipsData(ETipsType.WarnTips, FText.FromString(tip), InIsCloseAutoHide: false, 5);
                GenACommTips.SetTipsData(dSSimTipsData, nameof(ShowTip));
            });
        }

        public static void ShowPvPCountDown()
        {
            Utils.TryRunOnGameThread(() =>
            {
                var client = WukongMP.Instance.Client;
                var current = client.RoomState.CurrentRound;
                var total = client.RoomState.TournamentRounds;
                ShowTip(string.Format(Texts.RoundCount, current, total));
            });
        }

        public static void PlayBossDefeatedSound()
        {
            Utils.TryRunOnGameThread(() =>
            {
                var playUiSound = AccessTools.Method("B1UI.Script.GSUI.Util.GSUIAudioUtil:PlayUISound");
                playUiSound.Invoke(null, ["EVT_ui_kill_jisha_manjingtou"]);
            });
        }

        public static string GetTeamName(int teamId)
        {
            if (teamId == Constants.AvailableTeamIds[0])
                return "Red";
            if (teamId == Constants.AvailableTeamIds[1])
                return "Blue";
            return "";
        }

        public static string GetLocalizedTeamName(int teamId)
        {
            if (teamId == Constants.AvailableTeamIds[0])
                return Texts.RedTeam;
            if (teamId == Constants.AvailableTeamIds[1])
                return Texts.BlueTeam;
            return "";
        }

        public static int GetOppositeTeam(int teamId)
        {
            if (teamId == Constants.DrawTeamId)
                return teamId;
            return teamId == Constants.AvailableTeamIds[0] ? Constants.AvailableTeamIds[1] : Constants.AvailableTeamIds[0];
        }

        public static UBGWDataAsset? GetFxAssetByResId(UObject context, IList<FPlayFXByResID> fXs, int targetResId, int ownerResId)
        {
            var text = "";
            foreach (var fx in fXs)
            {
                if (fx.ResID == targetResId)
                {
                    text = fx.FXPathByDBC;
                    break;
                }

                if (fx.ResID == ownerResId)
                {
                    text = fx.FXPathByDBC;
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            return BGW_PreloadAssetMgr.Get(context).TryGetCachedResourceObj<UBGWDataAsset>(text, ELoadResourceType.AsyncLoadAndCache);
        }

        public static ImmobilizeConfigInstance CreateImmobilizeConfig(AActor character, AActor casterActor, FUStImmobilizeSkillConfigDesc cachedImmobilizeConfigDesc, int castImmobilizeDataResId, bool hasBuff)
        {
            var immobilizeConfigInstance = new ImmobilizeConfigInstance();
            var actorResID3 = BGU_DataUtil.GetActorResID(character);
            immobilizeConfigInstance.DurationSecond = cachedImmobilizeConfigDesc.DurationMs * 0.001f;
            immobilizeConfigInstance.AlmostEndAheadTimeSecond = cachedImmobilizeConfigDesc.AlmostEndAheadTimeMs * 0.001f;
            immobilizeConfigInstance.MinDurationSecond = cachedImmobilizeConfigDesc.MinimalDurationMs * 0.001f;
            immobilizeConfigInstance.RepeatedImmobilizedDef = cachedImmobilizeConfigDesc.RepeatedImmobilizedDef * 0.0001f;
            immobilizeConfigInstance.CasterActor = casterActor;
            immobilizeConfigInstance.bEnableGreatSageTalent = cachedImmobilizeConfigDesc.GreatSageTalentActiveBuff > 0 && hasBuff;
            immobilizeConfigInstance.BeginFX = GetFxAssetByResId(character, cachedImmobilizeConfigDesc.BeginFXs, actorResID3, castImmobilizeDataResId);
            immobilizeConfigInstance.AlmostEndFX = GetFxAssetByResId(character, cachedImmobilizeConfigDesc.AlmostEndFXs, actorResID3, castImmobilizeDataResId);
            immobilizeConfigInstance.EndFX = GetFxAssetByResId(character, cachedImmobilizeConfigDesc.EndFXs, actorResID3, castImmobilizeDataResId);
            immobilizeConfigInstance.QuickFX = GetFxAssetByResId(character, cachedImmobilizeConfigDesc.QuickEndFXs, actorResID3, castImmobilizeDataResId);
            immobilizeConfigInstance.BreakingFXsTriggerRatio = cachedImmobilizeConfigDesc.BreakingFXsTriggerRatio * 0.0001f;
            immobilizeConfigInstance.BreakingFX = GetFxAssetByResId(character, cachedImmobilizeConfigDesc.BreakingFXs, actorResID3, castImmobilizeDataResId);
            foreach (var beginEffect in cachedImmobilizeConfigDesc.BeginEffects)
            {
                immobilizeConfigInstance.BeginEffects.Add(new FSpellEffectForData(beginEffect));
            }

            foreach (var endEffect in cachedImmobilizeConfigDesc.EndEffects)
            {
                immobilizeConfigInstance.EndEffects.Add(new FSpellEffectForData(endEffect));
            }

            foreach (var breakEffect in cachedImmobilizeConfigDesc.BreakEffects)
            {
                immobilizeConfigInstance.BreakEffects.Add(new FSpellEffectForData(breakEffect));
            }

            foreach (var deadEffect in cachedImmobilizeConfigDesc.DeadEffects)
            {
                immobilizeConfigInstance.DeadEffects.Add(new FSpellEffectForData(deadEffect));
            }

            return immobilizeConfigInstance;
        }

        public static bool IsSkillWhitelisted(int skillId)
        {
            return Constants.SkillsWhitelist.Contains(skillId);
        }

        public static void ListAssets(string path)
        {
            UAssetDataArray assetsInFolder = UGSE_AssetUtilFuncLib.GetAssetsInFolder(new FName(path), bRecursive: true);
            if (assetsInFolder == null)
            {
                return;
            }

            int i = 0;
            foreach (FAssetData item6 in assetsInFolder.AssetDataArr)
            {
                Logging.LogInformation("Asset {Id} path : {Name}", i++, item6.GetFullName().ToString());
            }
        }

        public static string UnifyUnitName(string unitName)
        {
            return unitName.ToLower().Replace("-", "").Replace("_", "");
        }

        public static FVector GetFinalLocation(ABGUCharacter? CharacterCS, FVector InTargetLocation)
        {
            // TODO: For Heart of Birthstone map adjustment resulted in falling - invisible collision. So it is disabled for now.
            if (CmdLineParams.Instance.LevelId == 0)
            {
                return InTargetLocation;
            }
            FVector result = InTargetLocation;
            if (CharacterCS == null)
            {
                return result;
            }
            UCapsuleComponent? uCapsuleComponent = CharacterCS.GetRootComponent() as UCapsuleComponent;
            if (uCapsuleComponent == null)
            {
                return result;
            }
            float scaledCapsuleHalfHeight = uCapsuleComponent.GetScaledCapsuleHalfHeight();
            float scaledCapsuleHalfHeight2 = uCapsuleComponent.GetScaledCapsuleHalfHeight();
            float num = 2.4f;
            FVector start = InTargetLocation + FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
            FVector end = InTargetLocation - FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
            if (UGSE_TraceFuncLib.CharacterCapsuleTraceSingleByProfile(GetWorld(), start, end, scaledCapsuleHalfHeight2, scaledCapsuleHalfHeight, B1GlobalFNames.Pawn, bTraceComplex: false, CharacterCS, out var OutHitLocation))
            {
                result = OutHitLocation + num + FVector.UpVector * scaledCapsuleHalfHeight;
            }
            return result;
        }
    }
}