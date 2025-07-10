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
        if (!DI.Instance.RelayClient.InRoom)
            return;

        if (Action.ParamsInt.Count > 1 && InteractiveActor is BGUCharacterCS)
        {
            var entity = DI.Instance.PawnRegistry.GetMonsterByActor(InteractiveActor);
            if (entity.HasValue)
            {
                ref var netComp = ref entity.Value.GetComponent<NetworkIdComponent>();
                Logging.LogDebug("Sending skill interact for {ActorName} with ID {NetId}.", InteractiveActor.GetName(), netComp.Id);
                DI.Instance.Rpc.SendTamerSkillInteract(new DTO.SkillInteractData(netComp, Action.ParamsInt[1]));
            }
        }
    }
}
