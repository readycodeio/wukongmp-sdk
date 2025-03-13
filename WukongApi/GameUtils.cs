using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using b1;
using b1.BGW;
using B1UI.GSUI;
using BtlB1;
using CSharpModBase;
using GSE.GSUI;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.API;

namespace WukongApi
{
    public static class GameUtils
    {
        private static UWorld _world;

        public static UWorld GetWorld()
        {
            if (_world == null)
            {
                var obj = GCHelper.FindRef(FGlobals.GWorld)?.Managed;
                _world = (UWorld)(obj is UWorld ? obj : null);
            }

            return _world;
        }

        public static APawn GetControlledPawn()
        {
            var pawn = UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld())?.GetControlledPawn();
            if (pawn == null || pawn.IsDestroyed)
                return null;
            return pawn;
        }

        public static BGUPlayerCharacterCS GetBguPlayerCharacterCs()
        {
            var controlledPawn = GetControlledPawn();
            return (BGUPlayerCharacterCS)(controlledPawn is BGUPlayerCharacterCS ? controlledPawn : null);
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

        public static BGP_PlayerControllerB1 GetPlayerController()
        {
            return (BGP_PlayerControllerB1)UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld());
        }

        public static BUS_GSEventCollection GetBUS_GSEventCollection()
        {
            return BUS_EventCollectionCS.Get(GetControlledPawn());
        }

        public static BGUPlayerCharacterCS GetThis()
        {
            var controlledPawn = GetControlledPawn();
            return (BGUPlayerCharacterCS)(controlledPawn is BGUPlayerCharacterCS ? controlledPawn : null);
        }

        public static string GetSaveDirectory()
        {
            if (CmdLineParams.Instance.ModFolderOverride is not null)
            {
                return FPaths.Combine(CmdLineParams.Instance.ModFolderOverride, "WukongMPMod");
            }

            return FPaths.Combine(FPaths.ProjectDir, "Binaries", "Win64", "CSharpLoader", "Mods", "WukongMPMod");
        }

        public static string GetSaveFileFullName(string SlotName)
        {
            SlotName += ".sav";
            return FPaths.Combine(GetSaveDirectory(), SlotName);
        }

        public static bool IsGameInstanceValid()
        {
            if (BGWGameInstanceCS.Get(null) != null)
            {
                return true;
            }

            return false;
        }

        public static bool IsWorldValid()
        {
            if (GetWorld() != null)
            {
                return true;
            }

            return false;
        }

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
                var photon = WukongMP.Instance.Photon;
                var current = photon.CurrentRoomState.CurrentRound;
                var total = photon.CurrentRoomState.RoundsTotal;
                ShowTip($"Round {current} of {total}");
            });
        }

        public static void PlayBossDefeatedSound()
        {
            Utils.TryRunOnGameThread(() =>
            {
                var playUiSound = AccessTools.Method("B1UI.Script.GSUI.Util.GSUIAudioUtil:PlayUISound");
                playUiSound.Invoke(null, new object[] { "EVT_ui_kill_jisha_manjingtou" });
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

        public static UBGWDataAsset GetFXAssetByResID(UObject context, IList<FPlayFXByResID> FXs, int targetResID, int ownerResID)
        {
            string text = "";
            foreach (FPlayFXByResID FX in FXs)
            {
                if (FX.ResID == targetResID)
                {
                    text = FX.FXPathByDBC;
                    break;
                }

                if (FX.ResID == ownerResID)
                {
                    text = FX.FXPathByDBC;
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            return BGW_PreloadAssetMgr.Get(context).TryGetCachedResourceObj<UBGWDataAsset>(text, ELoadResourceType.AsyncLoadAndCache);
        }

        public static ImmobilizeConfigInstance CreateImmobilizeConfig(AActor character, AActor casterActor, FUStImmobilizeSkillConfigDesc cachedImmobilizeConfigDesc, int CastImmobilizeDataResId, bool hasBuff)
        {
            ImmobilizeConfigInstance immobilizeConfigInstance = new ImmobilizeConfigInstance();
            int actorResID3 = BGU_DataUtil.GetActorResID(character);
            immobilizeConfigInstance.DurationSecond = cachedImmobilizeConfigDesc.DurationMs * 0.001f;
            immobilizeConfigInstance.AlmostEndAheadTimeSecond = (float)cachedImmobilizeConfigDesc.AlmostEndAheadTimeMs * 0.001f;
            immobilizeConfigInstance.MinDurationSecond = (float)cachedImmobilizeConfigDesc.MinimalDurationMs * 0.001f;
            immobilizeConfigInstance.RepeatedImmobilizedDef = (float)cachedImmobilizeConfigDesc.RepeatedImmobilizedDef * 0.0001f;
            immobilizeConfigInstance.CasterActor = casterActor;
            immobilizeConfigInstance.bEnableGreatSageTalent = cachedImmobilizeConfigDesc.GreatSageTalentActiveBuff > 0 && hasBuff;
            immobilizeConfigInstance.BeginFX = GameUtils.GetFXAssetByResID(character, cachedImmobilizeConfigDesc.BeginFXs, actorResID3, CastImmobilizeDataResId);
            immobilizeConfigInstance.AlmostEndFX = GameUtils.GetFXAssetByResID(character, cachedImmobilizeConfigDesc.AlmostEndFXs, actorResID3, CastImmobilizeDataResId);
            immobilizeConfigInstance.EndFX = GameUtils.GetFXAssetByResID(character, cachedImmobilizeConfigDesc.EndFXs, actorResID3, CastImmobilizeDataResId);
            immobilizeConfigInstance.QuickFX = GameUtils.GetFXAssetByResID(character, cachedImmobilizeConfigDesc.QuickEndFXs, actorResID3, CastImmobilizeDataResId);
            immobilizeConfigInstance.BreakingFXsTriggerRatio = (float)cachedImmobilizeConfigDesc.BreakingFXsTriggerRatio * 0.0001f;
            immobilizeConfigInstance.BreakingFX = GameUtils.GetFXAssetByResID(character, cachedImmobilizeConfigDesc.BreakingFXs, actorResID3, CastImmobilizeDataResId);
            foreach (FSpellEffect beginEffect in cachedImmobilizeConfigDesc.BeginEffects)
            {
                immobilizeConfigInstance.BeginEffects.Add(new FSpellEffectForData(beginEffect));
            }

            foreach (FSpellEffect endEffect in cachedImmobilizeConfigDesc.EndEffects)
            {
                immobilizeConfigInstance.EndEffects.Add(new FSpellEffectForData(endEffect));
            }

            foreach (FSpellEffect breakEffect in cachedImmobilizeConfigDesc.BreakEffects)
            {
                immobilizeConfigInstance.BreakEffects.Add(new FSpellEffectForData(breakEffect));
            }

            foreach (FSpellEffect deadEffect in cachedImmobilizeConfigDesc.DeadEffects)
            {
                immobilizeConfigInstance.DeadEffects.Add(new FSpellEffectForData(deadEffect));
            }

            return immobilizeConfigInstance;
        }

        public static bool IsSkillWhitelisted(int skillId)
        {
            return Constants.SkillsWhitelist.Contains(skillId);
        }
    }
}