using b1;
using BtlB1;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
using System.Reflection;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchComplexSkillDoInteractAction
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUIAComplexSkill:DoInteractAction");
    }

    public static void Prefix(int InteractiveActorID, AActor User, AActor InteractiveActor, FUStInteractionMappingDesc Action)
    {
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return;

        if (Action.ParamsInt.Count > 1 && InteractiveActor is BGUCharacterCS)
        {
            var entity = WukongMpMod.Instance.GetMonsterByActor(InteractiveActor);
            if (entity.HasValue)
            {
                ref var netComp = ref entity.Value.GetComponent<NetworkIdComponent>();
                Logging.LogDebug($"Sending skill interact for {InteractiveActor.GetName()} with ID {netComp.Id}.");
                WukongMpMod.Instance.SendTamerSkillInteract(new DTO.SkillInteractData(netComp, Action.ParamsInt[1]));
            }
        }
    }
}
