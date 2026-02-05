using b1;
using BtlB1;
using BtlShare;
using HarmonyLib;
using System.Reflection;
using PreludeLib.Attributes;
using ReadyM.Api.Multiplayer.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchComplexSkillDoInteractAction
{
    [HarmonyTargetMethodHint("b1.BUIAComplexSkill", "DoInteractAction")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUIAComplexSkill:DoInteractAction");
    }

    public static void Prefix(int InteractiveActorID, AActor User, AActor InteractiveActor, FUStInteractionMappingDesc Action)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (Action.ParamsInt.Count > 1 && InteractiveActor is BGUCharacterCS)
        {
            var entity = DI.Instance.PawnState.GetEntityByTamerMonster(InteractiveActor);
            if (entity.HasValue)
            {
                ref var meta = ref entity.Value.GetMeta();
                Logging.LogDebug("Sending skill interact for {Name} with ID {Id}.", InteractiveActor.GetName(), meta.NetId);
                DI.Instance.Rpc.SendTamerSkillInteract(new DTO.SkillInteractData(meta.NetId, Action.ParamsInt[1]));
            }
        }
    }
}

[HarmonyPatch(typeof(BGW_EffectTemplateList), nameof(BGW_EffectTemplateList.GetInteractTypeTemplate))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchGetInteractTypeTemplate
{
    public static bool Prefix(EInteractType InteractType, BUInteractTypeTemplate? __result)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        Logging.LogDebug("GetInteractTypeTemplate called for {Type}", InteractType);
        if (DI.Instance.GameplayConfiguration.IsInteractionAllowed(InteractType))
        {
            return true;
        }
        else
        {
            __result = null;
            return false;
        }
    }
}
