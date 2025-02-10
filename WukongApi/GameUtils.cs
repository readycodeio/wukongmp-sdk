using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using b1;
using B1UI.GSUI;
using CSharpModBase;
using GSE.GSUI;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

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
            return UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld()).GetControlledPawn();
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
                Console.WriteLine($"Found actor: {actor.GetName()}");

                var monster = actor.GetMonster();
                if (monster != null)
                {
                    Console.WriteLine("Actor is a monster");
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
            return FPaths.Combine(FPaths.ProjectDir, "Saved", "Readycode");
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
            Utils.TryRunOnGameThread(() => { GenAGPage.ShowPage(95, nameof(ShowPvPCountDown)); });

            Task.Run(async () =>
            {
                await Task.Delay(4000);
                Utils.TryRunOnGameThread(() =>
                {
                    var photon = WukongMP.Instance.Photon;
                    var current = photon.CurrentRoomState.CurrentRound;
                    var total = photon.CurrentRoomState.RoundsTotal;
                    ShowTip($"Round {current} of {total}");
                });
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
    }
}